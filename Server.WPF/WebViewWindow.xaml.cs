using System;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace ChatAgendaServer
{
    public partial class WebViewWindow : Window
    {
        public WebViewWindow(string url)
        {
            InitializeComponent();
            Loaded += async (_, _) =>
            {
                try
                {
                    await WebView.EnsureCoreWebView2Async(null);
                    WebView.CoreWebView2.Navigate(url);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar WebView2:\n{ex.Message}\n\nAsegúrate de tener WebView2 Runtime instalado.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };
        }
    }
}
