// Views/TerminalWindow.xaml.cs
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace ToolBarApp.Views
{
    /// <summary>
    /// Interaction logic for TerminalWindow.xaml
    /// </summary>
    public partial class TerminalWindow : Window
    {
        public TerminalWindow()
        {
            InitializeComponent();
            InitializeAsync();
        }

        /// <summary>
        /// Initializes WebView2 and loads the terminal HTML.
        /// </summary>
        private async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);

            // Load the terminal HTML content
            string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "terminal.html");
            if (File.Exists(htmlPath))
            {
                webView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
            }
            else
            {
                // Handle missing terminal.html
                MessageBox.Show("terminal.html not found. Please ensure it exists in the wwwroot folder.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Appends a message to the terminal.
        /// </summary>
        /// <param name="message">The message to append.</param>
        /// <param name="level">The log level.</param>
        public async Task AppendMessageAsync(string message, LogLevel level)
        {
            // You can style messages based on log level if desired
            string script = $"appendMessage('{EscapeForJavaScript(message)}', '{level.ToString()}');";
            await webView.ExecuteScriptAsync(script);
        }

        /// <summary>
        /// Escapes special characters for safe JavaScript insertion.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>Escaped string.</returns>
        private string EscapeForJavaScript(string input)
        {
            return input.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "");
        }
    }
}
