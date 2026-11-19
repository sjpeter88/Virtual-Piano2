using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace ConcertGrandShell;

internal static class Program
{
    public const string AppVersion = "v1.1.0";
    public const string EmbeddedOriginUrl = "https://concertgrand.app/index.html";
    public const string LiveGitHubUrl = "https://sjpeter88.github.io/Virtual-Piano2/";

    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        bool kiosk = args.Any(a => a.Equals("--kiosk", StringComparison.OrdinalIgnoreCase));
        Application.Run(new MainForm(kiosk));
    }
}

/// <summary>Remembered window position, size and maximized state (Option 3A).</summary>
public sealed class WindowState_
{
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Maximized { get; set; }
}

public sealed class MainForm : Form
{
    private readonly WebView2 _webView;
    private bool _usingLiveGitHub = false;
    private bool _isFullscreen = false;
    private FormWindowState _prevWindowState;
    private FormBorderStyle _prevBorderStyle;
    private Rectangle _prevBounds;

    private readonly bool _kiosk;

    private static string AppDataDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ConcertGrandPiano");
    private static string WindowStatePath => Path.Combine(AppDataDir, "window-state.json");

    public MainForm(bool kiosk = false)
    {
        _kiosk = kiosk;
        Text = $"Concert Grand 3D Virtual Piano {Program.AppVersion} — Offline Embedded (Press F11 Fullscreen · Ctrl+Shift+G Switch Source)";
        Width = 1480;
        Height = 880;
        MinimumSize = new Size(960, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(14, 11, 9);
        KeyPreview = true;
        try { Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? Application.ExecutablePath) ?? CreatePianoAppIcon(); }
        catch { Icon = CreatePianoAppIcon(); }

        RestoreWindowState();
        FormClosing += (_, _) => SaveWindowState();

        _webView = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = Color.FromArgb(14, 11, 9)
        };
        Controls.Add(_webView);

