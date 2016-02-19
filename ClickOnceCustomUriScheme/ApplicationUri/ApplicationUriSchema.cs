using System;
using System.Deployment.Application;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;
using NLog;

namespace ClickOnceCustomUriScheme.ApplicationUri
{
    /// <summary>
    /// Documentation: https://msdn.microsoft.com/en-us/library/aa767914(v=vs.85).aspx
    /// 
    /// Keys are registered in User's space (HKCU)
    /// </summary>
    public static class ApplicationUriSchema
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private const string SchemaDefinition = "theapp";
        private static readonly string UriRegistrationKey = $"Software\\Classes\\{SchemaDefinition}";
        private static readonly string CommandRegistrationKey = $"{UriRegistrationKey}\\shell\\open\\command";

        public static string PathToCurrentExecutable => Assembly.GetExecutingAssembly().Location;

        public static void CheckRegistration()
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

            Log.Debug("Configuring Custom Uri schema");

            var stopwatch = Stopwatch.StartNew();
            Execute();
            Log.Debug($"Registry update completed in {stopwatch.ElapsedMilliseconds:N0} ms");
        }

        private static void Execute()
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
            
            return $"\"{PathToCurrentExecutable}\" -clickonce \"{uri}\" \"%1\"";
        }
    }
}