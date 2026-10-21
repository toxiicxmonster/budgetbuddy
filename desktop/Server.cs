using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace BudgetBuddy
{
    /// <summary>
    /// The same local helper as app\server.ps1: serves budget.html and saves your data to data\budget-data.json.
    /// Only listens on localhost; nothing leaves this computer.
    /// </summary>
    sealed class Server
    {
        static readonly Encoding Utf8 = new UTF8Encoding(false);

        readonly string htmlFile, dataDir, dataFile, backupDir, stmtDir;
        readonly HttpListener listener;
        public string Url { get; }
        /// <summary>The page's "Close app" button (POST /api/quit).</summary>
        public event Action QuitRequested;

        public Server(string htmlFile, string dataDir)
        {
            this.htmlFile = htmlFile;
            this.dataDir = dataDir;
            dataFile = Path.Combine(dataDir, "budget-data.json");
            backupDir = Path.Combine(dataDir, "backups");
            stmtDir = Path.Combine(dataDir, "statements");
            Directory.CreateDirectory(backupDir);

            for (int port = 8765; port <= 8790 && listener == null; port++)
            {
                var l = new HttpListener();
                l.Prefixes.Add($"http://localhost:{port}/");
                try { l.Start(); listener = l; Url = $"http://localhost:{port}/"; }
                catch (HttpListenerException) { l.Close(); }
            }
            if (listener == null) throw new Exception("No free port between 8765 and 8790.");
            new Thread(Loop) { IsBackground = true, Name = "Budget Buddy helper" }.Start();
        }

        public void Stop()
        {
            try { listener.Stop(); listener.Close(); } catch { }
        }

        /// <summary>Another Budget Buddy (this exe or Start Budget Buddy.bat) already serving this data folder, or null.</summary>
        public static string FindRunning(string dataDir)
        {
            string want = Path.Combine(dataDir, "budget-data.json");
            // All ports at once: on Windows a closed localhost port can take a second to refuse.
            return Enumerable.Range(8765, 26).AsParallel().WithDegreeOfParallelism(26).Select(port =>
            {
                try
                {
                    var req = (HttpWebRequest)WebRequest.Create($"http://127.0.0.1:{port}/api/info");
                    req.Timeout = 1500;
                    req.Proxy = null;
                    req.Host = $"localhost:{port}";
                    using (var res = req.GetResponse())
                    using (var r = new StreamReader(res.GetResponseStream(), Utf8))
                    {
                        var m = Regex.Match(r.ReadToEnd(), "\"dataFile\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
                        if (m.Success && string.Equals(Regex.Unescape(m.Groups[1].Value), want, StringComparison.OrdinalIgnoreCase))
                            return $"http://localhost:{port}/";
                    }
                }
                catch { }
                return null;
            }).FirstOrDefault(u => u != null);
        }

        void Loop()
        {
            while (listener.IsListening)
            {
                HttpListenerContext ctx;
                try { ctx = listener.GetContext(); } catch { break; }
                try { Handle(ctx); }
                catch (Exception ex)
                {
                    try { Send(ctx, 500, "text/plain", ex.Message); } catch { }
                }
            }
        }

        void Handle(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            string path = req.Url.AbsolutePath, method = req.HttpMethod;
            bool post = method == "POST";
            // Custom header blocks other websites from writing here (forces a CORS preflight we never approve).
            if (post && req.Headers["X-Budget"] != "1") { Send(ctx, 403, "text/plain", "forbidden"); return; }

            if (method == "GET" && (path == "/" || path == "/index.html" || path == "/budget.html"))
                Send(ctx, 200, "text/html; charset=utf-8", File.ReadAllBytes(htmlFile));
            else if (method == "GET" && path == "/api/data")
                Send(ctx, 200, "application/json; charset=utf-8", File.Exists(dataFile) ? File.ReadAllBytes(dataFile) : new byte[0]);
            else if (method == "GET" && path == "/api/info")
                Send(ctx, 200, "application/json; charset=utf-8", $"{{\"dataFile\":{Json(dataFile)},\"backups\":{Json(backupDir)},\"desktop\":true}}");
            else if (post && path == "/api/data")
                SaveData(ctx);
            else if (path == "/api/statement" || path == "/api/statement-delete")
                Statement(ctx, path, method);
            else if (post && path == "/api/purge-backups")
            {
                // After a password is set: remove backups that were saved before it (not encrypted).
                var old = Directory.GetFiles(backupDir, "*.json").Where(f => !File.ReadAllText(f, Utf8).Contains("pocketBudgetEncrypted")).ToList();
                old.ForEach(File.Delete);
                Send(ctx, 200, "text/plain", old.Count.ToString());
            }
            else if (post && path == "/api/quit")
            {
                Send(ctx, 200, "text/plain", "bye");
                QuitRequested?.Invoke();
            }
            else Send(ctx, 404, "text/plain", "not found");
        }

        void SaveData(HttpListenerContext ctx)
        {
            string body = ReadBody(ctx.Request);
            if (!body.TrimStart().StartsWith("{")) { Send(ctx, 400, "text/plain", "bad data"); return; }

            // One backup per day, keep the newest 30.
            string backup = Path.Combine(backupDir, "budget-data-" + DateTime.Now.ToString("yyyy-MM-dd") + ".json");
            if (File.Exists(dataFile) && !File.Exists(backup))
            {
                File.Copy(dataFile, backup);
                foreach (var f in Directory.GetFiles(backupDir, "budget-data-*.json").OrderByDescending(Path.GetFileName, StringComparer.Ordinal).Skip(30))
                    File.Delete(f);
            }

            // Write to a temp file first so a yanked drive can't leave a half-written data file.
            string tmp = dataFile + ".tmp";
            File.WriteAllText(tmp, body, Utf8);
            File.Copy(tmp, dataFile, true);
            File.Delete(tmp);
            Send(ctx, 200, "text/plain", "ok");
        }

        /// <summary>Statement archive: data\statements\YYYY-MM\&lt;file&gt;. Paths are validated so nothing escapes that folder.</summary>
        void Statement(HttpListenerContext ctx, string path, string method)
        {
            var q = ctx.Request.QueryString;
            if (method == "POST" && path == "/api/statement")
            {
                string month = q["month"] ?? "";
                if (!Regex.IsMatch(month, @"^\d{4}-\d{2}$")) { Send(ctx, 400, "text/plain", "bad month"); return; }
                string name = Regex.Replace(q["name"] ?? "", @"[^\w.\- ()]", "_").Trim(' ', '.');
                if (name == "") name = "statement.csv";
                string dir = Path.Combine(stmtDir, month);
                Directory.CreateDirectory(dir);
                string stem = Path.GetFileNameWithoutExtension(name), ext = Path.GetExtension(name);
                string file = Path.Combine(dir, name);
                for (int n = 2; File.Exists(file); n++) file = Path.Combine(dir, $"{stem} ({n}){ext}");
                File.WriteAllText(file, ReadBody(ctx.Request), Utf8);
                Send(ctx, 200, "text/plain", month + "/" + Path.GetFileName(file));
                return;
            }
            string rel = q["path"] ?? "";
            if (!Regex.IsMatch(rel, @"^\d{4}-\d{2}/[^/\\:*?""<>|]+$") || rel.Contains("..")) { Send(ctx, 400, "text/plain", "bad path"); return; }
            string full = Path.Combine(stmtDir, rel.Replace('/', '\\'));
            if (!File.Exists(full)) { Send(ctx, 404, "text/plain", "not found"); return; }
            if (method == "GET" && path == "/api/statement") Send(ctx, 200, "text/plain; charset=utf-8", File.ReadAllBytes(full));
            else if (method == "POST") { File.Delete(full); Send(ctx, 200, "text/plain", "ok"); }
            else Send(ctx, 405, "text/plain", "method");
        }

        static string ReadBody(HttpListenerRequest req)
        {
            using (var r = new StreamReader(req.InputStream, Utf8)) return r.ReadToEnd();
        }

        static string Json(string s) =>
            "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        static void Send(HttpListenerContext ctx, int code, string type, string text) => Send(ctx, code, type, Utf8.GetBytes(text));

        static void Send(HttpListenerContext ctx, int code, string type, byte[] bytes)
        {
            var res = ctx.Response;
            res.StatusCode = code;
            res.ContentType = type;
            res.Headers.Add("Cache-Control", "no-store");
            res.ContentLength64 = bytes.Length;
            if (bytes.Length > 0) res.OutputStream.Write(bytes, 0, bytes.Length);
            res.OutputStream.Close();
        }
    }
}
