using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Engine
{
    public sealed class TimeSeriesInstance
    {
        public TimeSeriesInstance(
            object[] timeSeriesId,
            string typeId,
            string name,
            string description,
            Dictionary<string, string> instanceFields,
            string[] hierarchyIds)
        {
            TimeSeriesId = timeSeriesId;
            TypeId = typeId;
            Name = name;
            Description = description;

            // Instance fields with null value are not supported.
            if (instanceFields != null)
            {
                instanceFields = instanceFields.Where(kvp => !String.IsNullOrEmpty(kvp.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                instanceFields = instanceFields.Count > 0 ? instanceFields : null;
            }
            InstanceFields = instanceFields;
            
            HierarchyIds = hierarchyIds;
        }

        [JsonProperty(PropertyName = "timeSeriesId")]
        public object[] TimeSeriesId { get; private set; }

        [JsonProperty(PropertyName = "typeId")]
        public string TypeId { get; private set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; private set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; private set; }

        [JsonProperty(PropertyName = "instanceFields")]
        public Dictionary<string, string> InstanceFields { get; private set; }

        [JsonProperty(PropertyName = "hierarchyIds")]
        public string[] HierarchyIds { get; private set; }
    }
}