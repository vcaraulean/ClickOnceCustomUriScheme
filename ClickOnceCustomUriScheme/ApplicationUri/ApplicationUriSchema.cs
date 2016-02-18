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
    public class ApplicationUriSchema
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private const string SchemaDefinition = "theapp";
        private static readonly string UriRegistrationKey = $"Software\\Classes\\{SchemaDefinition}";
        private static readonly string CommandRegistrationKey = $"{UriRegistrationKey}\\shell\\open\\command";

        public static string PathToCurrentExecutable => Assembly.GetExecutingAssembly().Location;

        public static void CheckRegistration()
        {
            Log.Debug("Configuring Custom Uri schema");
            Log.Debug($"Path to current executable: {PathToCurrentExecutable}");

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

                string launchCommand = GetApplicationLaunchCommand();
                Log.Debug($"Command to launch application: {launchCommand}");
                commandKey.SetValue(null, launchCommand, RegistryValueKind.String);
            }
        }

        private static string GetApplicationLaunchCommand()
        {
            if (!ApplicationDeployment.IsNetworkDeployed)
                return $"\"{PathToCurrentExecutable}\" \"%1\"";

            var uri = ApplicationDeployment.CurrentDeployment.UpdateLocation;

            return $"\"{PathToCurrentExecutable}\" -clickonce \"{uri}\" \"%1\"";
        }
    }
}