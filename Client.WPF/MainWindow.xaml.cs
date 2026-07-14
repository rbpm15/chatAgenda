using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;

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

        public MainWindow()
        {
            InitializeComponent();

            // Posicionar en el lado derecho de la pantalla
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

                _notifyIcon.DoubleClick += (_, _) => RestoreWindow();
                _notifyIcon.Click       += (s, e) =>
                {
                    if (e is System.Windows.Forms.MouseEventArgs me &&
                        me.Button == System.Windows.Forms.MouseButtons.Left)
                        RestoreWindow();
                };

                var menu = new System.Windows.Forms.ContextMenuStrip();
                menu.Items.Add("📱 Abrir ChatAgenda",      null, (_, _) => RestoreWindow());
                menu.Items.Add("⚙️ Cambiar servidor",      null, (_, _) => Dispatcher.Invoke(ShowSetupOverlay));
                menu.Items.Add("-");
                menu.Items.Add("❌ Salir",                  null, (_, _) => Dispatcher.Invoke(ExitApplication));
                _notifyIcon.ContextMenuStrip = menu;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error initializing system tray: " + ex.Message);
            }
        }

        private void RestoreWindow()
        {
            Dispatcher.Invoke(() =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            });
        }

        private void ExitApplication()
        {
            _isExplicitClose = true;
            Close();
        }

        // ─── Carga inicial ────────────────────────────────────────────
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Inicializando...";

                await WebView.EnsureCoreWebView2Async(null);

                // Permiso de notificaciones
                WebView.CoreWebView2.PermissionRequested += (s, args) =>
                {
                    if (args.PermissionKind == Microsoft.Web.WebView2.Core.CoreWebView2PermissionKind.Notifications)
                    {
                        args.State   = Microsoft.Web.WebView2.Core.CoreWebView2PermissionState.Allow;
                        args.Handled = true;
                    }
                };

                WebView.CoreWebView2.Settings.IsWebMessageEnabled = true;

                // Mensajes desde JS para notificaciones
                WebView.CoreWebView2.WebMessageReceived += (s, args) =>
                {
                    try
                    {
                        var msg = System.Text.Json.JsonSerializer.Deserialize<WebViewMessage>(
                            args.TryGetWebMessageAsString(),
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (msg != null && string.Equals(msg.Type, "notification", StringComparison.OrdinalIgnoreCase))
                            ShowTrayNotification(msg.Title, msg.Body);
                    }
                    catch { }
                };

                // Evento de navegación
                WebView.CoreWebView2.NavigationCompleted += async (s, args) =>
                {
                    if (args.IsSuccess)
                    {
                        _navigationFailureCount = 0;
                        StatusText.Text = $"Conectado a: {_serverUrl}";
                    }
                    else
                    {
                        _navigationFailureCount++;
                        StatusText.Text = "Reconectando...";

                        if (_navigationFailureCount <= MaxNavigationFailures && !string.IsNullOrEmpty(_serverUrl))
                        {
                            await System.Threading.Tasks.Task.Delay(2000);
                            try { WebView.CoreWebView2.Navigate(_serverUrl); } catch { }
                        }
                        else
                        {
                            StatusText.Text = "Sin conexión";
                            ShowSetupOverlay();
                            ShowError($"No se pudo conectar a {_serverUrl}\nVerifica que el servidor esté activo.");
                        }
                    }
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
            StatusText.Text         = $"Conectando a {url}...";
            TxtError.Visibility     = Visibility.Collapsed;
            _navigationFailureCount = 0;

            if (WebView.CoreWebView2 != null)
                WebView.CoreWebView2.Navigate(url);
            else
                WebView.Source = new Uri(url);
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
        private void ShowTrayNotification(string title, string body)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    string safeTitle = string.IsNullOrWhiteSpace(title) ? "ChatAgenda" : title;
                    string safeBody  = string.IsNullOrWhiteSpace(body)  ? "Nuevo mensaje" : body;

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

        private void BtnChangeMode_Click(object sender, RoutedEventArgs e)
        {
            if (WebView.CoreWebView2 != null)
                WebView.CoreWebView2.Navigate("about:blank");
            ShowSetupOverlay();
            StatusText.Text = "Cambiar servidor...";
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
        private class WebViewMessage
        {
            public string Type  { get; set; } = "";
            public string Title { get; set; } = "";
            public string Body  { get; set; } = "";
        }
    }
}
