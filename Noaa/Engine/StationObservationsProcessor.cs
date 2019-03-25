using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Newtonsoft.Json.Linq;

namespace Engine
{
    public sealed class StationObservationsProcessor
    {
        private static TimeSpan DefaultSleepInterval = TimeSpan.FromMinutes(20);
        private static Random RetryDelayRandom = new Random();
        private readonly NoaaClient _noaaClient;
        private readonly EventHubClient _eventHubClient;
        private readonly TsidCheckpointing _checkpointing;
        private DateTimeOffset? _lastCommittedTimestamp;
        private DateTime _lastSuccessfulPullTime;

        public StationObservationsProcessor(
            string stationShortId,
            NoaaClient noaaClient,
            EventHubClient eventHubClient,
            TsidCheckpointing checkpointing)
        {
            StationShortId = stationShortId;
            _noaaClient = noaaClient;
            _eventHubClient = eventHubClient;
            _checkpointing = checkpointing;
            _lastCommittedTimestamp = null;
            _lastSuccessfulPullTime = DateTime.MinValue;
            GoodNextTimeToProcess = DateTime.MinValue;
        }

        public string StationShortId { get; private set; }

        public DateTimeOffset GoodNextTimeToProcess { get; private set; }

        public async Task ProsessStationObservationsAsync()
        {
            if (DateTime.Now < GoodNextTimeToProcess)
            {
                Logger.TraceLine($"ProcessObservations({StationShortId}): {(GoodNextTimeToProcess - DateTime.Now).TotalMinutes:N} minutes remain till the next pull");
                return;
            }

            DateTimeOffset pullFromTimestamp =
                _lastCommittedTimestamp 
                ?? await _checkpointing.TryGetLastCommittedTimestampAsync(StationShortId) 
                // For brand-new stations start with one day back. 
                ?? DateTimeOffset.UtcNow - TimeSpan.FromDays(1);

            StationObservation[] observations = await Retry.RetryWebCallAsync(
                () => _noaaClient.GetStationObservationsAsync(StationShortId, pullFromTimestamp),
                $"ProcessObservations({StationShortId})",
                // Sometimes it fails with timeout, try it second time with some random delay, otherwise let it fail and skip during this pass.
                numberOfAttempts: 2, waitMilliseconds: RetryDelayRandom.Next(500, 1500), rethrowWebException: false);

            if (observations == null)
            {
                // If failed for any reason, skip it during this pass - it will be retried during the next pass.
                Logger.TraceLine($"ProcessObservations({StationShortId}): Pull failed and skipped during this pass");
                return;
            }

            observations = observations.Where(o => o.Timestamp > pullFromTimestamp).OrderBy(o => o.Timestamp).ToArray();

            if (observations.Length > 0)
            {
                TimeSpan sleepInterval = (DateTime.Now - _lastSuccessfulPullTime) / 2;
                sleepInterval = DefaultSleepInterval < sleepInterval ? DefaultSleepInterval : sleepInterval;
                GoodNextTimeToProcess = DateTime.Now + sleepInterval;
                _lastSuccessfulPullTime = DateTime.Now;

                await SendDataToEventHub(observations);

                _lastCommittedTimestamp = observations.Max(o => o.Timestamp);
                await _checkpointing.SetLastCommittedTimestampAsync(StationShortId, _lastCommittedTimestamp.Value);

                Logger.TraceLine(
                    $"ProcessObservations({StationShortId}): {observations.Length} observations during the last " +
                    $"{(DateTime.Now - pullFromTimestamp).TotalMinutes:N} minutes, " + 
                    $"available for the next pull in {(GoodNextTimeToProcess - DateTime.Now).TotalMinutes:N} minutes");
            }
            else
            {
                // We've go here because there is no new data.
                _lastCommittedTimestamp = pullFromTimestamp;
                await _checkpointing.SetLastCommittedTimestampAsync(StationShortId, _lastCommittedTimestamp.Value);
                GoodNextTimeToProcess = DateTimeOffset.Now + DefaultSleepInterval / 2;

                Logger.TraceLine($"ProcessObservations({StationShortId}): No new data, available for the next pull in {(GoodNextTimeToProcess - DateTime.Now).TotalMinutes:N} minutes");
            }
        }

        private async Task SendDataToEventHub(StationObservation[] observations)
        {
            EventData eventData;
            using (var memoryStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memoryStream))
            {
                JsonUtils.WriteJson(streamWriter, observations);
                await streamWriter.FlushAsync();
                await memoryStream.FlushAsync();
                eventData = new EventData(memoryStream.ToArray());
            }

            await _eventHubClient.SendAsync(eventData);
        }
    }
}