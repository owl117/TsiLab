using Newtonsoft.Json;

namespace Engine
{
    public sealed class TimeSeriesHierarchySource
    {
        public TimeSeriesHierarchySource(string[] instanceFieldNames)
        {
            InstanceFieldNames = instanceFieldNames;
        }

        [JsonProperty(PropertyName = "instanceFieldNames")]
        public string[] InstanceFieldNames { get; private set; }
    }
}