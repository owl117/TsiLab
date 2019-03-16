using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Engine
{
    public sealed class StastionsProcessor
    {
        private const int StationsUpdateBatchSize = 100;
        private const int StationsUpdateParalellism = 8;

        private readonly TsiDataClient _tsiDataClient;
        private readonly string _tsiEnvironmentFqdn;
        private readonly AzureMapsClient _azureMapsClient;

        public StastionsProcessor(TsiDataClient tsiDataClient, string tsiEnvironmentFqdn, AzureMapsClient azureMapsClient)
        {
            Stations = new List<Station>();
            _tsiDataClient = tsiDataClient;
            _tsiEnvironmentFqdn = tsiEnvironmentFqdn;
            _azureMapsClient = azureMapsClient;
        }

        public List<Station> Stations { get; private set; }

        public async Task ReloadStationsAsync()
        {
            bool detected403 = false;
            List<Station> stations = await Retry.RetryWebCallAsync(
                () => HttpUtils.MakeHttpCallAsync("https://api.weather.gov/stations", ParseStations, pretendBrowser: true),
                "ReloadStations",
                // Keep retrying until we get the stations.
                numberOfAttempts: -1, waitMilliseconds: 5000, rethrow: false,
                onWebException: (WebException e) => 
                {
                    HttpWebResponse response = (HttpWebResponse)e.Response;
                    if (response != null && response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        detected403 = true;
                    }

                    // Stop retrying if we get 403.
                    return !detected403;
                });
            
            if (detected403)
            {
                throw new Detected403Exception();
            }

            if (stations != null)
            {
                Stations = stations;
            }
        }

        private static List<Station> ParseStations(TextReader textReader)
        {
            JObject jObject = JsonUtils.ParseJson(textReader);

            JArray features = JsonUtils.GetPropertyValueOrNull<JArray>(jObject, "features");
            var stations = new List<Station>(features.Count);

            foreach (JObject feature in features)
            {
                JObject properties = JsonUtils.GetPropertyValueOrNull<JObject>(feature, "properties");
                Debug.Assert(properties != null && properties["@type"].ToString() == "wx:ObservationStation", 
                             "properties != null && properties[\"@type\"].ToString() == \"wx:ObservationStation\"");

                JObject geometry = JsonUtils.GetPropertyValueOrNull<JObject>(feature, "geometry");
                Debug.Assert(geometry != null && geometry["type"].ToString() == "Point", 
                             "geometry != null && geometry[\"type\"].ToString() == \"Point\"");
                
                JArray coordinates = JsonUtils.GetPropertyValueOrNull<JArray>(geometry, "coordinates");
                stations.Add(new Station(
                    id: properties["@id"].ToString(),
                    shortId: properties["stationIdentifier"].ToString(),
                    name: properties["name"].ToString(),
                    timeZone: properties["timeZone"].ToString(),
                    latitude: coordinates[1].Value<double>(),
                    longitude: coordinates[0].Value<double>()));
            }

            return stations;
        }

        public async Task<bool> UpdateTsmAsync()
        {
            if (Stations.Count == 0)
            {
                Logger.TraceLine($"UpdateTsm({_tsiEnvironmentFqdn}): Stations not loaded, model update skipped during this pass");
                return false;
            }

            TsiDataClient.BatchResult[] putTypesResult = await Retry.RetryWebCallAsync(
                () => _tsiDataClient.PutTimeSeriesTypesAsync(_tsiEnvironmentFqdn, new [] { StationObservation.TimeSeriesType }),
                "UpdateTsm({_tsiEnvironmentFqdn})/Type",
                // If failed, skip it during this pass - it will be retried during the next pass.
                numberOfAttempts: 1, waitMilliseconds: 0, rethrow: false);

            // If failed, skip it during this pass - it will be retried during the next pass.
            if (putTypesResult == null)
            {
                Logger.TraceLine($"UpdateTsm({_tsiEnvironmentFqdn})/Type: Update failed, model update skipped during this pass");
                return false;
            }
            else if (putTypesResult.Any(e => !String.IsNullOrWhiteSpace(e.Error)))
            {
                Logger.TraceLine($"UpdateTsm({_tsiEnvironmentFqdn})/Type: Update failed, model update skipped during this pass. Errors: " +
                                 String.Join(", ", putTypesResult.Where(e => !String.IsNullOrWhiteSpace(e.Error))
                                                                 .Select(e => $"typeId({e.ItemId}) -> {e.Error}")));
                return false;
            }

            TsiDataClient.BatchResult[] putHierarchiesResult = await Retry.RetryWebCallAsync(
                () => _tsiDataClient.PutTimeSeriesHierarchiesAsync(_tsiEnvironmentFqdn, new [] { Station.TimeSeriesHierarchy }),
                "UpdateTsm({_tsiEnvironmentFqdn})/Hierarchy",
                // If failed, skip it during this pass - it will be retried during the next pass.
                numberOfAttempts: 1, waitMilliseconds: 0, rethrow: false);

            // If failed, skip it during this pass - it will be retried during the next pass.
            if (putHierarchiesResult == null)
            {
                Logger.TraceLine($"UpdateTsm({_tsiEnvironmentFqdn})/Hierarchy: Update failed, model update skipped during this pass");
                return false;
            }
            else if (putHierarchiesResult.Any(e => !String.IsNullOrWhiteSpace(e.Error)))
            {
                Logger.TraceLine($"UpdateTsm({_tsiEnvironmentFqdn})/Hierarchy: Update failed, model update skipped during this pass. Errors: " +
                                 String.Join(", ", putHierarchiesResult.Where(e => !String.IsNullOrWhiteSpace(e.Error))
                                                                       .Select(e => $"hierarchyId({e.ItemId}) -> {e.Error}")));
                return false;
            }

            var stationBatches = new List<List<Station>>();
            for (int i = 0; i < Stations.Count / StationsUpdateBatchSize + 1; ++i)
            {
                stationBatches.Add(Stations.Skip(i * StationsUpdateBatchSize).Take(StationsUpdateBatchSize).ToList());
            }

            for (int i = 0; i < stationBatches.Count / StationsUpdateParalellism + 1; ++i)
            {
                await Task.WhenAll(
                    stationBatches
                    .Skip(i * StationsUpdateParalellism)
                    .Take(StationsUpdateParalellism)
                    .Select(batch => UpdateTsmInstancesAsync(batch)));
            }

            return true;
        }

        private async Task UpdateTsmInstancesAsync(List<Station> stations)
        {
            // Convert station to time series instances.
            // Some may be skipped due to errors, which is fine - they will be retried during the next pass.
            var instances = new List<TimeSeriesInstance>();
            foreach (Station station in stations)
            {
                TimeSeriesInstance instance = await ConvertToTimeSeriesInstanceAsync(station);
                if (instance != null)
                {
                    instances.Add(instance);
                }
            }

            TsiDataClient.BatchResult[] putTimeSeriesInstances =  await Retry.RetryWebCallAsync(
                () => _tsiDataClient.PutTimeSeriesInstancesAsync(_tsiEnvironmentFqdn, instances.ToArray()),
                $"UpdateTsm({_tsiEnvironmentFqdn})/Instances",
                // If failed, skip it during this pass - it will be retried during the next pass.
                numberOfAttempts: 1, waitMilliseconds: 0, rethrow: false);

            // If failed, skip it during this pass - it will be retried during the next pass.
            if (putTimeSeriesInstances == null)
            {
                Logger.TraceLine($"UpdateTsm({_tsiEnvironmentFqdn})/Instances: Update for the following stations failed and will be skipped during this pass: " +
                                 String.Join(", ", stations.Select(s => s.ShortId)));
            }
            else if (putTimeSeriesInstances.Any(e => !String.IsNullOrWhiteSpace(e.Error)))
            {
                Logger.TraceLine($"UpdateTsm({_tsiEnvironmentFqdn})/Instances: Update failed and will be skipped during this pass. Errors: " +
                                 String.Join(", ", putTimeSeriesInstances.Where(e => !String.IsNullOrWhiteSpace(e.Error))
                                                                         .Select(e => $"instanceId({e.ItemId}) -> {e.Error}")));
            }
            else
            {
                Logger.TraceLine($"UpdateTsm({_tsiEnvironmentFqdn})/Instances: Model for the following stations has been updated: " + 
                                 String.Join(", ", stations.Select(s => s.ShortId)));
            }
        }

        private async Task<TimeSeriesInstance> ConvertToTimeSeriesInstanceAsync(Station station)
        {
            var instanceFields = new Dictionary<string, string>()
            {
                { "FullName", station.Name },
                { "TimeZone", station.TimeZone }
            };

            AzureMapsClient.Address address = station.Latitude != null && station.Longitude != null ? await Retry.RetryWebCallAsync(
                () => _azureMapsClient.SearchAddressReverseAsync(station.Latitude.Value, station.Longitude.Value),
                $"UpdateTsm({_tsiEnvironmentFqdn})/ResolveStationAddress({station.ShortId})",
                // Try few times. If failed, skip it - this station will have no address.
                numberOfAttempts: 3, waitMilliseconds: 500, rethrow: false) : null;

            if (address != null)
            {
                instanceFields.Add(Station.InstanceFieldName_Country, address.Country);
                instanceFields.Add(Station.InstanceFieldName_CountrySubdivisionName, address.CountrySubdivisionName);
                instanceFields.Add(Station.InstanceFieldName_CountrySecondarySubdivision, address.CountrySecondarySubdivision);
                instanceFields.Add(Station.InstanceFieldName_Municipality, address.Municipality);
                instanceFields.Add(Station.InstanceFieldName_PostalCode, address.PostalCode);
            }

            return new TimeSeriesInstance(
                timeSeriesId: new object[] { station.Id },
                typeId: StationObservation.TimeSeriesType.Id,
                name: $"Observations {station.ShortId} ({station.Name})",
                description: "Weather observations for " + station.Name,
                instanceFields: instanceFields,
                hierarchyIds: new [] { Station.TimeSeriesHierarchy.Id });
        }
    }
}