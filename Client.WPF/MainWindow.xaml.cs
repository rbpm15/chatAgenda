using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;
using CefSharp;

namespace EmpresaChat
{
    public partial class MainWindow : Window
    {
        private const string ConfigFileName = "client_config.txt";
        private string _serverUrl = "";
        private int _navigationFailureCount = 0;
        private const int MaxNavigationFailures = 3;

        private System.Windows.Forms.NotifyIcon? _notifyIcon;
        private bool _isExplicitClose = false;
        private string _lastNotificationSource = "";

        public MainWindow()
        {
            InitializeComponent();

            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Width  = 420;
            this.Height = 820;
            this.Left   = SystemParameters.WorkArea.Width - this.Width - 15;
            this.Top    = (SystemParameters.WorkArea.Height - this.Height) / 2;

            Loaded += MainWindow_Loaded;
            InitializeSystemTray();
        }

        // ─── Tray ─────────────────────────────────────────────────────
        private void InitializeSystemTray()
        {
            try
            {
                _notifyIcon = new System.Windows.Forms.NotifyIcon();

                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                _notifyIcon.Icon = File.Exists(iconPath)
                    ? new System.Drawing.Icon(iconPath)
                    : System.Drawing.SystemIcons.Application;

                _notifyIcon.Text    = "ChatAgenda — Cliente";
                _notifyIcon.Visible = true;

                _notifyIcon.DoubleClick += (_, _) => RestoreWindow(false);
                _notifyIcon.BalloonTipClicked += (_, _) => RestoreWindow(true);
                _notifyIcon.Click       += (s, e) =>
                {
                    if (e is System.Windows.Forms.MouseEventArgs me &&
                        me.Button == System.Windows.Forms.MouseButtons.Left)
                        RestoreWindow(false);
                };

                var menu = new System.Windows.Forms.ContextMenuStrip();
                menu.Items.Add("📱 Abrir ChatAgenda",      null, (_, _) => RestoreWindow(false));
                menu.Items.Add("⚙️ Configuración",         null, (_, _) => Dispatcher.Invoke(PromptChangeServer));
                menu.Items.Add("-");
                menu.Items.Add("❌ Salir",                  null, (_, _) => Dispatcher.Invoke(ExitApplication));
                _notifyIcon.ContextMenuStrip = menu;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error initializing system tray: " + ex.Message);
            }
        }

        private void RestoreWindow(bool fromNotification)
        {
            Dispatcher.Invoke(() =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();

                if (fromNotification && _lastNotificationSource == "calendar")
                {
                    try
                    {
                        WebView.ExecuteScriptAsync("if(typeof window.switchView === 'function') { window.switchView('calendar'); }");
                    }
                    catch { }
                }
            });
        }

        private void ExitApplication()
        {
            _isExplicitClose = true;
            Close();
        }

        // ─── Carga inicial ────────────────────────────────────────────
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Inicializando...";

                if (CefSharp.Cef.IsInitialized != true)
                {
                    var settings = new CefSharp.Wpf.CefSettings();
                    CefSharp.Cef.Initialize(settings);
                }

