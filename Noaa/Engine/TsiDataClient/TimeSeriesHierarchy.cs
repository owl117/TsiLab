using Newtonsoft.Json;

namespace Engine
{
    public sealed class TimeSeriesHierarchy
    {
        public TimeSeriesHierarchy(string id, string name, TimeSeriesHierarchySource source)
        {
            Id = id;
            Name = name;
            Source = source;
        }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; private set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; private set; }

        [JsonProperty(PropertyName = "source")]
        public TimeSeriesHierarchySource Source { get; private set; }
    }
}