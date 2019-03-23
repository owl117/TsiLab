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

        private readonly NoaaClient _noaaClient;
        private readonly TsiDataClient _tsiDataClient;
        private readonly string _tsiEnvironmentFqdn;
        private readonly AzureMapsClient _azureMapsClient;

        public StastionsProcessor(
            NoaaClient noaaClient,
            TsiDataClient tsiDataClient,
            string tsiEnvironmentFqdn,
            AzureMapsClient azureMapsClient)
        {
            _noaaClient = noaaClient;
            _tsiDataClient = tsiDataClient;
            _tsiEnvironmentFqdn = tsiEnvironmentFqdn;
            _azureMapsClient = azureMapsClient;
            Stations = new Station[0];
        }

        public Station[] Stations { get; private set; }

        public async Task ReloadStationsAsync()
        {
            Station[] stations = await Retry.RetryWebCallAsync(
                () => _noaaClient.GetStationsAsync(),
                "ReloadStations",
                // Keep retrying until we get the stations.
                numberOfAttempts: -1, waitMilliseconds: 5000, rethrowWebException: false);

            if (stations != null && stations.Length > 0)
            {
                Stations = stations;
            }
        }

        public async Task<bool> UpdateTsmAsync()
        {
            if (Stations.Length == 0)
            {
                Logger.TraceLine($"UpdateTsm({_tsiEnvironmentFqdn}): Stations not loaded, model update skipped during this pass");
                return false;
            }

            TsiDataClient.BatchResult[] putTypesResult = await Retry.RetryWebCallAsync(
                () => _tsiDataClient.PutTimeSeriesTypesAsync(_tsiEnvironmentFqdn, new [] { TsmMapping.StationObservationsType }),
                "UpdateTsm({_tsiEnvironmentFqdn})/Type",
                // If failed, skip it during this pass - it will be retried during the next pass.
                numberOfAttempts: 1, waitMilliseconds: 0, rethrowWebException: false);

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
                () => _tsiDataClient.PutTimeSeriesHierarchiesAsync(_tsiEnvironmentFqdn, new [] { TsmMapping.GeoLocationMetadata.GeoLocationsHierarchy }),
                "UpdateTsm({_tsiEnvironmentFqdn})/Hierarchy",
                // If failed, skip it during this pass - it will be retried during the next pass.
                numberOfAttempts: 1, waitMilliseconds: 0, rethrowWebException: false);

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
            for (int i = 0; i < Stations.Length / StationsUpdateBatchSize + 1; ++i)
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
                numberOfAttempts: 1, waitMilliseconds: 0, rethrowWebException: false);

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
                numberOfAttempts: 3, waitMilliseconds: 500, rethrowWebException: false) : null;

            if (address != null)
            {
                instanceFields.Add(TsmMapping.GeoLocationMetadata.InstanceFieldName_Country, address.Country);
                instanceFields.Add(TsmMapping.GeoLocationMetadata.InstanceFieldName_CountrySubdivisionName, address.CountrySubdivisionName);
                instanceFields.Add(TsmMapping.GeoLocationMetadata.InstanceFieldName_CountrySecondarySubdivision, address.CountrySecondarySubdivision);
                instanceFields.Add(TsmMapping.GeoLocationMetadata.InstanceFieldName_Municipality, address.Municipality);
                instanceFields.Add(TsmMapping.GeoLocationMetadata.InstanceFieldName_PostalCode, address.PostalCode);
            }

            return new TimeSeriesInstance(
                timeSeriesId: new object[] { station.Id },
                typeId: TsmMapping.StationObservationsType.Id,
                name: $"Observations {station.ShortId} ({station.Name})",
                description: "Weather observations for " + station.Name,
                instanceFields: instanceFields,
                hierarchyIds: new [] { TsmMapping.GeoLocationMetadata.GeoLocationsHierarchy.Id });
        }
    }
}