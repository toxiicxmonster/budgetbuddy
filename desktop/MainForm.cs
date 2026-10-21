using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace BudgetBuddy
{
    /// <summary>The Budget Buddy window: the app page in a WebView2 (the Edge engine built into Windows).</summary>
    sealed class MainForm : Form
    {
        /// <summary>This computer's settings for the window (size, web cache). Your budget data stays in the data folder.</summary>
        public static readonly string LocalDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BudgetBuddy");
        static readonly string BoundsFile = Path.Combine(LocalDir, "window.txt");

        readonly Server server;
        readonly WebView2 web = new WebView2 { Dock = DockStyle.Fill };
        bool canClose, asked;
        readonly Timer noAnswer = new Timer { Interval = 3000 };

        public static void SetLoaderFolder(string dir) => CoreWebView2Environment.SetLoaderDllFolderPath(dir);

        public MainForm(Server server)
        {
            this.server = server;
            Text = "Budget Buddy";
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            MinimumSize = new Size(380, 480);
            BackColor = Color.FromArgb(0xf4, 0xf5, 0xf2);
            RestoreBounds_();
            Controls.Add(web);

            server.QuitRequested += () => BeginInvoke((Action)(() => { canClose = true; Close(); }));
            // The page didn't answer (stuck?): close anyway rather than trap the user.
            noAnswer.Tick += (s, e) => { noAnswer.Stop(); canClose = true; Close(); };
            Load += async (s, e) => await StartWeb();
        }

        async System.Threading.Tasks.Task StartWeb()
        {
            try
            {
                var env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(LocalDir, "WebView2"));
                await web.EnsureCoreWebView2Async(env);
            }
            catch (WebView2RuntimeNotFoundException)
            {
                if (MessageBox.Show("Budget Buddy needs the Microsoft Edge WebView2 Runtime, which is missing on this PC.\n\nOpen the download page now?",
                        "Budget Buddy", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    OpenOutside("https://developer.microsoft.com/microsoft-edge/webview2/");
                canClose = true; Close(); return;
            }

            var core = web.CoreWebView2;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.AreDevToolsEnabled = Environment.GetEnvironmentVariable("BUDGET_DEVTOOLS") == "1";
            core.Settings.IsGeneralAutofillEnabled = false;
            core.Settings.IsPasswordAutosaveEnabled = false;
            core.DocumentTitleChanged += (s, e) => Text = string.IsNullOrEmpty(core.DocumentTitle) ? "Budget Buddy" : core.DocumentTitle;
            core.WebMessageReceived += OnMessage;
            // Links to other websites open in your normal browser, never inside the app.
            core.NewWindowRequested += (s, e) => { e.Handled = true; if (!Ours(e.Uri)) OpenOutside(e.Uri); };
            core.NavigationStarting += (s, e) => { if (!Ours(e.Uri) && e.Uri.StartsWith("http", StringComparison.OrdinalIgnoreCase)) { e.Cancel = true; OpenOutside(e.Uri); } };
            web.Source = new Uri(server.Url);
        }

        bool Ours(string uri) => uri.StartsWith(server.Url, StringComparison.OrdinalIgnoreCase);

        static void OpenOutside(string uri)
        {
            if (!uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return;
            try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); } catch { }
        }

        void OnMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string msg;
            try { msg = e.TryGetWebMessageAsString(); } catch { return; }
            if (msg == "closing") noAnswer.Stop();                      // the page is saving; it may also ask a question
            else if (msg == "stay") { noAnswer.Stop(); asked = false; }
            else if (msg == "close") { noAnswer.Stop(); canClose = true; Close(); }
        }

        /// <summary>Closing the window: let the page finish saving first (it answers "close", or "stay" if you cancel).</summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!canClose && e.CloseReason == CloseReason.UserClosing && web.CoreWebView2 != null)
            {
                e.Cancel = true;
                if (!asked)
                {
                    asked = true;
                    noAnswer.Start();
                    _ = web.CoreWebView2.ExecuteScriptAsync("typeof bbDesktopClose === 'function' ? bbDesktopClose() : chrome.webview.postMessage('close')");
                }
                return;
            }
            SaveBounds();
            base.OnFormClosing(e);
        }

        void RestoreBounds_()
        {
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1280, 860);
            try
            {
                var p = File.ReadAllText(BoundsFile).Split(',').Select(int.Parse).ToArray();
                var r = new Rectangle(p[0], p[1], p[2], p[3]);
                // Only if it's still on a screen (the drive may be on another PC now).
                if (Screen.AllScreens.Any(sc => sc.WorkingArea.IntersectsWith(r) && r.Width >= MinimumSize.Width))
                {
                    StartPosition = FormStartPosition.Manual;
                    Bounds = r;
                    if (p[4] == 1) WindowState = FormWindowState.Maximized;
                }
            }
            catch { }
        }

        void SaveBounds()
        {
            try
            {
                var r = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                Directory.CreateDirectory(LocalDir);
                File.WriteAllText(BoundsFile, $"{r.X},{r.Y},{r.Width},{r.Height},{(WindowState == FormWindowState.Maximized ? 1 : 0)}");
            }
            catch { }
        }
    }
}
