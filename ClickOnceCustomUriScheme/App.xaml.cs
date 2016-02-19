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
                    Log.Error("When Application is deployed as ClickOnce the StartupArgs should not contain -clickonce parameter.");
                    Shutdown();
                    return;
                }

                Log.Debug("Application is launched with -clickonce key. Attempt to run as ClickOnce application");

                if (argList.Count != 2)
                {
                    Log.Error("StartupArgs count mismatch. " +
                              "Application running with -clickonce key expects 2 additional parameters: " +
                              "uri to clickonce deployment and the original URI that is executing. " +
                              "Application execution will continue as a normal application, non ClickOnce deployed");
                    Shutdown();
                    return;
                }

                var clickOnceDeploymentUri = argList[0];
                var schemeUri = new Uri(argList[1]);
                
                // We only support URI with a single hop: either host specified, or no host and a path
                // Valid examples: theapp://ui-module; theapp:ui-module
                // Invalid examples: theapp:/ui-module/sub, theapp:ui-module/sub

                // if '//' is specified, then host is not empty. without '//' host is null
                var applicationPath = $"{schemeUri.Host}/{schemeUri.AbsolutePath}".Trim('/');
                if (applicationPath.Contains("/"))
                {
                    Log.Error("The path in provided URI contains multiple segments. " +
                              "Only single-segment paths are allowed. Good examples: theapp://ui-module, theapp:ui-module. " +
                              $"Bad example: theapp://ui-module/segment. Provided uri value: {schemeUri}, extracted path: {applicationPath}");
                    Shutdown();
                    return;
                }

                var schemeUriQuery = schemeUri.Query.Trim('?', ';');
                if (!string.IsNullOrEmpty(schemeUriQuery))
                    schemeUriQuery = $"&{schemeUriQuery}";

                var clickOnceQuery = $"applicationPath={applicationPath}{schemeUriQuery}";
                var clickOnceLaunchCommand = $"{clickOnceDeploymentUri}?{clickOnceQuery}";
                Log.Debug("ClickOnce launch command: " + clickOnceLaunchCommand);
                var processInfo = new ProcessStartInfo
                {
                    FileName = "iexplore.exe",
                    Arguments = clickOnceLaunchCommand,
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
