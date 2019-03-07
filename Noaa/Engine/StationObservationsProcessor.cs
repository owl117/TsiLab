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
        private readonly EventHubClient _eventHubClient;
        private readonly TsidCheckpointing _checkpointing;
        private DateTimeOffset? _lastCommittedTimestamp;
        private DateTime _lastSuccessfulPullTime;

        public StationObservationsProcessor(string stationShortId, EventHubClient eventHubClient, TsidCheckpointing checkpointing)
        {
            StationShortId = stationShortId;
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
                ?? DateTimeOffset.UtcNow;

            string url = 
                $"https://api.weather.gov/stations/{StationShortId}/observations?start=" 
                + Uri.EscapeDataString(pullFromTimestamp.ToString("yyyy-MM-ddTHH:mm:sszzz"));

            bool detected403 = false;
            List<StationObservation> observations = await Retry.RetryWebCallAsync(
                () => HttpUtils.MakeHttpCallAsync(url, ParseObservartions, pretendBrowser: true),
                $"ProcessObservations({StationShortId})",
                // If failed, skip it during this pass - it will be retried during the next pass.
                numberOfAttempts: 1, waitMilliseconds: 0, rethrow: false,
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
                // Propagate 403 up.
                throw new Detected403Exception();
            }
            else if (observations == null)
            {
                // If failed for any other reason, skip it during this pass - it will be retried during the next pass.
                Logger.TraceLine($"ProcessObservations({StationShortId}): Pull failed and skipped during this pass");
                return;
            }

            observations = observations.Where(o => o.Timestamp > pullFromTimestamp).OrderBy(o => o.Timestamp).ToList();

            if (observations.Count > 0)
            {
                TimeSpan sleepInterval = (DateTime.Now - _lastSuccessfulPullTime) / 2;
                sleepInterval = DefaultSleepInterval < sleepInterval ? DefaultSleepInterval : sleepInterval;
                GoodNextTimeToProcess = DateTime.Now + sleepInterval;
                _lastSuccessfulPullTime = DateTime.Now;

                await SendDataToEventHub(observations);

                _lastCommittedTimestamp = observations.Max(o => o.Timestamp);
                await _checkpointing.SetLastCommittedTimestampAsync(StationShortId, _lastCommittedTimestamp.Value);

                Logger.TraceLine(
                    $"ProcessObservations({StationShortId}): {observations.Count} observations during the last " +
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

        private async Task SendDataToEventHub(List<StationObservation> observations)
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

        private static List<StationObservation> ParseObservartions(TextReader textReader)
        {
            JObject jObject = JsonUtils.ParseJson(textReader);

            JArray features = JsonUtils.GetPropertyValueOrNull<JArray>(jObject, "features");
            var observations = new List<StationObservation>(features.Count);
            foreach (JObject feature in features)
            {
                JObject properties = JsonUtils.GetPropertyValueOrNull<JObject>(feature, "properties");
                Debug.Assert(properties != null && properties["@type"].ToString() == "wx:ObservationStation", 
                                "properties != null && properties[\"@type\"].ToString() == \"wx:ObservationStation\"");

                JArray cloudLayers = JsonUtils.GetPropertyValueOrNull<JArray>(properties, "cloudLayers");
                observations.Add(new StationObservation(
                    stationId: properties["station"].ToString(),
                    timestamp: DateTimeOffset.Parse(properties["timestamp"].ToString()),
                    rawMessage: properties["rawMessage"].ToString(),
                    textDescription: properties["textDescription"].ToString(),
                    temperature: JsonUtils.GetPropertyValueOrNull<JObject>(properties, "temperature"),
                    dewpoint: JsonUtils.GetPropertyValueOrNull<JObject>(properties, "dewpoint"),
                    windDirection: JsonUtils.GetPropertyValueOrNull<JObject>(properties, "windDirection"),
                    windSpeed: JsonUtils.GetPropertyValueOrNull<JObject>(properties, "windSpeed"),
                    windGust: JsonUtils.GetPropertyValueOrNull<JObject>(properties, "windGust"),
                    barometricPressure: JsonUtils.GetPropertyValueOrNull<JObject>(properties, "barometricPressure"),
                    seaLevelPressure: JsonUtils.GetPropertyValueOrNull<JObject>(properties, "seaLevelPressure"),
                    visibility: JsonUtils.GetPropertyValueOrNull<JObject>(properties, "visibility"),
                    maxTemperatureLast24Hours: JsonUtils.GetPropertyValueOrNull<JObject>(properties, "maxTemperatureLast24Hours"),
                    minTemperatureLast24Hours: JsonUtils.GetPropertyValueOrNull<JObject>(properties, "minTemperatureLast24Hours"),
                    precipitationLastHour: JsonUtils.GetPropertyValueOrNull<JObject>(properties, "precipitationLastHour"),
                    precipitationLast3Hours: JsonUtils.GetPropertyValueOrNull<JObject>(properties, "precipitationLast3Hours"),
                    precipitationLast6Hours: JsonUtils.GetPropertyValueOrNull<JObject>(properties, "precipitationLast6Hours"),
                    relativeHumidity: JsonUtils.GetPropertyValueOrNull<JObject>(properties, "relativeHumidity"),
                    windChill: JsonUtils.GetPropertyValueOrNull<JObject>(properties, "windChill"),
                    heatIndex: JsonUtils.GetPropertyValueOrNull<JObject>(properties, "heatIndex"),
                    cloudLayer0: (JObject)cloudLayers?.FirstOrDefault()));
            }

            return observations;
        }
    }
}