        Load += async (_, _) =>
        {
            if (_kiosk) ToggleFullscreen();
            await InitializeWebViewAsync();
        };
        KeyDown += OnFormKeyDown;
    }

    private void RestoreWindowState()
    {
        try
        {
            if (!File.Exists(WindowStatePath)) return;
            var st = System.Text.Json.JsonSerializer.Deserialize<WindowState_>(File.ReadAllText(WindowStatePath));
            if (st == null) return;
            var bounds = new Rectangle(st.Left, st.Top, Math.Max(st.Width, MinimumSize.Width), Math.Max(st.Height, MinimumSize.Height));
            // Only restore if the saved rectangle is still visible on a connected monitor
            if (!Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(bounds))) return;
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
            if (st.Maximized) WindowState = FormWindowState.Maximized;
        }
        catch { /* corrupt or unreadable state file: fall back to defaults */ }
    }

    private void SaveWindowState()
    {
        if (_kiosk) return;
        try
        {
            // While fullscreen, save the pre-fullscreen window rather than the screen-sized one
            bool max = _isFullscreen ? _prevWindowState == FormWindowState.Maximized : WindowState == FormWindowState.Maximized;
            Rectangle b = _isFullscreen ? _prevBounds : (WindowState == FormWindowState.Normal ? Bounds : RestoreBounds);
            Directory.CreateDirectory(AppDataDir);
            File.WriteAllText(WindowStatePath, System.Text.Json.JsonSerializer.Serialize(
                new WindowState_ { Left = b.Left, Top = b.Top, Width = b.Width, Height = b.Height, Maximized = max }));
        }
        catch { /* non-fatal */ }
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F11 || (e.Alt && e.KeyCode == Keys.Enter))
        {
            e.Handled = true;
            ToggleFullscreen();
        }
        else if (e.Control && e.Shift && e.KeyCode == Keys.G)
        {
            e.Handled = true;
            ToggleAppSource();
        }
    }

    private void ToggleFullscreen()
    {
        if (!_isFullscreen)
        {
            _prevWindowState = WindowState;
            _prevBorderStyle = FormBorderStyle;
            _prevBounds = Bounds;

            WindowState = FormWindowState.Normal;
            FormBorderStyle = FormBorderStyle.None;
            Bounds = Screen.FromControl(this).Bounds;
            _isFullscreen = true;
        }
        else
        {
            FormBorderStyle = _prevBorderStyle;
            WindowState = _prevWindowState;
            Bounds = _prevBounds;
            _isFullscreen = false;
        }
    }

    private void ToggleAppSource()
    {
        if (_webView.CoreWebView2 == null) return;
        _usingLiveGitHub = !_usingLiveGitHub;
        string targetUrl = _usingLiveGitHub ? Program.LiveGitHubUrl : Program.EmbeddedOriginUrl;
        UpdateWindowTitle();
        _webView.CoreWebView2.Navigate(targetUrl);
    }

    private void UpdateWindowTitle()
    {
        string modeLabel = _usingLiveGitHub ? "Live GitHub Pages" : "Offline Embedded";
        Text = $"Concert Grand 3D Virtual Piano {Program.AppVersion} — {modeLabel} (F11 Fullscreen · Ctrl+Shift+G Switch Source)";
    }

    private static Icon CreatePianoAppIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(20, 14, 12));

        using var goldPen = new Pen(Color.FromArgb(216, 181, 120), 2);
        g.DrawRectangle(goldPen, 1, 1, 29, 29);

        // 4 Ivory white keys
        using var ivoryBrush = new SolidBrush(Color.FromArgb(245, 240, 228));
        using var keyGapPen = new Pen(Color.FromArgb(30, 24, 20), 1);
        for (int i = 0; i < 4; i++)
        {
            int kx = 3 + i * 7;
            g.FillRectangle(ivoryBrush, kx, 6, 6, 22);
            g.DrawRectangle(keyGapPen, kx, 6, 6, 22);
        }

        // Red felt strip along top of keys
        using var redBrush = new SolidBrush(Color.FromArgb(165, 24, 34));
        g.FillRectangle(redBrush, 3, 4, 26, 2);

        // 3 Ebony black keys
        using var ebonyBrush = new SolidBrush(Color.FromArgb(22, 20, 24));
        int[] blackX = { 8, 15, 22 };
        foreach (int bx in blackX)
        {
            g.FillRectangle(ebonyBrush, bx, 6, 4, 13);
        }

        return Icon.FromHandle(bmp.GetHicon());
    }

    private async System.Threading.Tasks.Task InitializeWebViewAsync()
    {
        try
        {
            string userDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ConcertGrandPiano",
                "WebView2Data"
            );

            // Enable zero-click instant audio autoplay for the AudioWorklet sound engine
            var options = new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required");
            var env = await CoreWebView2Environment.CreateAsync(null, userDataDir, options);
            await _webView.EnsureCoreWebView2Async(env);

            var core = _webView.CoreWebView2;
            core.Settings.IsZoomControlEnabled = false;
            core.Settings.IsPinchZoomEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.IsStatusBarEnabled = false;

            // Auto-grant Web MIDI permissions so hardware MIDI keyboards work immediately
            core.PermissionRequested += (_, args) =>
            {
                if (args.PermissionKind == CoreWebView2PermissionKind.MidiSystemExclusiveMessages ||
                    args.PermissionKind == CoreWebView2PermissionKind.Autoplay)
                {
                    args.State = CoreWebView2PermissionState.Allow;
                }
            };

            // Listen for messages from the web UI (e.g. clicking the "App Source" toggle button in the View menu)
            core.WebMessageReceived += (_, args) =>
            {
                string msg = args.TryGetWebMessageAsString();
                if (msg == "toggle-source")
                {
                    ToggleAppSource();
                }
                else if (msg == "toggle-fullscreen")
                {
                    ToggleFullscreen();
                }
                else if (msg == "kiosk-minimize")
                {
                    WindowState = FormWindowState.Minimized;
                }
                else if (msg == "kiosk-windowed")
                {
                    if (_isFullscreen) ToggleFullscreen();
                }
                else if (msg == "kiosk-exit")
                {
                    Close();
                }
            };

            // Locked desktop chrome (Option 4C): no swipe back/forward, no navigating away to other sites.
            // DevTools (F12 / Ctrl+Shift+I) intentionally stay available.
            core.Settings.IsSwipeNavigationEnabled = false;
            core.NavigationStarting += (_, args) =>
            {
                if (!Uri.TryCreate(args.Uri, UriKind.Absolute, out var u)) { args.Cancel = true; return; }
                bool allowed = u.Host.Equals("concertgrand.app", StringComparison.OrdinalIgnoreCase)
                            || u.Host.Equals("sjpeter88.github.io", StringComparison.OrdinalIgnoreCase)
                            || u.Scheme is "blob" or "data" or "about";
                if (!allowed) args.Cancel = true;
            };
            core.NewWindowRequested += (_, args) => args.Handled = true; // no pop-up browser windows

            // Native Windows "Save As" dialog for .wav recordings (Option 4E) and any other page download
            core.DownloadStarting += (_, args) =>
            {
                string suggested = Path.GetFileName(args.ResultFilePath);
                using var dlg = new SaveFileDialog
                {
                    Title = "Save recording",
                    FileName = suggested,
                    Filter = suggested.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
                        ? "WAV audio (*.wav)|*.wav|All files (*.*)|*.*"
                        : "All files (*.*)|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                    OverwritePrompt = true
                };
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    args.ResultFilePath = dlg.FileName;
                    args.Handled = true; // suppress WebView2's default download bubble
                }
                else
                {
                    args.Cancel = true;
                }
            };

            // Intercept https://concertgrand.app/* and serve embedded resources directly from the single .exe binary
            core.AddWebResourceRequestedFilter("https://concertgrand.app/*", CoreWebView2WebResourceContext.All);
            core.WebResourceRequested += OnWebResourceRequested;

            // Handle keyboard shortcuts when focus is inside the WebView2 control
            core.NavigationCompleted += (_, _) =>
            {
                core.ExecuteScriptAsync(@"
                    if (!window.__shellHooked) {
                      window.__shellHooked = true;
                      window.addEventListener('keydown', (e) => {
                        if (e.key === 'F11' || (e.altKey && e.key === 'Enter')) {
                            e.preventDefault();
                            window.chrome.webview.postMessage('toggle-fullscreen');
                        } else if (e.ctrlKey && e.shiftKey && (e.key === 'G' || e.key === 'g')) {
                            e.preventDefault();
                            window.chrome.webview.postMessage('toggle-source');
                        } else if (e.altKey && (e.key === 'ArrowLeft' || e.key === 'ArrowRight')) {
                            e.preventDefault(); // block browser back/forward
                        } else if (e.key === 'BrowserBack' || e.key === 'BrowserForward') {
                            e.preventDefault();
                        }
                      }, true);
                      // Block mouse back/forward side buttons
                      window.addEventListener('mouseup', (e) => {
                        if (e.button === 3 || e.button === 4) e.preventDefault();
                      }, true);
                    }
                ");
                if (_kiosk) InjectKioskPill(core);
            };

            core.Navigate(Program.EmbeddedOriginUrl);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to initialize WebView2 runtime:\n{ex.Message}",
                $"Concert Grand {Program.AppVersion}",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    // Kiosk mode (Option 3B): floating top-right pill with Minimize / Windowed / Exit
    private static void InjectKioskPill(CoreWebView2 core)
    {
        core.ExecuteScriptAsync(@"
            if (!document.getElementById('kiosk-pill')) {
              const pill = document.createElement('div');
              pill.id = 'kiosk-pill';
              pill.style.cssText = 'position:fixed;top:8px;right:10px;z-index:99999;display:flex;gap:4px;padding:3px;border-radius:14px;background:rgba(20,14,12,0.72);border:1px solid rgba(216,181,120,0.5);opacity:0.35;transition:opacity .2s;font:12px sans-serif;';
              pill.onmouseenter = () => pill.style.opacity = 1;
              pill.onmouseleave = () => pill.style.opacity = 0.35;
              [['–','kiosk-minimize','Minimize'],['□','kiosk-windowed','Windowed'],['✕','kiosk-exit','Exit']].forEach(([t,m,tip]) => {
                const b = document.createElement('button');
                b.textContent = t; b.title = tip;
                b.style.cssText = 'width:24px;height:22px;border:0;border-radius:11px;background:transparent;color:#f5f0e4;cursor:pointer;';
                b.onclick = () => window.chrome.webview.postMessage(m);
                pill.appendChild(b);
              });
              document.body.appendChild(pill);
            }
        ");
    }

    private void OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        if (_webView.CoreWebView2 == null) return;

        var uri = new Uri(e.Request.Uri);
        string path = uri.AbsolutePath.TrimStart('/').ToLowerInvariant();
        if (string.IsNullOrEmpty(path)) path = "index.html";

        string? resourceName = null;
        string contentType = "text/plain; charset=utf-8";

        if (path == "index.html")
        {
            resourceName = "ConcertGrand.index.html";
            contentType = "text/html; charset=utf-8";
        }
        else if (path == "lib/three.module.min.js")
        {
            resourceName = "ConcertGrand.lib.three.module.min.js";
            contentType = "application/javascript; charset=utf-8";
        }

        if (resourceName != null)
        {
            var asm = Assembly.GetExecutingAssembly();
            Stream? resStream = asm.GetManifestResourceStream(resourceName);
            if (resStream != null)
            {
                var mem = new MemoryStream();
                resStream.CopyTo(mem);
                resStream.Dispose();
                mem.Position = 0;

                string headers = $"Content-Type: {contentType}\r\nAccess-Control-Allow-Origin: *\r\nCache-Control: no-cache";
                e.Response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                    mem, 200, "OK", headers
                );
                return;
            }
        }

        e.Response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
            null, 404, "Not Found", "Content-Type: text/plain"
        );
    }
}
