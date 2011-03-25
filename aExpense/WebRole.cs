using System.Collections.Specialized;
using System.Linq;
using System.Web.Profile;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace AExpense
{
    using AExpense.Data;

    public class WebRole : RoleEntryPoint
    {
        public override bool OnStart()
        {
            DiagnosticMonitor.Start(AzureConnectionStrings.Diagnostics);

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            RoleEnvironment.Changing += (sender, e) =>
                                            {
                                                if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
                                                {
                                                    // Set e.Cancel to true to restart this role instance
                                                    e.Cancel = true;
                                                }
                                            };

            CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) =>
                                                                 configSetter(RoleEnvironment.GetConfigurationSettingValue(configName)));

            ApplicationStorageInitializer.Initialize();

            return base.OnStart();
        }
    }
}