using System;
using System.Deployment.Application;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
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
                sb.AppendLine($"ActivationUri: {ApplicationDeployment.CurrentDeployment.ActivationUri?.AbsoluteUri}");
                sb.AppendLine($"ActivationUri.Query: {ApplicationDeployment.CurrentDeployment.ActivationUri?.PathAndQuery}");
                sb.AppendLine($"UpdateLocation: {ApplicationDeployment.CurrentDeployment.UpdateLocation}");
                sb.AppendLine($"UpdateLocation.Query: {ApplicationDeployment.CurrentDeployment.UpdateLocation?.Query}");
                sb.AppendLine($"UpdateLocation.PathAndQuery: {ApplicationDeployment.CurrentDeployment.UpdateLocation?.PathAndQuery}");
                sb.AppendLine($"DataDirectory: {ApplicationDeployment.CurrentDeployment.DataDirectory}");
                return sb.ToString();
            }
        }

        public string QueryStringParameters
        {
            get
            {
                if (!ApplicationDeployment.IsNetworkDeployed)
                    return "<none>";

                //var query = ApplicationDeployment.CurrentDeployment.ActivationUri?.Query;
                if (ApplicationDeployment.CurrentDeployment.UpdateLocation == null)
                    return "UpdateLocation is null";

                var collection = ApplicationDeployment.CurrentDeployment.UpdateLocation?.ParseQueryString();
                if (collection == null)
                    return "No query paramters";

                return collection
                    .AllKeys
                    .ToDictionary(k => k, k => collection[k])
                    .Aggregate("", (acc, kvp1) => $"{kvp1.Key}: {kvp1.Value}{Environment.NewLine}");
            }
        }
    }
}