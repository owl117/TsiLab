using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Engine
{
    public sealed class NoaaClient
    {
        public NoaaClient()
        {
        }

        public async Task<Station[]> GetStationsAsync()
        {
            return await MakeNoaaApiCall(
                "https://api.weather.gov/stations",
                ParseGetStationsResponse);
        }

        public async Task<StationObservation[]> GetStationObservationsAsync(string stationShortId, DateTimeOffset start)
        {
            return await MakeNoaaApiCall(
                $"https://api.weather.gov/stations/{stationShortId}/observations?start=" + 
                    Uri.EscapeDataString(start.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:sszzz")),
                ParseGetStationObservationsResponse);
        }

        private static Station[] ParseGetStationsResponse(TextReader textReader)
        {
            GetStationsResponse getStationsResponse = JsonUtils.ParseJson<GetStationsResponse>(textReader);

            if (getStationsResponse != null && getStationsResponse.features != null)
            {
                return getStationsResponse.features
                                          .Where(f => f.type == "Feature" &&
                                                      f.properties?.type == "wx:ObservationStation" &&
                                                      f.geometry?.type == "Point")
                                          .Select(f => new Station(
                                              id: f.id,
                                              shortId: f.properties.stationIdentifier,
                                              name: f.properties.name,
                                              timeZone: f.properties.timeZone,
                                              latitude: f.geometry?.coordinates[1],
                                              longitude: f.geometry?.coordinates[0]))
                                          .ToArray();
            }
            else
            {
                return new Station[0];
            }
        }

        private static StationObservation[] ParseGetStationObservationsResponse(TextReader textReader)
        {
            GetStationObservationsResponse getStationObservationsResponse = JsonUtils.ParseJson<GetStationObservationsResponse>(textReader);

            if (getStationObservationsResponse != null && getStationObservationsResponse.features != null)
            {
                return getStationObservationsResponse.features
                                          .Where(f => f.type == "Feature" &&
                                                      f.properties?.type == "wx:ObservationStation")
                                          .Select(f => new StationObservation(
                                              stationId: f.properties.station,
                                              timestamp: f.properties.timestamp,
                                              rawMessage: f.properties.rawMessage,
                                              textDescription: f.properties.textDescription,
                                              temperature: f.properties.temperature,
                                              dewpoint: f.properties.dewpoint,
                                              windDirection: f.properties.windDirection,
                                              windSpeed: f.properties.windSpeed,
                                              windGust: f.properties.windGust,
                                              barometricPressure: f.properties.barometricPressure,
                                              seaLevelPressure: f.properties.seaLevelPressure,
                                              visibility: f.properties.visibility,
                                              maxTemperatureLast24Hours: f.properties.maxTemperatureLast24Hours,
                                              minTemperatureLast24Hours: f.properties.minTemperatureLast24Hours,
                                              precipitationLastHour: f.properties.precipitationLastHour,
                                              precipitationLast3Hours: f.properties.precipitationLast3Hours,
                                              precipitationLast6Hours: f.properties.precipitationLast6Hours,
                                              relativeHumidity: f.properties.relativeHumidity,
                                              windChill: f.properties.windChill,
                                              heatIndex: f.properties.heatIndex,
                                              cloudLayer0: f.properties.cloudLayers.FirstOrDefault()))
                                          .ToArray();
            }
            else
            {
                return new StationObservation[0];
            }
        }

        private async Task<TResult> MakeNoaaApiCall<TResult>(
            string requestUriString,
            Func<TextReader, TResult> consumeResponseTextReader)
        {
            try
            {
                return await HttpUtils.MakeHttpCallAsync(requestUriString, consumeResponseTextReader, pretendBrowser: true); 
            }
            catch (Exception e)
            {
                WebException webException = Retry.UnwrapAggregateException<WebException>(e);
                if (webException != null)
                {
                    HttpWebResponse response = (HttpWebResponse)webException.Response;
                    if (response != null && response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        throw new NoaaThrottlingDetected();
                    }
                }

                throw;
            }
        }

        #pragma warning disable 649
        private sealed class GetStationsResponse
        {
            public Feature[] features;

            public sealed class Feature
            {
                public string id;
                public string type;
                public Geometry geometry;
                public Properties properties;

                public sealed class Geometry
                {
                    public string type;
                    public double[] coordinates;
                }

                public sealed class Properties
                {
                    [JsonProperty(PropertyName = "@type")]
                    public string type; 
                    public string stationIdentifier;
                    public string name;
                    public string timeZone;
                }
            }
        }

        private sealed class GetStationObservationsResponse
        {
            public Feature[] features;

            public sealed class Feature
            {
                public string type;
                public Properties properties;

                public sealed class Properties
                {
                    [JsonProperty(PropertyName = "@type")]
                    public string type; 
                    public string station;
                    public DateTimeOffset timestamp;
                    public string rawMessage;
                    public string textDescription;

                    public StationObservation.Measurement temperature;
                    public StationObservation.Measurement dewpoint;
                    public StationObservation.Measurement windDirection;
                    public StationObservation.Measurement windSpeed;
                    public StationObservation.Measurement windGust;
                    public StationObservation.Measurement barometricPressure;
                    public StationObservation.Measurement seaLevelPressure;
                    public StationObservation.Measurement visibility;
                    public StationObservation.Measurement maxTemperatureLast24Hours;
                    public StationObservation.Measurement minTemperatureLast24Hours;
                    public StationObservation.Measurement precipitationLastHour;
                    public StationObservation.Measurement precipitationLast3Hours;
                    public StationObservation.Measurement precipitationLast6Hours;
                    public StationObservation.Measurement relativeHumidity;
                    public StationObservation.Measurement windChill;
                    public StationObservation.Measurement heatIndex;
                    public StationObservation.CloudLayer[] cloudLayers;
                }
            }
        }
        #pragma warning restore 649
    }
}