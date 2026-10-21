using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace BudgetBuddy
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += LoadEmbedded;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Run();
        }

        /// <summary>The WebView2 libraries are packed inside the exe, so Budget Buddy is one file next to app\ and data\.</summary>
        static Assembly LoadEmbedded(object sender, ResolveEventArgs e)
        {
            byte[] bytes = Resource(new AssemblyName(e.Name).Name + ".dll");
            return bytes == null ? null : Assembly.Load(bytes);
        }

        internal static byte[] Resource(string name)
        {
            using (Stream s = typeof(Program).Assembly.GetManifestResourceStream(name))
            {
                if (s == null) return null;
                var ms = new MemoryStream();
                s.CopyTo(ms);
                return ms.ToArray();
            }
        }

        static void Fail(string message) =>
            MessageBox.Show(message, "Budget Buddy", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        // Kept out of Main so the WebView2 types load only after AssemblyResolve is hooked up.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Run()
        {
            string root = AppDomain.CurrentDomain.BaseDirectory;
            string htmlFile = Path.Combine(root, "app", "budget.html");
            string env = Environment.GetEnvironmentVariable("BUDGET_DATA_DIR");
            string dataDir = Path.GetFullPath(string.IsNullOrEmpty(env) ? Path.Combine(root, "data") : env);

            if (!File.Exists(htmlFile))
            {
                Fail("Can't find app\\budget.html.\n\nKeep Budget Buddy.exe in the Budget Buddy folder, next to the \"app\" and \"data\" folders.");
                return;
            }

            // One window per data folder: two copies saving the same file would overwrite each other.
            uint hash = 2166136261;
            foreach (char c in dataDir.ToLowerInvariant()) hash = (hash ^ c) * 16777619;
            string key = "BudgetBuddy-" + hash.ToString("x8");
            using (var single = new Mutex(true, key, out bool first))
            {
                if (!first || Server.FindRunning(dataDir) != null)
                {
                    Fail("Budget Buddy is already open.\n\nLook for its window on the taskbar (or close the other copy first).");
                    return;
                }

                Server server;
                try { server = new Server(htmlFile, dataDir); }
                catch (Exception ex) { Fail("Could not start Budget Buddy.\n\n" + ex.Message); return; }

                try
                {
                    string loaderDir = ExtractLoader();
                    MainForm.SetLoaderFolder(loaderDir);
                    Application.Run(new MainForm(server));
                }
                finally { server.Stop(); }
            }
        }

        /// <summary>WebView2Loader.dll is native, so it can't be loaded from memory: copy the one for this CPU next to the app's settings.</summary>
        static string ExtractLoader()
        {
            string arch;
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.Arm64: arch = "arm64"; break;
                case Architecture.X86: arch = "x86"; break;
                default: arch = "x64"; break;
            }
            string version = typeof(Program).Assembly.GetName().Version.ToString();
            string dir = Path.Combine(MainForm.LocalDir, "loader", version + "-" + arch);
            string file = Path.Combine(dir, "WebView2Loader.dll");
            if (!File.Exists(file))
            {
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(file, Resource(arch + ".WebView2Loader.dll"));
            }
            return dir;
        }
    }
}
