using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using NLog;

namespace ClickOnceCustomUriScheme
{
    public partial class MainWindow : Window
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainWindowViewModel();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var dir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName;
            var htmlFile = Path.Combine(dir, "index.html");
            Log.Debug($"Trying to launch file {htmlFile}");
            Process.Start(htmlFile);
        }
    }
}
