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
        private const string ConfigFileName = "server_config.txt";
        private string _serverUrl = "";
        private string _mode = ""; // "SERVER" or "CLIENT"
        private Process _serverProcess = null;
        private int _navigationFailureCount = 0;
        private const int MaxNavigationFailures = 3;
        
        private System.Windows.Forms.NotifyIcon? _notifyIcon;
        private bool _isExplicitClose = false;

        public MainWindow()
        {
            InitializeComponent();
            
            // Posicionar la ventana en el lado derecho de la pantalla
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Width = 420;
            this.Height = 820;
            this.Left = SystemParameters.WorkArea.Width - this.Width - 15;
            this.Top = (SystemParameters.WorkArea.Height - this.Height) / 2;

            ModeSelectionOverlay.Visibility = Visibility.Visible;
            StatusText.Text = "Cargando...";

            Loaded += MainWindow_Loaded;
            InitializeSystemTray();
        }

        private void InitializeSystemTray()
        {
            try
            {
                _notifyIcon = new System.Windows.Forms.NotifyIcon();

                // Cargar icono desde archivo
                try
                {
                    string iconPath = System.IO.Path.Combine(
                        System.AppDomain.CurrentDomain.BaseDirectory, 
                        "app.ico");
                    if (System.IO.File.Exists(iconPath))
                    {
                        _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                    }
                    else
                    {
                        _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                    }
                }
                catch
                {
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }

                _notifyIcon.Text = "ChatAgenda - Mensajes instantáneos";
                _notifyIcon.Visible = true;

                // Double click restores window
                _notifyIcon.DoubleClick += (s, e) => RestoreWindow();

                // Click único también restaura
                _notifyIcon.Click += (s, e) => 
                {
                    if (e is System.Windows.Forms.MouseEventArgs me && me.Button == System.Windows.Forms.MouseButtons.Left)
                    {
                        RestoreWindow();
                    }
                };

                // Context menu
                var menu = new System.Windows.Forms.ContextMenuStrip();
                menu.Items.Add("📱 Abrir ChatAgenda", null, (s, e) => RestoreWindow());
                menu.Items.Add("⚙️ Configuración", null, (s, e) => ShowSettings());
                menu.Items.Add("-"); // Separador
                menu.Items.Add("❌ Salir de la aplicación", null, (s, e) => ExitApplication());
                _notifyIcon.ContextMenuStrip = menu;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error initializing system tray: " + ex.Message);
            }
        }

        private void ShowSettings()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            // Podrías aquí abrir una pestaña de configuración específica
        }

        private void RestoreWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void ExitApplication()
        {
            _isExplicitClose = true;
            this.Close();
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (rk != null)
                    {
                        string appName = "ChatAgenda";
                        if (enable)
                        {
                            string path = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                            if (!string.IsNullOrEmpty(path))
                            {
                                rk.SetValue(appName, $"\"{path}\"");
                            }
                        }
                        else
                        {
                            rk.DeleteValue(appName, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error setting registry auto-start: " + ex.Message);
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ModeSelectionOverlay.Visibility = Visibility.Visible;
                StatusText.Text = "Inicializando la interfaz...";

                // Initialize WebView2 environment
                await WebView.EnsureCoreWebView2Async(null);

                // Auto-approve Notification Permission request
                WebView.CoreWebView2.PermissionRequested += (s, args) =>
                {
                    if (args.PermissionKind == Microsoft.Web.WebView2.Core.CoreWebView2PermissionKind.Notifications)
                    {
                        args.State = Microsoft.Web.WebView2.Core.CoreWebView2PermissionState.Allow;
                        args.Handled = true;
                    }
                };

                WebView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                WebView.CoreWebView2.AddHostObjectToScript("chatAgendaBridge", new ScriptBridge(this));

                WebView.CoreWebView2.WebMessageReceived += (s, args) =>
                {
                    try
                    {
                        var message = System.Text.Json.JsonSerializer.Deserialize<WebViewMessage>(
                            args.TryGetWebMessageAsString(),
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (message != null && string.Equals(message.Type, "notification", StringComparison.OrdinalIgnoreCase))
                        {
                            ShowBackgroundNotification(message.Title, message.Body);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error processing web notification: " + ex.Message);
                    }
                };

                // Load saved configuration
                if (LoadConfig())
                {
                    if (Uri.TryCreate(_serverUrl, UriKind.Absolute, out var savedUri))
                    {
                        // Auto-start with stored config
                        ModeSelectionOverlay.Visibility = Visibility.Collapsed;
                        WebView.Visibility = Visibility.Visible;

                        if (_mode == "SERVER")
                        {
                            ModeBadge.Text = "Modo: Servidor";
                            StatusText.Text = "Iniciando servidor local...";
                            StartLocalServer();
                        }
                        else
                        {
                            ModeBadge.Text = "Modo: Cliente LAN";
                            StatusText.Text = $"Conectando a servidor remoto {_serverUrl}...";
                        }

                        WebView.Source = savedUri;
                    }
                    else
                    {
                        ModeSelectionOverlay.Visibility = Visibility.Visible;
                        WebView.Visibility = Visibility.Collapsed;
                        StatusText.Text = "Dirección de servidor inválida. Configura la aplicación.";
                    }
                }
                else
                {
                    // No config, prompt user
                    ModeSelectionOverlay.Visibility = Visibility.Visible;
                    WebView.Visibility = Visibility.Collapsed;
                }

                // Listen to navigation events
                WebView.CoreWebView2.NavigationCompleted += async (s, args) =>
                {
                    if (args.IsSuccess)
                    {
                        _navigationFailureCount = 0;
                        StatusText.Text = $"Conectado a: {_serverUrl}";

                        // Inyectar código para inicializar el bridge
                        try
                        {
                            await WebView.CoreWebView2.ExecuteScriptAsync(@"
                                console.log('[BRIDGE] Inicializando bridge de notificaciones...');
                                window.notificationBridgeReady = true;
                                console.log('[BRIDGE] Bridge listo:', !!window.chrome?.webview?.hostObjects?.sync?.chatAgendaBridge);
                            ");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Error inyectando script de bridge: " + ex.Message);
                        }
                    }
                    else
                    {
                        _navigationFailureCount++;
                        StatusText.Text = "WebView2 no pudo cargar la página (reintentando)...";

                        if (_navigationFailureCount <= MaxNavigationFailures && !string.IsNullOrEmpty(_serverUrl))
                        {
                            await System.Threading.Tasks.Task.Delay(1500);
                            try
                            {
                                WebView.CoreWebView2.Navigate(_serverUrl);
                            }
                            catch { }
                        }
                        else
                        {
                            var errorMessage = $"No se pudo cargar la página en {_serverUrl}.\nComprueba que la aplicación del servidor está en ejecución y que WebView2 Runtime está instalado.";
                            if (args.WebErrorStatus != Microsoft.Web.WebView2.Core.CoreWebView2WebErrorStatus.Unknown)
                            {
                                errorMessage += $"\nError WebView2: {args.WebErrorStatus}";
                            }

                            MessageBox.Show(errorMessage, "Error de Navegación", MessageBoxButton.OK, MessageBoxImage.Warning);
                            StatusText.Text = "WebView2 no pudo mostrar la página.";

                            if (!string.IsNullOrEmpty(_serverUrl) && _mode == "CLIENT")
                            {
                                try
                                {
                                    Process.Start(new ProcessStartInfo(_serverUrl) { UseShellExecute = true });
                                    StatusText.Text = "Abriendo en el navegador predeterminado...";
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"No se pudo abrir el navegador: {ex.Message}");
                                }
                            }

                            ModeSelectionOverlay.Visibility = Visibility.Collapsed;
                            WebView.Source = new Uri("about:blank");
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                WebView.Visibility = Visibility.Collapsed;
                StatusText.Text = "WebView2 no está disponible. Usa la interfaz básica para configurar la app.";
                ModeSelectionOverlay.Visibility = Visibility.Visible;
                MessageBox.Show($"Error al inicializar el navegador local:\n{ex.Message}\n\nAsegúrate de tener instalado WebView2 Runtime.", "Error de Inicialización", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ShowBackgroundNotification(string title, string body)
        {
            try
            {
                var safeTitle = string.IsNullOrWhiteSpace(title) ? "ChatAgenda" : title;
                var safeBody = string.IsNullOrWhiteSpace(body) ? "Tienes un nuevo mensaje." : body;

                // Primera opción: Usar powershell para mostrar notificación toast de Windows 10/11 (funciona incluso con app minimizada)
                try
                {
                    ShowWindowsNotification(safeTitle, safeBody);
                }
                catch (Exception notifEx)
                {
                    Debug.WriteLine("Error con Windows Notifications Toast: " + notifEx.Message);

                    // Fallback: si Windows Notifications falla, usar NotifyIcon
                    if (_notifyIcon != null)
                    {
                        _notifyIcon.BalloonTipTitle = safeTitle;
                        _notifyIcon.BalloonTipText = safeBody;
                        _notifyIcon.ShowBalloonTip(5000);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error showing background notification: " + ex.Message);
            }
        }

        private void ShowWindowsNotification(string title, string body)
        {
            try
            {
                // Limpiar caracteres problemáticos para PowerShell
                var safeTitle = title.Replace("\"", "").Replace("'", "").Replace("`", "");
                var safeBody = body.Replace("\"", "").Replace("'", "").Replace("`", "");

                // Crear el template XML para la notificación toast INTERACTIVA
                // La notificación tendrá un botón para abrir la app
                string xmlTemplate = $@"<toast launch='action=openApp'>
    <visual>
        <binding template='ToastText02'>
            <text id='1'>{System.Security.SecurityElement.Escape(safeTitle)}</text>
            <text id='2'>{System.Security.SecurityElement.Escape(safeBody)}</text>
        </binding>
    </visual>
    <actions>
        <action activationType='foreground' arguments='openApp' content='Abrir'/>
        <action activationType='system' arguments='dismiss' content='Descartar'/>
    </actions>
</toast>";

                // Script PowerShell para mostrar notificación
                string psScript = $@"
Add-Type -AssemblyName System.Runtime.WindowsRuntime
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] > $null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] > $null

$APP_ID = 'ChatAgenda'

try {{
    $xml = New-Object Windows.Data.Xml.Dom.XmlDocument
    $xml.LoadXml('{xmlTemplate.Replace("'", "''").Replace("`", "``")}')
    $toast = New-Object Windows.UI.Notifications.ToastNotification $xml
    $notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier($APP_ID)
    $notifier.Show($toast)
}} catch {{
    Write-Error ""Error: $_""
}}
";

                // Ejecutar PowerShell sin ventana visible
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $@"-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -Command ""{psScript}""",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        // Esperar a que termine sin bloquear más de 5 segundos
                        bool finished = process.WaitForExit(5000);
                        if (!finished)
                        {
                            process.Kill();
                        }
                    }
                }

                Debug.WriteLine($"✓ Notificación Windows mostrada (clickeable): {safeTitle}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("✗ Error al mostrar notificación Windows: " + ex.Message);
                throw;
            }
        }

        [ComVisible(true)]
        public class ScriptBridge
        {
            private readonly MainWindow _owner;

            public ScriptBridge(MainWindow owner)
            {
                _owner = owner;
            }

            public void Notify(string title, string body)
            {
                _owner.Dispatcher.Invoke(() => _owner.ShowBackgroundNotification(title, body));
            }
        }

        private class WebViewMessage
        {
            public string Type { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Body { get; set; } = string.Empty;
        }

        private class ServerSettings
        {
            public string? CalendarId { get; set; }
            public bool IsEnabled { get; set; }
        }

        private bool LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFileName))
                {
                    string[] lines = File.ReadAllLines(ConfigFileName);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("MODE="))
                        {
                            _mode = line.Substring(5).Trim().ToUpper();
                        }
                        else if (line.StartsWith("URL="))
                        {
                            _serverUrl = line.Substring(4).Trim();
                        }
                    }

                    return !string.IsNullOrEmpty(_mode) && !string.IsNullOrEmpty(_serverUrl);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error reading config: " + ex.Message);
            }
            return false;
        }

        private void SaveConfig(string mode, string url)
        {
            try
            {
                _mode = mode;
                _serverUrl = url;
                File.WriteAllLines(ConfigFileName, new[] {
                    $"MODE={mode}",
                    $"URL={url}"
                });
                SetAutoStart(true); // Configura auto-inicio con Windows
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo guardar la configuración: {ex.Message}", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void StartLocalServer()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string exeName = "chatAgenda.exe";
                string dllName = "chatAgenda.dll";

                ProcessStartInfo psi = new ProcessStartInfo();

                // 1. Look in the same folder as the WPF executable
                if (File.Exists(Path.Combine(baseDir, exeName)))
                {
                    psi.FileName = Path.Combine(baseDir, exeName);
                    psi.WorkingDirectory = baseDir;
                }
                else if (File.Exists(Path.Combine(baseDir, dllName)))
                {
                    psi.FileName = "dotnet";
                    psi.Arguments = $"\"{Path.Combine(baseDir, dllName)}\"";
                    psi.WorkingDirectory = baseDir;
                }
                else
                {
                    // 2. Look in workspace directories (relative paths for dev)
                    // Traverse upwards to find the directory containing chatAgenda.csproj
                    string? currentDir = baseDir;
                    string? workspaceRoot = null;
                    while (currentDir != null)
                    {
                        if (File.Exists(Path.Combine(currentDir, "chatAgenda.csproj")))
                        {
                            workspaceRoot = currentDir;
                            break;
                        }
                        currentDir = Path.GetDirectoryName(currentDir);
                    }

                    if (workspaceRoot != null)
                    {
                        string debugDll = Path.Combine(workspaceRoot, "bin", "Debug", "net8.0", "chatAgenda.dll");
                        string releaseDll = Path.Combine(workspaceRoot, "bin", "Release", "net8.0", "chatAgenda.dll");

                        if (File.Exists(debugDll))
                        {
                            psi.FileName = "dotnet";
                            psi.Arguments = $"\"{debugDll}\"";
                            psi.WorkingDirectory = workspaceRoot;
                        }
                        else if (File.Exists(releaseDll))
                        {
                            psi.FileName = "dotnet";
                            psi.Arguments = $"\"{releaseDll}\"";
                            psi.WorkingDirectory = workspaceRoot;
                        }
                        else
                        {
                            string projFile = Path.Combine(workspaceRoot, "chatAgenda.csproj");
                            psi.FileName = "dotnet";
                            psi.Arguments = $"run --project \"{projFile}\" --launch-profile http";
                            psi.WorkingDirectory = workspaceRoot;
                        }
                    }
                    else
                    {
                        throw new FileNotFoundException("No se encontró el ejecutable ni el código fuente de ChatAgenda.");
                    }
                }

                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                _serverProcess = Process.Start(psi);
                StatusText.Text = "Servidor local iniciado en segundo plano.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al iniciar el servidor local:\n{ex.Message}\n\nSe intentará conectar a http://localhost:5002.", "Error del Servidor", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void StopLocalServer()
        {
            try
            {
                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    _serverProcess.Kill(true);
                    _serverProcess = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error killing server process: " + ex.Message);
            }
        }

        private void SelectServerMode_Click(object sender, MouseButtonEventArgs e)
        {
            _serverUrl = "http://localhost:5002";
            SaveConfig("SERVER", _serverUrl);
            ModeSelectionOverlay.Visibility = Visibility.Collapsed;
            ModeBadge.Text = "Modo: Servidor";
            StatusText.Text = "Iniciando servidor local...";
            StartLocalServer();
            WebView.Source = new Uri(_serverUrl);
            ServerConfigPanel.Visibility = Visibility.Visible;
            ModeCardsContainer.Visibility = Visibility.Collapsed;
            ClientSetupFields.Visibility = Visibility.Collapsed;
            LoadServerConfigIntoForm();
        }

        private void SelectClientMode_Click(object sender, RoutedEventArgs e)
        {
            ServerConfigPanel.Visibility = Visibility.Collapsed;
            ModeCardsContainer.Visibility = Visibility.Collapsed;
            ClientSetupFields.Visibility = Visibility.Visible;
        }

        private void CancelClientSetup_Click(object sender, RoutedEventArgs e)
        {
            ClientSetupFields.Visibility = Visibility.Collapsed;
            ServerConfigPanel.Visibility = Visibility.Collapsed;
            ModeCardsContainer.Visibility = Visibility.Visible;
        }

        private void CancelServerConfig_Click(object sender, RoutedEventArgs e)
        {
            ServerConfigPanel.Visibility = Visibility.Collapsed;
            ModeCardsContainer.Visibility = Visibility.Visible;
        }

        private async void SaveServerConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                var payload = new
                {
                    calendarId = ServerCalendarIdInput.Text.Trim(),
                    isEnabled = ServerSyncEnabledCheck.IsChecked == true,
                    credentialsJson = string.IsNullOrWhiteSpace(ServerCredentialsInput.Text) ? null : ServerCredentialsInput.Text.Trim()
                };

                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync("http://localhost:5002/api/settings", content);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Configuración guardada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"No se pudo guardar la configuración: {errorText}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al conectar con el servidor: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void LoadServerConfigIntoForm()
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                var response = await client.GetAsync("http://localhost:5002/api/settings");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var settings = System.Text.Json.JsonSerializer.Deserialize<ServerSettings>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (settings != null)
                    {
                        ServerCalendarIdInput.Text = settings.CalendarId ?? "primary";
                        ServerSyncEnabledCheck.IsChecked = settings.IsEnabled;
                    }
                }
            }
            catch
            {
                ServerCalendarIdInput.Text = "primary";
                ServerSyncEnabledCheck.IsChecked = false;
            }
        }

        private void ConnectAsClient_Click(object sender, RoutedEventArgs e)
        {
            string newUrl = ClientIpInput.Text.Trim();
            if (!newUrl.StartsWith("http://") && !newUrl.StartsWith("https://"))
            {
                newUrl = "http://" + newUrl;
            }

            if (Uri.TryCreate(newUrl, UriKind.Absolute, out var uri))
            {
                _serverUrl = newUrl;
                SaveConfig("CLIENT", _serverUrl);
                ModeSelectionOverlay.Visibility = Visibility.Collapsed;
                ModeBadge.Text = "Modo: Cliente LAN";
                StatusText.Text = $"Conectando a {_serverUrl}...";
                WebView.Visibility = Visibility.Visible;

                if (WebView.CoreWebView2 != null)
                {
                    WebView.CoreWebView2.Navigate(_serverUrl);
                }
                else
                {
                    WebView.Source = uri;
                }
            }
            else
            {
                MessageBox.Show("Por favor ingresa una dirección IP válida (ej: 192.168.1.150:5002)", "Dirección Inválida", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnChangeMode_Click(object sender, RoutedEventArgs e)
        {
            // Reset state
            StopLocalServer();
            SetAutoStart(false); // Quita auto-inicio al restablecer
            
            try
            {
                if (File.Exists(ConfigFileName))
                {
                    File.Delete(ConfigFileName);
                }
            }
            catch { }

            _mode = "";
            _serverUrl = "";

            WebView.Source = new Uri("about:blank");
            ModeBadge.Text = "Modo: Sin Seleccionar";
            StatusText.Text = "Configura la aplicación para iniciar...";
            
            // Show overlay resetting controls
            ClientSetupFields.Visibility = Visibility.Collapsed;
            ModeCardsContainer.Visibility = Visibility.Visible;
            ModeSelectionOverlay.Visibility = Visibility.Visible;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExplicitClose)
            {
                e.Cancel = true;
                this.WindowState = WindowState.Minimized;
                this.Hide(); // Ocultar en la bandeja del sistema
                _notifyIcon?.ShowBalloonTip(2000, "ChatAgenda", "La aplicación sigue ejecutándose en segundo plano.", System.Windows.Forms.ToolTipIcon.Info);
            }
            else
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
                StopLocalServer();
                base.OnClosing(e);
            }
        }
    }
}
