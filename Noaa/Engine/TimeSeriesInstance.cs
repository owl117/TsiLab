using System.Collections.Generic;
using Newtonsoft.Json;

namespace Engine
{
    public sealed class TimeSeriesInstance
    {
        public TimeSeriesInstance(
            object[] timeSeriesId,
            string typeId,
            string description,
            Dictionary<string, string> instanceFields,
            string[] hierarchyIds)
        {
            TimeSeriesId = timeSeriesId;
            TypeId = typeId;
            Description = description;
            InstanceFields = instanceFields;
            HierarchyIds = hierarchyIds;
        }

        [JsonProperty(PropertyName = "timeSeriesId")]
        public object[] TimeSeriesId { get; private set; }

        [JsonProperty(PropertyName = "typeId")]
        public string TypeId { get; private set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; private set; }

        [JsonProperty(PropertyName = "instanceFields")]
        public Dictionary<string, string> InstanceFields { get; private set; }

        [JsonProperty(PropertyName = "hierarchyIds")]
        public string[] HierarchyIds { get; private set; }
    }
}