using System;
using System.Reflection;

namespace ClickOnceCustomUriScheme
{
    public class MainWindowViewModel
    {
        public static string ApplicationVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public string Arguments { get; set; }
    }
}