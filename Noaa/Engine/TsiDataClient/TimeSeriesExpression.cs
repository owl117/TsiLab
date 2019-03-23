using Newtonsoft.Json;

namespace Engine
{
    public sealed class TimeSeriesExpression
    {
        public TimeSeriesExpression(string tsx)
        {
            Tsx = tsx;
        }

        [JsonProperty(PropertyName = "tsx")]
        public string Tsx { get; private set; }
    }
}