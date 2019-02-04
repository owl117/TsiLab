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
        public string AzureMapsSubscriptionKey { get; set;}

        public static Settings Loaded
        {
            get
            {
                if (LoadedCache == null)
                {
                    using (StreamReader streamReader = File.OpenText("settings.json"))
                    {
                        LoadedCache = JsonUtils.ParseJson<Settings>(streamReader);
                    }
                }

                return LoadedCache;
            }
        }
    }
}