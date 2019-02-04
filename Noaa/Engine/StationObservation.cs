using System;
using Newtonsoft.Json.Linq;

namespace Engine
{
    public sealed class StationObservation
    {
        public StationObservation(
            string stationId,
            DateTimeOffset timestamp,
            string rawMessage,
            string textDescription,
            JObject temperature,
            JObject dewpoint,
            JObject windDirection,
            JObject windSpeed,
            JObject windGust,
            JObject barometricPressure,
            JObject seaLevelPressure,
            JObject visibility,
            JObject maxTemperatureLast24Hours,
            JObject minTemperatureLast24Hours,
            JObject precipitationLastHour,
            JObject precipitationLast3Hours,
            JObject precipitationLast6Hours,
            JObject relativeHumidity,
            JObject windChill,
            JObject heatIndex,
            JObject cloudLayer0)
        {
            StationId = stationId;
            Timestamp = timestamp;
            RawMessage = rawMessage;
            TextDescription = textDescription;
            Temperature = temperature;
            Dewpoint = dewpoint;
            WindDirection = windDirection;
            WindSpeed = windSpeed;
            WindGust = windGust;
            BarometricPressure = barometricPressure;
            SeaLevelPressure = seaLevelPressure;
            Visibility = visibility;
            MaxTemperatureLast24Hours = maxTemperatureLast24Hours;
            MinTemperatureLast24Hours = minTemperatureLast24Hours;
            PrecipitationLastHour = precipitationLastHour;
            PrecipitationLast3Hours = precipitationLast3Hours;
            PrecipitationLast6Hours = precipitationLast6Hours;
            RelativeHumidity = relativeHumidity;
            WindChill = windChill;
            HeatIndex = heatIndex;
            CloudLayer0 = cloudLayer0;
        }

        public string StationId { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }
        public string RawMessage { get; private set; }
        public string TextDescription { get; private set; }
        public JObject Temperature { get; private set; }
        public JObject Dewpoint { get; private set; }
        public JObject WindDirection { get; private set; }
        public JObject WindSpeed { get; private set; }
        public JObject WindGust { get; private set; }
        public JObject BarometricPressure { get; private set; }
        public JObject SeaLevelPressure { get; private set; }
        public JObject Visibility { get; private set; }
        public JObject MaxTemperatureLast24Hours { get; private set; }
        public JObject MinTemperatureLast24Hours { get; private set; }
        public JObject PrecipitationLastHour { get; private set; }
        public JObject PrecipitationLast3Hours { get; private set; }
        public JObject PrecipitationLast6Hours { get; private set; }
        public JObject RelativeHumidity { get; private set; }
        public JObject WindChill { get; private set; }
        public JObject HeatIndex { get; private set; }
        public JObject CloudLayer0 { get; private set; }
    }
}