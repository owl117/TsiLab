using System;
using Engine;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var stationsProcessor = new StastionsProcessor(
                    TsiDataClient.AadLoginAsApplicationAsync(Settings.Loaded.ApplicationClientInfo).Result,
                    Settings.Loaded.TsiEnvironmentFqdn,
                    new AzureMapsClient(Settings.Loaded.AzureMapsSubscriptionKey));
                stationsProcessor.ReloadStationsAsync().Wait();
                stationsProcessor.UpdateTsmAsync().Wait();
            }
            catch (Exception e)
            {
                var we = Retry.UnwrapAggregateException<System.Net.WebException>(e);
                using (var sr = new System.IO.StreamReader(we.Response.GetResponseStream()))
                {
                    Console.WriteLine(sr.ReadToEnd());
                }
            }
            return;
            Logger.InitializeTelemetryClient(Settings.Loaded.ApplicationInsightsInstrumentationKey);
            
            try
            {
                HttpUtils.DefaultConnectionLimit = 18;

                var mainProcessor = new MainProcessor(
                    Settings.Loaded.ApplicationClientInfo,
                    Settings.Loaded.StorageAccountInfo,
                    Settings.Loaded.TsidCheckpointingTableName,
                    Settings.Loaded.StationObservationsCheckpointingPartitionKey,
                    Settings.Loaded.EventHubConnectionString,
                    Settings.Loaded.AzureMapsSubscriptionKey,
                    Settings.Loaded.TsiEnvironmentFqdn);
                
                mainProcessor.Run().Wait();
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
