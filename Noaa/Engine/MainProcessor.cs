using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;

namespace Engine
{
    public sealed class MainProcessor
    {
        private static TimeSpan NormalPassDelay = TimeSpan.FromMinutes(5);
        /// <summary>
        /// weather.gov limits to 10,000 calls 'per day per caller'.
        /// Throttling is indicated by HTTP 403.
        /// In reality throttling goes away sooner than a day, just needs a long wait. 
        /// </summary>
        private static TimeSpan DelayOn403 = TimeSpan.FromMinutes(30);
        private static TimeSpan UpdateStationsInterval = TimeSpan.FromDays(7);
        private readonly StastionsProcessor _stationsProcessor;
        private readonly TsidCheckpointing _stationObservationsCheckpointing;
        private Dictionary<string, StationObservationsProcessor> _stationObservationProcessors;
        private readonly EventHubClient _eventHubClient;

        public MainProcessor(
            AzureUtils.ApplicationClientInfo applicationClientInfo,
            AzureUtils.StorageAccountInfo storageAccountInfo,
            string tsidCheckpointingTableName,
            string stationObservationsCheckpointingPartitionKey,
            string eventHubConnectionString,
            string azureMapsSubscriptionKey,
            string azureMapsCacheTableName,
            string tsiEnvironmentFqdn)
        {
            _stationsProcessor = new StastionsProcessor(
                TsiDataClient.AadLoginAsApplicationAsync(applicationClientInfo).Result,
                tsiEnvironmentFqdn,
                new AzureMapsClient(
                    azureMapsSubscriptionKey,
                    AzureUtils.GetOrCreateTableAsync(storageAccountInfo, azureMapsCacheTableName).Result));

            _stationObservationsCheckpointing = new TsidCheckpointing(
                AzureUtils.GetOrCreateTableAsync(storageAccountInfo, tsidCheckpointingTableName).Result,
                stationObservationsCheckpointingPartitionKey);

            _stationObservationProcessors = new Dictionary<string, StationObservationsProcessor>();

            _eventHubClient = EventHubClient.CreateFromConnectionString(eventHubConnectionString);
        }

        public async Task Run()
        {
            DateTime? updateStationsTimestamp = null;

            var tasks = new List<Task>(HttpUtils.DefaultConnectionLimit);
            int passCount = 1;
            while (true)
            {
                // Make a pass thru all stations.
                Logger.TraceLine($"PASS {passCount} START");
                Stopwatch stopwatch = Stopwatch.StartNew();

                if (updateStationsTimestamp == null || DateTime.Now - updateStationsTimestamp > UpdateStationsInterval)
                {
                    await ReloadStationsAndUpdateTsmAsync();
                    updateStationsTimestamp = DateTime.Now;
                }

                List<StationObservationsProcessor> stationObservationsProcessors = _stationObservationProcessors.Values.ToList();
                bool detected403 = false;
                while (stationObservationsProcessors.Count > 0 || tasks.Count > 0)
                {
                    while (stationObservationsProcessors.Count > 0 && 
                           tasks.Count < System.Net.ServicePointManager.DefaultConnectionLimit)
                    {
                        StationObservationsProcessor stationObservationsProcessor = stationObservationsProcessors[0];
                        tasks.Add(stationObservationsProcessor.ProsessStationObservationsAsync());
                        stationObservationsProcessors.RemoveAt(0);
                    }

                    Task completedTask = await Task.WhenAny(tasks);
                    tasks.Remove(completedTask);
                    try
                    {
                        await completedTask;
                    }
                    catch (Exception e)
                    {
                        detected403 |= Retry.UnwrapAggregateException<Detected403Exception>(e) != null;
                        if (detected403)
                        {
                            // Skip remaining work on this pass.
                            stationObservationsProcessors.Clear();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                TimeSpan delay;
                if (detected403)
                {
                    Logger.TraceLine($"PASS {passCount} CANCELLED: Detected 403, waiting until {DateTime.Now + DelayOn403}");
                    delay = DelayOn403;
                }
                else
                {
                    Logger.TraceLine($"PASS {passCount} DONE: Elapsed {stopwatch.Elapsed}");
                    delay = (_stationObservationProcessors.Values.Min(sop => sop.GoodNextTimeToProcess) - DateTime.Now) + TimeSpan.FromMinutes(1);
                    delay = NormalPassDelay > delay ? NormalPassDelay : delay;
                }

                ++passCount;
                Logger.Flush();
                await Task.Delay(delay);
            }
        }

        public async Task ReloadStationsAndUpdateTsmAsync()
        {
            Logger.TraceLine("Loading stations.");
            while (true)
            {
                bool detected403 = false;
                try
                {
                    await _stationsProcessor.ReloadStationsAsync();
                }
                catch (Exception e)
                {
                    detected403 |= Retry.UnwrapAggregateException<Detected403Exception>(e) != null;
                    if (!detected403)
                    {
                        throw;
                    }
                }

                if (detected403)
                {
                    Logger.TraceLine($"Detected 403, waiting until {DateTime.Now + DelayOn403}");
                    await Task.Delay(DelayOn403);
                }
                else
                {
                    break;
                }
            }
            Logger.TraceLine($"Loaded {_stationsProcessor.Stations.Count} stations.");

            var stationObservationProcessors = new Dictionary<string, StationObservationsProcessor>(_stationsProcessor.Stations.Count);
            foreach (Station station in _stationsProcessor.Stations)
            {
                stationObservationProcessors.Add(
                    station.Id, 
                    _stationObservationProcessors.ContainsKey(station.Id) 
                        ? _stationObservationProcessors[station.Id] :
                        new StationObservationsProcessor(station.ShortId, _eventHubClient, _stationObservationsCheckpointing));
            }

            _stationObservationProcessors = stationObservationProcessors;

            Logger.TraceLine("Updating TSM.");
            await _stationsProcessor.UpdateTsmAsync();
            Logger.TraceLine($"Updated TSM for {_stationsProcessor.Stations.Count} stations.");
        }
    }
}