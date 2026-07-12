using System.Windows;

namespace EmpresaChat
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            var window = new MainWindow();
            MainWindow = window;
            window.Show();

            // Manejador para cuando se hace clic en notificaciones
            // Si la app fue activada desde una notificación, restablecer ventana
            if (MainWindow != null && MainWindow.WindowState == WindowState.Minimized)
            {
                MainWindow.Show();
                MainWindow.WindowState = WindowState.Normal;
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // Cuando la app se activa (incluyendo desde notificaciones)
            if (MainWindow != null)
            {
                MainWindow.Show();
                MainWindow.WindowState = WindowState.Normal;
                MainWindow.Activate();
            }
        }
    }
}
