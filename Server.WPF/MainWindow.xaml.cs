using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;
using WpfColor = System.Windows.Media.Color;

namespace ChatAgendaServer
{
    public partial class MainWindow : Window
    {
        // ─── Constantes ───────────────────────────────────────────────
        private const string ServiceName    = "ChatAgendaServer";
        private const string ServiceDisplay = "ChatAgenda Servidor";
        private const string ServiceDesc    = "Servidor web de mensajería y agenda ChatAgenda";
        private const int    ServerPort     = 5002;

        // ─── Estado interno ───────────────────────────────────────────
        private DispatcherTimer _statusTimer = new();
        private System.Windows.Forms.NotifyIcon? _trayIcon;
        private bool _isExplicitClose = false;
        private string _mainIP = "";

        // ─── Constructor ──────────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        // ─── Inicio ───────────────────────────────────────────────────
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            InitializeTrayIcon();
            DetectIPs();
            LoadAutoStartState();

            // Verificar que el ejecutable del servidor esté presente
            if (!FindServerExe(out string serverExe))
            {
                LogLine("⚠️  No se encontró chatAgenda.exe junto al programa.");
                LogLine($"   Buscado en: {AppDomain.CurrentDomain.BaseDirectory}");
                SetStatus(false, "Ejecutable del servidor no encontrado");
                UpdateButtons(running: false);
                MessageBox.Show(
                    "No se encontró el ejecutable del servidor (chatAgenda.exe).\n\n" +
                    "Asegúrese de que chatAgenda.exe esté en la misma carpeta que ChatAgendaServidor.exe.",
                    "Error de Configuración", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LogLine($"✅  Servidor encontrado: {serverExe}");

            // Registrar el servicio si no existe todavía
            if (!ServiceExists())
            {
                LogLine("⚙️  Registrando servicio de Windows...");
                bool ok = await System.Threading.Tasks.Task.Run(() => InstallWindowsService(serverExe));
                if (ok)
                    LogLine("✅  Servicio registrado correctamente.");
                else
                {
                    LogLine("⚠️  No se pudo registrar el servicio. Verifique permisos de administrador.");
                }
            }

            // Timer para actualizar estado cada 3 s
            _statusTimer.Interval = TimeSpan.FromSeconds(3);
            _statusTimer.Tick += (_, _) => RefreshServiceStatus();
            _statusTimer.Start();

            RefreshServiceStatus();

            // Auto-iniciar si el servicio no está corriendo
            if (IsServiceRunning())
            {
                LogLine("✅  El servicio ya estaba activo.");
            }
            else
            {
                LogLine("🔄  Iniciando servicio automáticamente...");
                StartService();
            }
        }

        // ─── Detección de IPs ─────────────────────────────────────────
        private void DetectIPs()
        {
            var ips = new List<string>();

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ip = addr.Address.ToString();
                        // Preferir redes LAN privadas 192.168.x.x / 10.x.x.x / 172.16-31.x.x
                        bool isPrivate = ip.StartsWith("192.168.") ||
                                         ip.StartsWith("10.")       ||
                                         (ip.StartsWith("172.") && int.TryParse(ip.Split('.')[1], out int oct) && oct >= 16 && oct <= 31);

                        string entry = $"http://{ip}:{ServerPort}";
                        if (isPrivate && string.IsNullOrEmpty(_mainIP))
                            _mainIP = entry;
                        else
                            ips.Add(entry);
                    }
                }
            }

            if (string.IsNullOrEmpty(_mainIP) && ips.Count > 0)
            {
                _mainIP = ips[0];
                ips.RemoveAt(0);
            }

            TxtMainIP.Text = string.IsNullOrEmpty(_mainIP)
                ? $"http://localhost:{ServerPort}"
                : _mainIP;

