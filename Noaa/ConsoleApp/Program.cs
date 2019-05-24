using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                // mainProcessor.LoadStationsAndGenerateReferenceDataJsonAsync("referenceData.json").Wait(); // generate ref data without pulling data
                // TestSerializeToJsonForEventHub();
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

        private static void TestSerializeToJsonForEventHub()
        {
            var input = new List<string>();
            for (int i = 0; i < 17; ++i)
            {
                input.Add($"aaaaaa {i}");
            }
            
            for (int maxSizeInBytes = 20; maxSizeInBytes < 1000; ++maxSizeInBytes)
            {
                Console.WriteLine($"test maxSizeInBytes = {maxSizeInBytes}");
                List<byte[]> partitions = AzureUtils.SerializeToJsonForEventHub(input.Select(s => (object)s).ToArray(), maxSizeInBytes);
                var output = new List<string>();
                foreach (byte[] partition in partitions)
                {
                    using (var ms = new MemoryStream())
                    {
                        ms.Write(partition);
                        ms.Flush();
                        ms.Position = 0;
                        string[] data = JsonUtils.ParseJson<string[]>(new StreamReader(ms));
                        output.AddRange(data);
                    }
                }

                Debug.Assert(input.Count == output.Count, "input.Count == output.Count");
                for (int i = 0; i < input.Count; ++i)
                {
                    Debug.Assert(input[i] == output[i], $"input[{i}] == output[{i}]");
                }
            }
        }
    }
}
