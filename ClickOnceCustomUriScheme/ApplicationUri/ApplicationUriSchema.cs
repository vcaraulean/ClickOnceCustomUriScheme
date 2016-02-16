using Microsoft.Win32;
using NLog;

namespace ClickOnceCustomUriScheme.ApplicationUri
{
    public class ApplicationUriSchema
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private const string SchemaDefinition = "theapp";
        private static readonly string UriRegistrationKey = $"Software\\Classes\\{SchemaDefinition}";

        public static void CheckRegistration()
        {
            Log.Debug("Starting Custom Uri check");

            var registration = Registry.CurrentUser.OpenSubKey(UriRegistrationKey);
            if (registration != null)
            {
                Log.Debug($"Registration key found ({UriRegistrationKey})");
                // TODO: check all properties are setup correctly
                return;
            }

            Log.Debug($"Registration key not found ({UriRegistrationKey})");
            // TODO create key with values

            //  http://stackoverflow.com/questions/3964152/how-do-i-create-a-custom-protocol-and-map-it-to-an-application/3964401#3964401
            //  http://stackoverflow.com/questions/24455311/uri-scheme-launching
            //  https://msdn.microsoft.com/en-us/library/aa767914(v=vs.85).aspx
        }
    }
}