            AllIPsList.ItemsSource = ips;
            TxtFooterStatus.Text = $"IP principal: {_mainIP}   |   Puerto: {ServerPort}";
        }

        // ─── Servicio Windows ─────────────────────────────────────────
        private bool ServiceExists()
        {
            return ServiceController.GetServices()
                .Any(s => s.ServiceName.Equals(ServiceName, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsServiceRunning()
        {
            try
            {
                using var sc = new ServiceController(ServiceName);
                return sc.Status == ServiceControllerStatus.Running;
            }
            catch { return false; }
        }

        private bool InstallWindowsService(string exePath)
        {
            try
            {
                // Crear servicio con sc.exe
                var result = RunSc($"create \"{ServiceName}\" binPath= \"{exePath}\" start= auto DisplayName= \"{ServiceDisplay}\"");
                if (result != 0) return false;

                // Descripción
                RunSc($"description \"{ServiceName}\" \"{ServiceDesc}\"");

                // Recuperación automática en caso de fallo
                RunSc($"failure \"{ServiceName}\" reset= 60 actions= restart/5000/restart/10000/restart/30000");

                return true;
            }
            catch (Exception ex)
            {
                LogLine("❌  Error instalando servicio: " + ex.Message);
                return false;
            }
        }

        private int RunSc(string args)
        {
            var psi = new ProcessStartInfo("sc.exe", args)
            {
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                Verb                   = "runas"   // Elevar si es necesario
            };

            using var p = Process.Start(psi);
            p?.WaitForExit(10000);
            return p?.ExitCode ?? -1;
        }

        private bool UninstallWindowsService()
        {
            try
            {
                StopService(waitForStop: true);
                int result = RunSc($"delete \"{ServiceName}\"");
                return result == 0;
            }
            catch (Exception ex)
            {
                LogLine("❌  Error desinstalando servicio: " + ex.Message);
                return false;
            }
        }

        // ─── Control del servicio ─────────────────────────────────────
        private void StartService()
        {
            try
            {
                if (!ServiceExists())
                {
                    // Fallback: si no hay servicio, lanzar proceso directamente
                    if (FindServerExe(out string exe))
                    {
                        Process.Start(new ProcessStartInfo(exe)
                        {
                            CreateNoWindow  = true,
                            UseShellExecute = false
                        });
                        LogLine("▶  Servidor iniciado como proceso (sin servicio).");
                    }
                    return;
                }

                using var sc = new ServiceController(ServiceName);
                if (sc.Status != ServiceControllerStatus.Running &&
                    sc.Status != ServiceControllerStatus.StartPending)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
                    LogLine("▶  Servicio iniciado correctamente.");
                }
            }
            catch (Exception ex)
            {
                LogLine("❌  Error al iniciar servicio: " + ex.Message);
                // Intentar como proceso si el servicio falla
                TryStartAsProcess();
            }

            RefreshServiceStatus();
        }

        private void StopService(bool waitForStop = false)
        {
            try
            {
                if (!ServiceExists()) return;
                using var sc = new ServiceController(ServiceName);
                if (sc.Status == ServiceControllerStatus.Running ||
                    sc.Status == ServiceControllerStatus.PausePending)
                {
                    sc.Stop();
                    if (waitForStop)
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
                    LogLine("■  Servicio detenido.");
                }
            }
            catch (Exception ex)
            {
                LogLine("❌  Error al detener servicio: " + ex.Message);
            }

            RefreshServiceStatus();
        }

        private void TryStartAsProcess()
        {
            if (FindServerExe(out string exe))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(exe)
                    {
                        CreateNoWindow  = true,
                        UseShellExecute = false
                    });
                    LogLine("▶  Servidor iniciado como proceso (modo alternativo).");
                }
                catch (Exception ex2)
                {
                    LogLine("❌  No se pudo iniciar el servidor: " + ex2.Message);
                }
            }
        }

        // ─── Actualizar UI según estado ───────────────────────────────
        private void RefreshServiceStatus()
        {
            Dispatcher.Invoke(() =>
            {
                bool running = false;

                try
                {
                    if (ServiceExists())
                    {
                        using var sc = new ServiceController(ServiceName);
                        running = sc.Status == ServiceControllerStatus.Running;
                        string statusText = sc.Status switch
                        {
                            ServiceControllerStatus.Running       => "En ejecución",
                            ServiceControllerStatus.Stopped       => "Detenido",
                            ServiceControllerStatus.StartPending  => "Iniciando...",
                            ServiceControllerStatus.StopPending   => "Deteniéndose...",
                            _                                     => sc.Status.ToString()
                        };
                        ServiceStatusLabel.Text = statusText;
                    }
                    else
                    {
                        // Verificar si el puerto está abierto (proceso independiente)
                        running = IsPortOpen(ServerPort);
                        ServiceStatusLabel.Text = running ? "En ejecución (proceso)" : "Detenido";
                    }
                }
                catch
                {
                    running = IsPortOpen(ServerPort);
                    ServiceStatusLabel.Text = running ? "En ejecución" : "Detenido";
                }

                SetStatus(running, running ? $"Servidor activo en puerto {ServerPort}" : "Servidor detenido");
                UpdateButtons(running);
            });
        }

        private void SetStatus(bool running, string detail)
        {
            StatusDot.Fill = new SolidColorBrush(running
                ? WpfColor.FromRgb(0x22, 0xC5, 0x5E)
                : WpfColor.FromRgb(0xEF, 0x44, 0x44));

            ServiceStatusIcon.Text  = running ? "✅" : "⏸";
            ServiceStatusTitle.Text = running ? "Servicio Activo" : "Servicio Detenido";
            ServiceStatusDetail.Text = detail;

            if (_trayIcon != null)
                _trayIcon.Text = running ? "ChatAgenda ● Servidor activo" : "ChatAgenda ○ Servidor detenido";
        }

        private void UpdateButtons(bool running)
        {
            BtnStartService.IsEnabled   = !running;
            BtnStopService.IsEnabled    =  running;
            BtnRestartService.IsEnabled =  running;
        }

        // ─── Utilidades ───────────────────────────────────────────────
        private bool FindServerExe(out string exePath)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string candidate = Path.Combine(baseDir, "chatAgenda.exe");
            exePath = candidate;
            return File.Exists(candidate);
        }

        private bool IsPortOpen(int port)
        {
            try
            {
                using var client = new TcpClient();
                var result = client.BeginConnect("127.0.0.1", port, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(400));
                if (success) client.EndConnect(result);
                return success;
            }
            catch { return false; }
        }

        private void LogLine(string message)
        {
            Dispatcher.Invoke(() =>
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                TxtLog.Text += $"[{timestamp}] {message}\n";
                LogScrollViewer.ScrollToBottom();
            });
        }

        private void LoadAutoStartState()
        {
            try
            {
                using var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                ChkAutoStart.IsChecked = rk?.GetValue("ChatAgendaServidor") != null;
            }
            catch { }
        }

        private void SetAutoStartThisApp(bool enable)
        {
            try
            {
                using var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (rk == null) return;
                if (enable)
                {
                    string exe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    if (!string.IsNullOrEmpty(exe))
                        rk.SetValue("ChatAgendaServidor", $"\"{exe}\"");
                }
                else
                {
                    rk.DeleteValue("ChatAgendaServidor", false);
                }
            }
            catch (Exception ex)
            {
                LogLine("⚠️  Error en registro de auto-inicio: " + ex.Message);
            }
        }

        // ─── System Tray ──────────────────────────────────────────────
        private void InitializeTrayIcon()
        {
            _trayIcon = new System.Windows.Forms.NotifyIcon();

            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                _trayIcon.Icon = File.Exists(iconPath)
                    ? new System.Drawing.Icon(iconPath)
                    : System.Drawing.SystemIcons.Application;
            }
            catch { _trayIcon.Icon = System.Drawing.SystemIcons.Application; }

            _trayIcon.Text    = "ChatAgenda Servidor";
            _trayIcon.Visible = true;

            _trayIcon.DoubleClick += (_, _) => RestoreWindow();
            _trayIcon.Click       += (s, e) =>
            {
                if (e is System.Windows.Forms.MouseEventArgs me &&
                    me.Button == System.Windows.Forms.MouseButtons.Left)
                    RestoreWindow();
            };

            var menu = new System.Windows.Forms.ContextMenuStrip();
            menu.Items.Add("🖥️ Panel del Servidor",    null, (_, _) => RestoreWindow());
            menu.Items.Add("▶  Iniciar Servicio",      null, (_, _) => Dispatcher.Invoke(StartService));
            menu.Items.Add("■  Detener Servicio",       null, (_, _) => Dispatcher.Invoke(() => StopService()));
            menu.Items.Add("-");
            menu.Items.Add("❌ Salir",                  null, (_, _) => Dispatcher.Invoke(ExitApp));
            _trayIcon.ContextMenuStrip = menu;
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

        private void ExitApp()
        {
            _isExplicitClose = true;
            Close();
        }

        // ─── Event Handlers ───────────────────────────────────────────
        private void BtnStartService_Click(object sender, RoutedEventArgs e)
        {
            LogLine("▶  Solicitando inicio del servicio...");
            StartService();
        }

        private void BtnStopService_Click(object sender, RoutedEventArgs e)
        {
            var r = MessageBox.Show(
                "¿Detener el servidor?\n\nLos clientes conectados perderán la conexión.",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (r == MessageBoxResult.Yes)
            {
                LogLine("■  Solicitando detención del servicio...");
                StopService();
            }
        }

        private async void BtnRestartService_Click(object sender, RoutedEventArgs e)
        {
            LogLine("↺  Reiniciando servicio...");
            BtnRestartService.IsEnabled = false;
            await System.Threading.Tasks.Task.Run(() =>
            {
                StopService(waitForStop: true);
                System.Threading.Thread.Sleep(1500);
                StartService();
            });
        }

        private void BtnCopyIP_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Clipboard.SetText(TxtMainIP.Text);
                var orig = ((System.Windows.Controls.Button)sender).Content;
                ((System.Windows.Controls.Button)sender).Content = "✅ Copiado";
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                timer.Tick += (_, _) =>
                {
                    ((System.Windows.Controls.Button)sender).Content = orig;
                    timer.Stop();
                };
                timer.Start();
            }
            catch { }
        }

        private void BtnOpenBrowser_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo($"http://localhost:{ServerPort}")
            { UseShellExecute = true });
        }

        private void BtnOpenWebView_Click(object sender, RoutedEventArgs e)
        {
            var viewer = new WebViewWindow($"http://localhost:{ServerPort}");
            viewer.Owner = this;
            viewer.Show();
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e) =>
            TxtLog.Text = "";

        private void ChkAutoStart_Checked(object sender, RoutedEventArgs e) =>
            SetAutoStartThisApp(true);

        private void ChkAutoStart_Unchecked(object sender, RoutedEventArgs e) =>
            SetAutoStartThisApp(false);

        private void BtnUninstallService_Click(object sender, RoutedEventArgs e)
        {
            var r = MessageBox.Show(
                "¿Desinstalar el servicio de Windows?\n\nEl servidor no arrancará automáticamente con la PC.",
                "Confirmar desinstalación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (r == MessageBoxResult.Yes)
            {
                bool ok = UninstallWindowsService();
                LogLine(ok ? "🗑️  Servicio desinstalado." : "❌  Error al desinstalar el servicio.");
                RefreshServiceStatus();
            }
        }

        // ─── Cierre ───────────────────────────────────────────────────
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExplicitClose)
            {
                e.Cancel = true;
                WindowState = WindowState.Minimized;
                Hide();
                _trayIcon?.ShowBalloonTip(2000, "ChatAgenda",
                    "El panel sigue en la bandeja. El servicio continúa activo.", System.Windows.Forms.ToolTipIcon.Info);
            }
            else
            {
                _statusTimer.Stop();
                _trayIcon?.Dispose();
                base.OnClosing(e);
            }
        }
    }
}
