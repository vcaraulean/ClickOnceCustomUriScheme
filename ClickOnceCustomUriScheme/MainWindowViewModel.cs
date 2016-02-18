using System;
using System.Deployment.Application;
using System.Reflection;
using System.Text;

namespace ClickOnceCustomUriScheme
{
    public class MainWindowViewModel
    {
        public string ApplicationVersion
        {
            get
            {
                if (ApplicationDeployment.IsNetworkDeployed)
                {
                    return $"(clickonce) {ApplicationDeployment.CurrentDeployment.CurrentVersion}";
                }
                
                return "(standalone) " + Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        public string AdditionalInfo
        {
            get
            {
                if (!ApplicationDeployment.IsNetworkDeployed)
                    return "<none>";

                var sb = new StringBuilder();
                sb.AppendLine($"ActivationUri: {ApplicationDeployment.CurrentDeployment.ActivationUri}");
                sb.AppendLine($"UpdateLocation: {ApplicationDeployment.CurrentDeployment.UpdateLocation}");
                sb.AppendLine($"DataDirectory: {ApplicationDeployment.CurrentDeployment.DataDirectory}");
                return sb.ToString();
            }
        }
        public string Arguments { get; set; }
    }
}