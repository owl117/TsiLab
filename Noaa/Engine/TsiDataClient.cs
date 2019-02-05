using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Engine
{
    public sealed class TsiDataClient
    {
        private readonly string _accessToken;

        private TsiDataClient(string accessToken)
        {
            _accessToken = accessToken;
        }

        public static async Task<TsiDataClient> AadLoginAsApplicationAsync(AzureUtils.ApplicationClientInfo applicationClientInfo)
        {
            string accessToken = await AzureUtils.AadLoginAsApplicationAsync("https://api.timeseries.azure.com/", applicationClientInfo);
            return new TsiDataClient(accessToken);
        }

        public async Task<EnvironmentInfo[]> GetEnvironmentsAsync()
        {
            return await MakeTsiDataApiCall(
                environmentFqdn: null,
                method: "GET",
                path: "environments",
                writeRequestBody: null,
                consumeResponseTextReader: ParseGetEnvironmentsResponse);
        }

        public async Task<TimeSeriesType[]> GetTimeSeriesTypesAsync(string environmentFqdn)
        {
            return await MakeTsiDataApiCall(
                environmentFqdn,
                "GET",
                "timeseries/types",
                writeRequestBody: null,
                consumeResponseTextReader: ParseGetTimeSeriesTypesResponse);
        }

        public async Task<BatchResult[]> PutTimeSeriesTypesAsync(string environmentFqdn, TimeSeriesType[] types)
        {
            return await MakeTsiDataApiCall(
                environmentFqdn,
                "POST",
                "timeseries/types/$batch",
                textWriter => WritePutTimeSeriesTypesRequest(textWriter, types),
                ParsePutTimeSeriesTypesResponse);
        }

        public async Task<TimeSeriesInstance[]> GetTimeSeriesInstancesAsync(string environmentFqdn)
        {
            return await MakeTsiDataApiCall(
                environmentFqdn,
                "GET",
                "timeseries/instances",
                writeRequestBody: null,
                consumeResponseTextReader: ParseGetTimeSeriesInstancesResponse);
        }

        public async Task<TimeSeriesInstanceBatchResult[]> PutTimeSeriesInstancesAsync(string environmentFqdn, TimeSeriesInstance[] instances)
        {
            return await MakeTsiDataApiCall(
                environmentFqdn,
                "POST",
                "timeseries/instances/$batch",
                textWriter => WritePutTimeSeriesInstancesRequest(textWriter, instances),
                ParsePutTimeSeriesInstancesResponse);
        }

        private static EnvironmentInfo[] ParseGetEnvironmentsResponse(TextReader textReader)
        {
            GetEnvironmentsResponse getEnvironmentsResponse = JsonUtils.ParseJson<GetEnvironmentsResponse>(textReader);
            return (getEnvironmentsResponse?.environments) ?? new EnvironmentInfo[0];
        }

        private static TimeSeriesType[] ParseGetTimeSeriesTypesResponse(TextReader textReader)
        {
            GetTimeSeriesTypesResponse getTimeSeriesTypesResponse = JsonUtils.ParseJson<GetTimeSeriesTypesResponse>(textReader);
            return (getTimeSeriesTypesResponse?.types) ?? new TimeSeriesType[0];
        }

        private static void WritePutTimeSeriesTypesRequest(TextWriter textWriter, TimeSeriesType[] types)
        {
            JsonUtils.WriteJson(textWriter, new PutTimeSeriesTypesRequest(types));
        }

        private static BatchResult[] ParsePutTimeSeriesTypesResponse(TextReader textReader)
        {
            PutTimeSeriesTypesResponse putTimeSeriesTypesResponse = JsonUtils.ParseJson<PutTimeSeriesTypesResponse>(textReader);
            return (putTimeSeriesTypesResponse
                   ?.put
                   ?.Select(timeSeriesTypeInfo => new BatchResult(timeSeriesTypeInfo?.timeSeriesType?.id,
                                                                  timeSeriesTypeInfo?.error?.ToString()))
                   .ToArray())
                   ?? new BatchResult[0];
        }

        private static TimeSeriesInstance[] ParseGetTimeSeriesInstancesResponse(TextReader textReader)
        {
            GetTimeSeriesInstancesResponse getTimeSeriesInstancesResponse = JsonUtils.ParseJson<GetTimeSeriesInstancesResponse>(textReader);
            return (getTimeSeriesInstancesResponse?.instances) ?? new TimeSeriesInstance[0];
        }

        private static void WritePutTimeSeriesInstancesRequest(TextWriter textWriter, TimeSeriesInstance[] instances)
        {
            JsonUtils.WriteJson(textWriter, new PutTimeSeriesInstancesRequest(instances));
        }

        private static TimeSeriesInstanceBatchResult[] ParsePutTimeSeriesInstancesResponse(TextReader textReader)
        {
            PutTimeSeriesInstancesResponse putTimeSeriesInstancesResponse = JsonUtils.ParseJson<PutTimeSeriesInstancesResponse>(textReader);
            return (putTimeSeriesInstancesResponse
                   ?.put
                   ?.Select(timeSeriesInstanceInfo => new TimeSeriesInstanceBatchResult(timeSeriesInstanceInfo?.instance?.timeSeriesId,
                                                                                        timeSeriesInstanceInfo?.error?.ToString()))
                   .ToArray())
                   ?? new TimeSeriesInstanceBatchResult[0];
        }

        private async Task<TResult> MakeTsiDataApiCall<TResult>(
            string environmentFqdn,
            string method,
            string path,
            Action<TextWriter> writeRequestBody,
            Func<TextReader, TResult> consumeResponseTextReader)
        {
            return await HttpUtils.MakeHttpCallAsync(
                environmentFqdn ?? "api.timeseries.azure.com",
                method,
                path,
                _accessToken,
                "TsiDataClient",
                textWriter => { writeRequestBody(textWriter); return "application/json"; },
                consumeResponseTextReader,
                new string[] { "api-version=2018-11-01-preview" }); // v1: api-version=2016-12-12 
        }

        public sealed class EnvironmentInfo
        {
            public EnvironmentInfo(
                string environmentId,
                string environmentFqdn,
                string displayName,
                string resourceId,
                Role[] roles)
            {
                EnvironmentId = environmentId;
                EnvironmentFqdn = environmentFqdn;
                DisplayName = displayName;
                ResourceId = resourceId;
                Roles = roles;
            }

            [JsonProperty(PropertyName = "environmentId")]
            public string EnvironmentId { get; private set; }

            [JsonProperty(PropertyName = "environmentFqdn")]
            public string EnvironmentFqdn { get; private set; }

            [JsonProperty(PropertyName = "displayName")]
            public string DisplayName { get; private set; }

            [JsonProperty(PropertyName = "resourceId")]
            public string ResourceId { get; private set; }

            [JsonProperty(PropertyName = "roles")]
            public Role[] Roles { get; private set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public enum Role
            {
                Reader,
                Contributor
            }
        }

        public sealed class BatchResult
        {
            public BatchResult(string itemId, string error)
            {
                ItemId = itemId;
                Error = error;
            }

            public string ItemId { get; private set; }
            public string Error { get; private set; }
        }

        public sealed class TimeSeriesInstanceBatchResult
        {
            public TimeSeriesInstanceBatchResult(object[] timeSeriesId, string error)
            {
                TimeSeriesId = timeSeriesId;
                Error = error;
            }

            public object[] TimeSeriesId { get; private set; }
            public string Error { get; private set; }
        }

        #pragma warning disable 649
        private sealed class GetEnvironmentsResponse
        {
            public EnvironmentInfo[] environments;
        }

        private sealed class GetTimeSeriesTypesResponse
        {
            public TimeSeriesType[] types;
        }

        private sealed class PutTimeSeriesTypesRequest
        {
            public PutTimeSeriesTypesRequest(TimeSeriesType[] put)
            {
                this.put = put;
            }

            public TimeSeriesType[] put;
        }

        private sealed class PutTimeSeriesTypesResponse
        {
            public TimeSeriesTypeInfo[] put;

            public sealed class TimeSeriesTypeInfo
            {
                public TimeSeriesTypeId timeSeriesType;
                public JToken error;

                public sealed class TimeSeriesTypeId
                {
                    public string id;
                }
            }
        }

        private sealed class GetTimeSeriesInstancesResponse
        {
            public TimeSeriesInstance[] instances;
            public string continuationToken;
        }

        private sealed class PutTimeSeriesInstancesRequest
        {
            public PutTimeSeriesInstancesRequest(TimeSeriesInstance[] put)
            {
                this.put = put;
            }

            public TimeSeriesInstance[] put;
        }

        private sealed class PutTimeSeriesInstancesResponse
        {
            public TimeSeriesInstanceInfo[] put;

            public sealed class TimeSeriesInstanceInfo
            {
                public TimeSeriesInstanceId instance;
                public JToken error;

                public sealed class TimeSeriesInstanceId
                {
                    public string[] timeSeriesId;
                }
            }
        }
        #pragma warning restore 649
    }
}