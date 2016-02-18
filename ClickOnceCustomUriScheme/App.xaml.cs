using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Diagnostics;
using System.Linq;
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

            if (e.Args.Any())
                HandleStartupArgs(e.Args);

            ApplicationUriSchema.CheckRegistration();
        }

        private void HandleStartupArgs(string[] args)
        {
            Log.Debug($"Application's startup arguments: {args.Aggregate("", (s, s1) => $"{s}, {s1}")}");

            var argList = args.ToList();
            if (argList.RemoveAll(MatchMaker("-clickonce")) > 0)
            {
                if (ApplicationDeployment.IsNetworkDeployed)
                {
                    Log.Warn("When Application is deployed as ClickOnce the StartupArgs should not contain -clickonce parameter.");
                    return;
                }

                Log.Debug("Application is launched with -clickonce key. Attempt to run as ClickOnce application");

                if (argList.Count != 2)
                {
                    Log.Error("StartupArgs count mismatch. " +
                              "Application running with -clickonce key expects 2 additional parameters: " +
                              "uri to clickonce deployment and the original URI that is executing. " +
                              "Application execution will continue as a normal application, non ClickOnce deployed");
                    return;
                }

                var clickOnceDeploymentUri = argList[0];
                var customUri = new Uri(argList[1]);
                var applicationPath = customUri.AbsolutePath.Trim('/');
                if (applicationPath.Contains("/"))
                {
                    Log.Error("AbsolutePath for provided URI contains multiple path segments. " +
                              "Only single-segment paths are allowed. Good example: theapp://ui/module. " +
                              $"Bad example: theapp://ui/module/segment. Provided uri value: {customUri}, extracted path: {applicationPath}");
                    return;
                }

                var originalQuery = customUri.Query.Trim('?', ';');

                var clickOnceQuery = $"applicationPath={applicationPath};{originalQuery}";
                var clickOnceLaunchCommand = $"{clickOnceDeploymentUri}?{clickOnceQuery}";
                Log.Debug("ClickOnce launch command: " + clickOnceLaunchCommand);
                var processInfo = new ProcessStartInfo
                {
                    // The supplied URL
                    FileName = clickOnceLaunchCommand,
                    UseShellExecute = true
                };

                Log.Debug("Launching application in ClickOnce context, then shutting down");
                new Process {StartInfo = processInfo}.Start();
                Shutdown();
            }
        }

        static Predicate<T> MatchMaker<T>(params T[] searches)
        {
            return input => searches.Any(s => EqualityComparer<T>.Default.Equals(s, input));
        }
    }
}
