using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using NLog;

namespace ClickOnceCustomUriScheme.ApplicationUri
{
    public class ApplicationUriSchema
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private const string SchemaDefinition = "theapp";
        private static readonly string UriRegistrationKey = $"Software\\Classes\\{SchemaDefinition}";
        private static readonly string CommandRegistrationKey = $"{UriRegistrationKey}\\shell\\open\\command";

        private static string PathToCurrentExecutable => Path.Combine(
            Assembly.GetExecutingAssembly().Location
            //, Process.GetCurrentProcess().ProcessName + ".exe"
            );

        public static void CheckRegistration()
        {
            Log.Debug("Starting Custom Uri check");
            Log.Debug($"Path to current executable: {PathToCurrentExecutable}");

            Log.Debug($"Registry key dump before: {Registry.CurrentUser.OpenSubKey(UriRegistrationKey)}");
            Execute();
            Log.Debug($"Registry key dump after: {Registry.CurrentUser.OpenSubKey(UriRegistrationKey)}");
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
                var defaultIconKey = uriKey.CreateSubKey("DefaultIcon");
                if (defaultIconKey == null)
                {
                    Log.Error("default icon key is null");
                    return;
                }
                defaultIconKey.SetValue(null, $"{PathToCurrentExecutable},1");
            }

            using (var commandKey = Registry.CurrentUser.CreateSubKey(CommandRegistrationKey))
            {
                if (commandKey == null)
                {
                    Log.Error($"Key {CommandRegistrationKey} was not created");
                    return;
                }
                commandKey.SetValue(null, $"\"{PathToCurrentExecutable}\" \"%1\"", RegistryValueKind.String);
            }
        }
    }
}