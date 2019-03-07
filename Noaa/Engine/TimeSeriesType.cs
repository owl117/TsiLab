using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Engine
{
    public sealed class TimeSeriesType
    {
        public TimeSeriesType(string id, string name, string description, Dictionary<string, TimeSeriesVariable> variables)
        {
            Id = id;
            Name = name;
            Description = description;
            Variables = variables;
        }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; private set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; private set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; private set; }

        [JsonProperty(PropertyName = "variables")]
        public Dictionary<string, TimeSeriesVariable> Variables { get; private set; }
    }
}