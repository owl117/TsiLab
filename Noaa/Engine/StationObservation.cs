using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Engine
{
    public sealed class StationObservation
    {
        public static TimeSeriesType TimeSeriesType = new TimeSeriesType(
            id: "cde07f8f-ca64-4843-85d7-97df37b0a21e",
            name: "StationObservations",
            description: "NOAA weather station observations",
            variables: new Dictionary<string, TimeSeriesVariable>()
            {
                { "Temperature", new TimeSeriesVariable(
                    kind: TimeSeriesVariable.VariableKind.Numeric,
                    value: new TimeSeriesExpression("$event.Temperature_value.Double"),
                    filter: null,
                    aggregation: new TimeSeriesExpression("avg($value)")
                )},
                { "Dewpoint", new TimeSeriesVariable(
                    kind: TimeSeriesVariable.VariableKind.Numeric,
                    value: new TimeSeriesExpression("$event.Dewpoint_value.Double"),
                    filter: null,
                    aggregation: new TimeSeriesExpression("avg($value)")
                )},
                { "WindDirection", new TimeSeriesVariable(
                    kind: TimeSeriesVariable.VariableKind.Numeric,
                    value: new TimeSeriesExpression("$event.WindDirection_value.Double"),
                    filter: null,
                    aggregation: new TimeSeriesExpression("avg($value)")
                )},
                { "WindSpeed", new TimeSeriesVariable(
                    kind: TimeSeriesVariable.VariableKind.Numeric,
                    value: new TimeSeriesExpression("$event.WindSpeed_value.Double"),
                    filter: null,
                    aggregation: new TimeSeriesExpression("avg($value)")
                )},
                { "WindGust", new TimeSeriesVariable(
                    kind: TimeSeriesVariable.VariableKind.Numeric,
                    value: new TimeSeriesExpression("$event.WindGust_value.Double"),
                    filter: null,
                    aggregation: new TimeSeriesExpression("avg($value)")
                )},
                { "BarometricPressure", new TimeSeriesVariable(
                    kind: TimeSeriesVariable.VariableKind.Numeric,
                    value: new TimeSeriesExpression("$event.BarometricPressure_value.Double"),
                    filter: null,
                    aggregation: new TimeSeriesExpression("avg($value)")
                )},
                { "SeaLevelPressure", new TimeSeriesVariable(
                    kind: TimeSeriesVariable.VariableKind.Numeric,
                    value: new TimeSeriesExpression("$event.SeaLevelPressure_value.Double"),
                    filter: null,
                    aggregation: new TimeSeriesExpression("avg($value)")
                )},
                { "Visibility", new TimeSeriesVariable(
                    kind: TimeSeriesVariable.VariableKind.Numeric,
                    value: new TimeSeriesExpression("$event.Visibility_value.Double"),
                    filter: null,
                    aggregation: new TimeSeriesExpression("avg($value)")
                )},
                { "MaxTemperatureLast24Hours", new TimeSeriesVariable(
                    kind: TimeSeriesVariable.VariableKind.Numeric,
                    value: new TimeSeriesExpression("$event.MaxTemperatureLast24Hours_value.Double"),
                    filter: null,
                    aggregation: new TimeSeriesExpression("avg($value)")
                )},
                { "MinTemperatureLast24Hours", new TimeSeriesVariable(
                    kind: TimeSeriesVariable.VariableKind.Numeric,
                    value: new TimeSeriesExpression("$event.MinTemperatureLast24Hours_value.Double"),
                    filter: null,
                    aggregation: new TimeSeriesExpression("avg($value)")
                )},
                { "PrecipitationLastHour", new TimeSeriesVariable(
                    kind: TimeSeriesVariable.VariableKind.Numeric,
                    value: new TimeSeriesExpression("$event.PrecipitationLastHour_value.Double"),
                    filter: null,
                    aggregation: new TimeSeriesExpression("avg($value)")
                )},
                { "PrecipitationLast3Hours", new TimeSeriesVariable(
                    kind: TimeSeriesVariable.VariableKind.Numeric,
                    value: new TimeSeriesExpression("$event.PrecipitationLast3Hours_value.Double"),
                    filter: null,
                    aggregation: new TimeSeriesExpression("avg($value)")
                )},
                { "PrecipitationLast6Hours", new TimeSeriesVariable(
                    kind: TimeSeriesVariable.VariableKind.Numeric,
                    value: new TimeSeriesExpression("$event.PrecipitationLast6Hours_value.Double"),
                    filter: null,
                    aggregation: new TimeSeriesExpression("avg($value)")
                )},
                { "RelativeHumidity", new TimeSeriesVariable(
                    kind: TimeSeriesVariable.VariableKind.Numeric,
                    value: new TimeSeriesExpression("$event.RelativeHumidity_value.Double"),
                    filter: null,
                    aggregation: new TimeSeriesExpression("avg($value)")
                )},
                { "WindChill", new TimeSeriesVariable(
                    kind: TimeSeriesVariable.VariableKind.Numeric,
                    value: new TimeSeriesExpression("$event.WindChill_value.Double"),
                    filter: null,
                    aggregation: new TimeSeriesExpression("avg($value)")
                )},
                { "HeatIndex", new TimeSeriesVariable(
                    kind: TimeSeriesVariable.VariableKind.Numeric,
                    value: new TimeSeriesExpression("$event.HeatIndex_value.Double"),
                    filter: null,
                    aggregation: new TimeSeriesExpression("avg($value)")
                )},
                { "CloudLayer0_base", new TimeSeriesVariable(
                    kind: TimeSeriesVariable.VariableKind.Numeric,
                    value: new TimeSeriesExpression("$event.CloudLayer0_base_value.Double"),
                    filter: null,
                    aggregation: new TimeSeriesExpression("avg($value)")
                )},
            }
        );

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