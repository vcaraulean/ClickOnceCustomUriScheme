using System;
using System.Deployment.Application;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Web;

namespace ClickOnceCustomUriScheme
{
    public class MainWindowViewModel
    {
        public string ApplicationVersion
        {
            get
            {
                if (ApplicationDeployment.IsNetworkDeployed)
                    return $"(clickonce) {ApplicationDeployment.CurrentDeployment.CurrentVersion}";

                return "(standalone) " + Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        public string IsNetworkDeployed => ApplicationDeployment.IsNetworkDeployed ? "true" : "false";
        public string IsFirstRun => ApplicationDeployment.IsNetworkDeployed ? (ApplicationDeployment.CurrentDeployment?.IsFirstRun == true ? "true" : "false") : "";

        public string AdditionalInfo
        {
            get
            {
                if (!ApplicationDeployment.IsNetworkDeployed)
                    return "<none>";

                var sb = new StringBuilder();
                sb.AppendLine($"ActivationUri: {ApplicationDeployment.CurrentDeployment.ActivationUri?.AbsoluteUri}");
                sb.AppendLine($"ActivationUri.Query: {ApplicationDeployment.CurrentDeployment.ActivationUri?.PathAndQuery}");
                sb.AppendLine($"UpdateLocation: {ApplicationDeployment.CurrentDeployment.UpdateLocation?.AbsoluteUri}");
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
                    return "<not network deployed>";

                var launchUri = ApplicationDeployment.CurrentDeployment.ActivationUri;

                if (launchUri == null)
                    return "<ActivationUri is null>";

                var collection = HttpUtility.ParseQueryString(launchUri.ToString());
                if (collection.Count == 0)
                    return "<no query paramters>";

                return collection
                    .AllKeys
                    .ToDictionary(k => k, k => collection[k])
                    .Aggregate("", (acc, kvp1) => acc + $"{kvp1.Key}: {kvp1.Value}{Environment.NewLine}");
            }
        }
    }
}