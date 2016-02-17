using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace ClickOnceCustomUriScheme
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainWindowViewModel();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var dir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName;
            var htmlFile = Path.Combine(dir, "index.html");

            Process.Start(htmlFile);
        }
    }
}
