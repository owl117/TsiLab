using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Engine
{
    public static class JsonUtils
    {
        public static JObject ParseJson(TextReader textReader)
        {
            using (var jsonTextReader = new JsonTextReader(textReader))
            {
                return (JObject)new JsonSerializer() { DateParseHandling = DateParseHandling.None }.Deserialize(jsonTextReader);
            }
        }

        public static TResult ParseJson<TResult>(TextReader textReader)
        {
            using (var jsonTextReader = new JsonTextReader(textReader))
            {
                return new JsonSerializer() { DateParseHandling = DateParseHandling.None }.Deserialize<TResult>(jsonTextReader);
            }
        }

        public static void WriteJson(TextWriter textWriter, object value)
        {
                new JsonSerializer() { DateParseHandling = DateParseHandling.None }.Serialize(textWriter, value);
        }

        public static TResult GetPropertyValueOrNull<TResult>(JObject jObject, string propertyName) where TResult : JToken
        {
            JToken jToken = jObject[propertyName];
            return jToken == null || jToken.Type == JTokenType.Null ? null : (TResult)jToken;
        }
    }
}