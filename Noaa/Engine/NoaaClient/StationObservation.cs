using System;
using System.Collections.Generic;
using Newtonsoft.Json;
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
            Measurement temperature,
            Measurement dewpoint,
            Measurement windDirection,
            Measurement windSpeed,
            Measurement windGust,
            Measurement barometricPressure,
            Measurement seaLevelPressure,
            Measurement visibility,
            Measurement maxTemperatureLast24Hours,
            Measurement minTemperatureLast24Hours,
            Measurement precipitationLastHour,
            Measurement precipitationLast3Hours,
            Measurement precipitationLast6Hours,
            Measurement relativeHumidity,
            Measurement windChill,
            Measurement heatIndex,
            CloudLayer cloudLayer0)
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
        public Measurement Temperature { get; private set; }
        public Measurement Dewpoint { get; private set; }
        public Measurement WindDirection { get; private set; }
        public Measurement WindSpeed { get; private set; }
        public Measurement WindGust { get; private set; }
        public Measurement BarometricPressure { get; private set; }
        public Measurement SeaLevelPressure { get; private set; }
        public Measurement Visibility { get; private set; }
        public Measurement MaxTemperatureLast24Hours { get; private set; }
        public Measurement MinTemperatureLast24Hours { get; private set; }
        public Measurement PrecipitationLastHour { get; private set; }
        public Measurement PrecipitationLast3Hours { get; private set; }
        public Measurement PrecipitationLast6Hours { get; private set; }
        public Measurement RelativeHumidity { get; private set; }
        public Measurement WindChill { get; private set; }
        public Measurement HeatIndex { get; private set; }
        public CloudLayer CloudLayer0 { get; private set; }

        // !!! This class is reused for NOAA API response deserialization !!!
        public sealed class Measurement
        {
            public Measurement(
                double? value,
                string unitCode,
                string qualityControl)
            {
                Value = value;
                UnitCode = unitCode;
                QualityControl = qualityControl;
            }

            [JsonProperty(PropertyName = "value")]
            public double? Value { get; private set; }

            [JsonProperty(PropertyName = "unitCode")]
            public string UnitCode { get; private set; }

            [JsonProperty(PropertyName = "qualityControl")]
            public string QualityControl { get; private set; }
        }

        // !!! This class is reused for NOAA API response deserialization !!!
        public sealed class CloudLayer
        {
            public CloudLayer(Measurement cloudBase, string amount)
            {
                CloudBase = cloudBase;
                Amount = amount;
            }

            [JsonProperty(PropertyName = "base")]
            public Measurement CloudBase;
            public string Amount;
        }
    }
}