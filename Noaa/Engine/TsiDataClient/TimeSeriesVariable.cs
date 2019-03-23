using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Engine
{
    public sealed class TimeSeriesVariable
    {
        public TimeSeriesVariable(
            VariableKind kind,
            TimeSeriesExpression value,
            TimeSeriesExpression filter,
            TimeSeriesExpression aggregation)
        {
            Kind = kind;
            Value = value;
            Filter = filter;
            Aggregation = aggregation;
        }

        [JsonProperty(PropertyName = "kind")]
        public VariableKind Kind { get; private set; }

        [JsonProperty(PropertyName = "value")]
        public TimeSeriesExpression Value { get; private set; }

        [JsonProperty(PropertyName = "filter")]
        public TimeSeriesExpression Filter { get; private set; }

        [JsonProperty(PropertyName = "aggregation")]
        public TimeSeriesExpression Aggregation { get; private set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum VariableKind
        {
            [EnumMember(Value = "numeric")] 
            Numeric,

            [EnumMember(Value = "aggregate")] 
            Aggregate
        }
    }
}