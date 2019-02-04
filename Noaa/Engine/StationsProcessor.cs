using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Engine
{
    public sealed class StastionsProcessor
    {
        public StastionsProcessor()
        {
            Stations = new List<Station>();
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
                
                stations.Add(new Station(
                    id: properties["@id"].ToString(),
                    shortId: properties["stationIdentifier"].ToString(),
                    name: properties["name"].ToString(),
                    timeZone: properties["timeZone"].ToString()));
            }

            return stations;
        }
    }
}