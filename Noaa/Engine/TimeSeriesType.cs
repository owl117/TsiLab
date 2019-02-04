using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Engine
{
    public sealed class TimeSeriesType
    {
        public void Write(string filename)
        {
            using (var sr = System.IO.File.CreateText(filename))
            {
                JsonUtils.WriteJson(sr, this);
            }
        }
        public TimeSeriesType(string id, string name, string description, TimeSeriesVariable[] variables)
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
        public TimeSeriesVariable[] Variables { get; private set; }
    }
}

/*

{
  "put": [
    {
      "id": "1be09af9-f089-4d6b-9f0b-48018b5f7393",
      "name": "DefaultType",
      "description": "My Default type",
      "variables": {
        "EventCount": {
          "kind": "aggregate",
          "value": null,
          "filter": null,
          "aggregation": {
            "tsx": "count()"
          }
        }
      }
    },
    {
      "id": "c1cb7a33-ed9b-4cf1-9958-f3162fed8ee8",
      "name": "OutdoorTemperatureSensor",
      "description": "This is an outdoor temperature sensor.",
      "variables": {
        "AverageTemperature": {
          "kind": "numeric",
          "value": {
            "tsx": "$event.Temperature.Double"
          },
          "filter": {
            "tsx": "$event.Mode.String = 'outdoor'"
          },
          "aggregation": {
            "tsx": "avg($value)"
          }
        }
      }
  }]
}
 */