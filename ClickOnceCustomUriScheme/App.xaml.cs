using System;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;
using ClickOnceCustomUriScheme.ApplicationUri;
using NLog;

namespace ClickOnceCustomUriScheme
{
    public partial class App : Application
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            var exception = args.Exception;

            Log.Error(exception, $"Application.DispatcherUnhandledException: {exception?.Message}");
            LogManager.Flush();

            Environment.Exit(-1);
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var exception = args.ExceptionObject as Exception;
            Log.Error(exception, $"AppDomain.CurrentDomain.UnhandledException: {exception?.Message}");
            LogManager.Flush();

            Environment.Exit(-1);
        }

        protected override void OnLoadCompleted(NavigationEventArgs e)
        {
            base.OnLoadCompleted(e);

            Log.Info("Application load completed");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Log.Info("Application is starting up");

            ApplicationUriSchema.CheckRegistration();
        }
    }
}
