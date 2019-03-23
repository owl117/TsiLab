using System.Collections.Generic;

namespace Engine
{
    public static class TsmMapping
    {
        public static class GeoLocationMetadata
        {
            public const string InstanceFieldName_Country = "Country";
            public const string InstanceFieldName_CountrySubdivisionName = "CountrySubdivisionName";
            public const string InstanceFieldName_CountrySecondarySubdivision = "CountrySecondarySubdivision";
            public const string InstanceFieldName_Municipality = "Municipality";
            public const string InstanceFieldName_PostalCode = "PostalCode";

            public static TimeSeriesHierarchy GeoLocationsHierarchy = new TimeSeriesHierarchy(
                id: "cde07f8f-ca64-4843-85d7-97df37b0a21e",
                name: "Geo Locations",
                source: new TimeSeriesHierarchySource(instanceFieldNames: new [] 
                    {
                        InstanceFieldName_Country,
                        InstanceFieldName_CountrySubdivisionName,
                        InstanceFieldName_CountrySecondarySubdivision 
                    }));
        }
        
        public static TimeSeriesType StationObservationsType = new TimeSeriesType(
            id: "cde07f8f-ca64-4843-85d7-97df37b0a21e",
            name: "Station Observations",
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
                )}
            }
        );
    }
}