                // Mensajes desde JS para notificaciones
                WebView.JavascriptMessageReceived += (s, args) =>
                {
                    try
                    {
                        var message = args.Message?.ToString();
                        if (string.IsNullOrEmpty(message)) return;

                        var msg = System.Text.Json.JsonSerializer.Deserialize<WebViewMessage>(
                            message,
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (msg != null && string.Equals(msg.Type, "notification", StringComparison.OrdinalIgnoreCase))
                            ShowTrayNotification(msg.Title, msg.Body, msg.Source);
                    }
                    catch { }
                };

                // Evento de navegación
                WebView.FrameLoadEnd += (s, args) =>
                {
                    if (args.Frame.IsMain)
                    {
                        // Inyectar polyfill para que el cdigo JS anterior de WebView2 siga funcionando sin importar la cach
                        string polyfill = @"
                            if (typeof window.chrome === 'undefined') { window.chrome = {}; }
                            if (typeof window.chrome.webview === 'undefined') { window.chrome.webview = {}; }
                            window.chrome.webview.postMessage = function(msg) {
                                if (typeof CefSharp !== 'undefined' && CefSharp.PostMessage) {
                                    var strMsg = typeof msg === 'string' ? msg : JSON.stringify(msg);
                                    CefSharp.PostMessage(strMsg);
                                }
                            };
                        ";
                        args.Frame.ExecuteJavaScriptAsync(polyfill);

                        Dispatcher.Invoke(() =>
                        {
                            if (args.HttpStatusCode >= 200 && args.HttpStatusCode < 400 || (args.HttpStatusCode == 0 && args.Url != "about:blank"))
                            {
                                _navigationFailureCount = 0;
                                StatusText.Text = "Conectado al servidor";
                            }
                        });
                    }
                };

                WebView.LoadError += async (s, args) =>
                {
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        _navigationFailureCount++;
                        StatusText.Text = "Reconectando...";

                        if (_navigationFailureCount <= MaxNavigationFailures && !string.IsNullOrEmpty(_serverUrl))
                        {
                            await System.Threading.Tasks.Task.Delay(2000);
                            try { WebView.Address = _serverUrl; } catch { }
                        }
                        else
                        {
                            StatusText.Text = "Sin conexión";
                            ShowSetupOverlay();
                            ShowError($"No se pudo conectar a {_serverUrl}\nVerifica que el servidor esté activo.");
                        }
                    });
                };

                // Cargar configuración guardada
                if (LoadConfig() && Uri.TryCreate(_serverUrl, UriKind.Absolute, out _))
                {
                    ConnectTo(_serverUrl);
                }
                else
                {
                    ShowSetupOverlay();
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error: WebView2 no disponible";
                ShowSetupOverlay();
                MessageBox.Show(
                    $"Error al inicializar el navegador:\n{ex.Message}\n\nInstala WebView2 Runtime.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ─── Conexión ─────────────────────────────────────────────────
        private void ConnectTo(string url)
        {
            _serverUrl = url;
            SetupOverlay.Visibility = Visibility.Collapsed;
            WebView.Visibility      = Visibility.Visible;
            StatusText.Text         = "Conectando al servidor...";
            TxtError.Visibility     = Visibility.Collapsed;
            _navigationFailureCount = 0;

            WebView.Address = url;
        }

        private void ShowSetupOverlay()
        {
            SetupOverlay.Visibility = Visibility.Visible;
        }

        private void ShowError(string msg)
        {
            TxtError.Text       = msg;
            TxtError.Visibility = Visibility.Visible;
        }

        // ─── Config ───────────────────────────────────────────────────
        private bool LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigFileName)) return false;
                foreach (var line in File.ReadAllLines(ConfigFileName))
                {
                    if (line.StartsWith("URL="))
                        _serverUrl = line.Substring(4).Trim();
                }
                return !string.IsNullOrEmpty(_serverUrl);
            }
            catch { return false; }
        }

        private void SaveConfig(string url)
        {
            try { File.WriteAllLines(ConfigFileName, new[] { $"URL={url}" }); }
            catch { }
        }

        // ─── Notificaciones ───────────────────────────────────────────
        private void ShowTrayNotification(string title, string body, string source = "")
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    _lastNotificationSource = source;
                    string safeTitle = string.IsNullOrWhiteSpace(title) ? "ChatAgenda" : title;
                    string safeBody  = string.IsNullOrWhiteSpace(body)  ? "Nuevo mensaje" : body;

                    if (string.IsNullOrEmpty(source))
                    {
                        if (safeTitle.Contains("📅") || safeTitle.Contains("🔔") || safeTitle.Contains("Evento"))
                            _lastNotificationSource = "calendar";
                    }

                    if (_notifyIcon != null)
                    {
                        _notifyIcon.BalloonTipTitle = safeTitle;
                        _notifyIcon.BalloonTipText  = safeBody;
                        _notifyIcon.ShowBalloonTip(5000);
                    }
                }
                catch { }
            });
        }

        // ─── Handlers ─────────────────────────────────────────────────
        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            string input = ClientIpInput.Text.Trim();
            if (!input.StartsWith("http://") && !input.StartsWith("https://"))
                input = "http://" + input;

            if (!Uri.TryCreate(input, UriKind.Absolute, out _))
            {
                ShowError("Dirección inválida. Ejemplo: http://192.168.1.10:5002");
                return;
            }

            SaveConfig(input);
            ConnectTo(input);
        }

        private void PromptChangeServer()
        {
            var result = MessageBox.Show(
                "¿Estás seguro que deseas cambiar la configuración de red y desconectarte del servidor actual?",
                "Configuración", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                WebView.Address = "about:blank";
                ShowSetupOverlay();
                StatusText.Text = "Cambiar servidor...";
            }
        }

        private void BtnChangeMode_Click(object sender, RoutedEventArgs e)
        {
            PromptChangeServer();
        }

        // ─── Cierre ───────────────────────────────────────────────────
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExplicitClose)
            {
                e.Cancel    = true;
                WindowState = WindowState.Minimized;
                Hide();
                _notifyIcon?.ShowBalloonTip(2000, "ChatAgenda",
                    "La app sigue activa en segundo plano.", System.Windows.Forms.ToolTipIcon.Info);
            }
            else
            {
                _notifyIcon?.Dispose();
                base.OnClosing(e);
            }
        }

        // ─── Clases auxiliares ────────────────────────────────────────
        public class WebViewMessage
        {
            public string Type  { get; set; } = "";
            public string Title { get; set; } = "";
            public string Body  { get; set; } = "";
            public string Source { get; set; } = "";
        }
    }
}
