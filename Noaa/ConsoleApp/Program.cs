using System;
using Engine;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.InitializeTelemetryClient(Settings.Loaded.ApplicationInsightsInstrumentationKey);
            
            try
            {
                HttpUtils.DefaultConnectionLimit = 8;

                var mainProcessor = new MainProcessor(
                    Settings.Loaded.ApplicationClientInfo,
                    Settings.Loaded.StorageAccountInfo,
                    Settings.Loaded.TsidCheckpointingTableName,
                    Settings.Loaded.StationObservationsCheckpointingPartitionKey,
                    Settings.Loaded.EventHubConnectionString,
                    Settings.Loaded.AzureMapsSubscriptionKey,
                    Settings.Loaded.AzureMapsCacheTableName,
                    Settings.Loaded.TsiEnvironmentFqdn);
                
                mainProcessor.Run().Wait();
                // mainProcessor.ReloadStationsAndUpdateTsmAsync().Wait(); // test TSM generation without pulling data
            }
            catch (Exception e)
            {
                Logger.TraceException("Main", e);
                throw;
            }
            finally
            {
                Logger.Flush();
            }
        }
    }
}
