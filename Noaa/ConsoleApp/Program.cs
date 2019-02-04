using System;
using Engine;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            new TimeSeriesType(
                id: "id1",
                name: "name1",
                description: "descr1",
                variables: new TimeSeriesVariable[]
                {
                    new TimeSeriesVariable(
                        kind: TimeSeriesVariable.VariableKind.Aggregate,
                        value: new TimeSeriesExpression("hello"),
                        filter: new TimeSeriesExpression("world"),
                        aggregation: new TimeSeriesExpression("!!!")
                    )
                }
            ).Write("TimeSeriesType.txt");
            var tsiDataClient = TsiDataClient.AadLoginAsApplicationAsync(Settings.Loaded.ApplicationClientInfo).Result;
            Console.WriteLine(tsiDataClient.GetEnvironmentsAsync().Result);

            var azureMapsClient = new AzureMapsClient(Settings.Loaded.AzureMapsSubscriptionKey);
            var x = azureMapsClient.SearchAddressReverseAsync(37.156, -94.5).Result;
            Console.WriteLine(x.FreeformAddress);
            return;
            Logger.InitializeTelemetryClient(Settings.Loaded.ApplicationInsightsInstrumentationKey);
            
            try
            {
                HttpUtils.DefaultConnectionLimit = 18;

                var mainProcessor = new MainProcessor(
                    Settings.Loaded.StorageAccountInfo,
                    Settings.Loaded.TsidCheckpointingTableName,
                    Settings.Loaded.StationObservationsCheckpointingPartitionKey,
                    Settings.Loaded.EventHubConnectionString);
                
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
