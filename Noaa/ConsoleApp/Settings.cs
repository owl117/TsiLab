using System.IO;
using Engine;

namespace ConsoleApp
{
    public sealed class Settings
    {
        private static Settings LoadedCache = null;

        public AzureUtils.ApplicationClientInfo ApplicationClientInfo { get; set; }
        public string ApplicationInsightsInstrumentationKey { get; set; }
        public AzureUtils.StorageAccountInfo StorageAccountInfo { get; set; }
        public string TsidCheckpointingTableName { get; set; }
        public string StationObservationsCheckpointingPartitionKey { get; set; }
        public string EventHubConnectionString { get; set; }
        public string AzureMapsSubscriptionKey { get; set; }
        public string AzureMapsCacheTableName { get; set; }
        public string TsiEnvironmentFqdn { get; set; }

        public static Settings Loaded
        {
            get
            {
                if (LoadedCache == null)
                {
                    string fileName = File.Exists("settings-test.json") ? "settings-test.json" : "settings.json";
                    using (StreamReader streamReader = File.OpenText(fileName))
                    {
                        LoadedCache = JsonUtils.ParseJson<Settings>(streamReader);
                    }
                }

                return LoadedCache;
            }
        }
    }
}