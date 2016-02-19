using System;
using System.Deployment.Application;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using NLog;

namespace ClickOnceCustomUriScheme
{
    public class MainWindowViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
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

        public string IsNetworkDeployed => ApplicationDeployment.IsNetworkDeployed ? "true" : "false";
        public string IsFirstRun => ApplicationDeployment.CurrentDeployment?.IsFirstRun == true ? "true" : "false";

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
                if (ApplicationDeployment.CurrentDeployment.ActivationUri == null)
                    return "ActivationUri is null";

                var collection = ApplicationDeployment.CurrentDeployment.ActivationUri?.ParseQueryString();
                if (collection == null)
                    return "No query paramters";

                //
                //foreach (var VARIABLE in collection.)
                //{

                //}

                //foreach (var item in collection)
                //{
                //    item.
                //}

                
                Log.Debug($"Query string Collection has {collection.AllKeys.Length} keys");
                var dict = collection
                    .AllKeys
                    .ToDictionary(k => k, k => collection[k]);

                var sb = new StringBuilder();
                foreach (var x in dict)
                {
                    Log.Debug($"Query string Collection {x.Key} : {x.Value}");
                    sb.AppendLine($"{x.Key}: {x.Value}");
                }
                return sb.ToString();
                //return collection
                //    .AllKeys
                //    .ToDictionary(k => k, k => collection[k])
                //    .Aggregate("", (acc, kvp1) => $"{kvp1.Key}: {kvp1.Value}{Environment.NewLine}");
            }
        }
    }
}