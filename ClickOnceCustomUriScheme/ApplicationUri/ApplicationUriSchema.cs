using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using NLog;

namespace ClickOnceCustomUriScheme.ApplicationUri
{
    /// <summary>
    /// 
    /// Registering an Application to a URI Scheme: https://msdn.microsoft.com/en-us/library/aa767914(v=vs.85).aspx
    /// 
    /// Keys are registered in User's space (HKCU)
    /// </summary>
    public static class ApplicationUriSchema
    {
        private const string SchemaDefinition = "theapp";
        private const string CustomUriSwitch = "-clickonce";

        private static readonly string UriRegistrationKey = $"Software\\Classes\\{SchemaDefinition}";
        private static readonly string CommandRegistrationKey = $"{UriRegistrationKey}\\shell\\open\\command";

        private static string PathToCurrentExecutable => Assembly.GetExecutingAssembly().Location;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // Possible improvement: 
        // If there are other consumers of startup arguments, then take out consumed arguments and return what's left
        public static void Handle(List<string> startupArgs)
        {
            if (startupArgs.RemoveAll(x => x.ToLowerInvariant() == CustomUriSwitch) > 0)
                HandleStartupFromCustomUriScheme(startupArgs);

            CheckCustomUriRegistration();
        }

        private static void HandleStartupFromCustomUriScheme(IReadOnlyList<string> startupArgs)
        {
            if (ApplicationDeployment.IsNetworkDeployed)
            {
                Log.Error("When Application is deployed and run as ClickOnce the StartupArgs should not contain -clickonce parameter.");
                Application.Current.Shutdown();
                return;
            }

            Log.Debug("Application is launched with -clickonce key. Attempt to run as ClickOnce application");

            if (startupArgs.Count != 2)
            {
                Log.Error("StartupArgs count mismatch. " +
                          "Application running with -clickonce key expects 2 additional parameters: " +
                          "uri to clickonce deployment and the original URI that is executing. " +
                          "Application execution will continue as a normal application, non ClickOnce deployed");
                Application.Current.Shutdown();
                return;
            }

            var clickOnceDeploymentUri = startupArgs[0];
            var schemeUri = new Uri(startupArgs[1]);

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
                Application.Current.Shutdown();
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
            new Process { StartInfo = processInfo }.Start();
            Application.Current.Shutdown();
        }

        private static void CheckCustomUriRegistration()
        {
            if (!ApplicationDeployment.IsNetworkDeployed)
            {
                Log.Debug("Application is not deployed using ClickOnce. Uri Scheme will not be registered");
                return;
            }

            if (!ApplicationDeployment.CurrentDeployment.IsFirstRun)
            {
                Log.Debug("This is not first run of the application, skipping registration of custom URI schema");
                return;
            }

            WriteRequiredRegistryKeys();
            Log.Debug("Application's Custom Uri schema configuration completed");
        }

        private static void WriteRequiredRegistryKeys()
        {
            using (var uriKey = Registry.CurrentUser.CreateSubKey(UriRegistrationKey))
            {
                if (uriKey == null)
                {
                    Log.Debug($"Registration key cannot be created ({UriRegistrationKey})");
                    return;
                }

                uriKey.SetValue(null, "URL:Catalyst protocol", RegistryValueKind.String);
                uriKey.SetValue("URL Protocol", "", RegistryValueKind.String);
            }

            var defaultIconKey = Registry.CurrentUser.CreateSubKey("DefaultIcon");
            if (defaultIconKey == null)
            {
                Log.Error("default icon key is null");
                return;
            }
            defaultIconKey.SetValue(null, $"{PathToCurrentExecutable},1");

            using (var commandKey = Registry.CurrentUser.CreateSubKey(CommandRegistrationKey))
            {
                if (commandKey == null)
                {
                    Log.Error($"Key {CommandRegistrationKey} was not created");
                    return;
                }

                var launchCommand = GetApplicationLaunchCommand();
                Log.Debug($"Command to launch application: {launchCommand}");
                commandKey.SetValue(null, launchCommand, RegistryValueKind.String);
            }
        }

        private static string GetApplicationLaunchCommand()
        {
            var uri = ApplicationDeployment.CurrentDeployment.ActivationUri?.AbsoluteUri;
            if (uri == null)
                throw new InvalidOperationException("ApplicationDeployment.CurrentDeployment.ActivationUri is null");

            // In case if application was launched by URI schema handler and was also updated, then 
            // uri might contain query parameters. We want only the absolute uri.
            if (uri.Contains("?"))
                uri = uri.Substring(0, uri.IndexOf('?'));
            
            return $"\"{PathToCurrentExecutable}\" {CustomUriSwitch} \"{uri}\" \"%1\"";
        }
    }
}