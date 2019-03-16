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
        private static TimeSpan AccessTokenRefreshAge = TimeSpan.FromMinutes(59);

        private readonly Func<Task<string>> _refreshAccessTokenAsync;
        /// <summary>
        /// Do not access this field directly. Use GetAccessTokenAsync().
        /// </summary>
        private string _accessToken;
        private DateTime _accessTokenAge;

        private TsiDataClient(Func<Task<string>> refreshAccessTokenAsync)
        {
            _refreshAccessTokenAsync = refreshAccessTokenAsync;
            _accessToken = null;
            _accessTokenAge = DateTime.MinValue;
        }

        public static TsiDataClient AadLoginAsApplication(AzureUtils.ApplicationClientInfo applicationClientInfo)
        {
            return new TsiDataClient(() => AzureUtils.AadLoginAsApplicationAsync("https://api.timeseries.azure.com/", applicationClientInfo));
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

        public async Task<TimeSeriesHierarchy[]> GetTimeSeriesHierarchiesAsync(string environmentFqdn)
        {
            return await MakeTsiDataApiCall(
                environmentFqdn,
                "GET",
                "timeseries/hierarchies",
                writeRequestBody: null,
                consumeResponseTextReader: ParseGetTimeSeriesHierarchiesResponse);
        }

        public async Task<BatchResult[]> PutTimeSeriesHierarchiesAsync(string environmentFqdn, TimeSeriesHierarchy[] hierarchies)
        {
            return await MakeTsiDataApiCall(
                environmentFqdn,
                "POST",
                "timeseries/hierarchies/$batch",
                textWriter => WritePutTimeSeriesHierarchiesRequest(textWriter, hierarchies),
                ParsePutTimeSeriesHierarchiesResponse);
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

        public async Task<BatchResult[]> PutTimeSeriesInstancesAsync(string environmentFqdn, TimeSeriesInstance[] instances)
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

        private static TimeSeriesHierarchy[] ParseGetTimeSeriesHierarchiesResponse(TextReader textReader)
        {
            GetTimeSeriesHierarchiesResponse getTimeSeriesHierarchiesResponse = JsonUtils.ParseJson<GetTimeSeriesHierarchiesResponse>(textReader);
            return (getTimeSeriesHierarchiesResponse?.hierarchies) ?? new TimeSeriesHierarchy[0];
        }

        private static void WritePutTimeSeriesHierarchiesRequest(TextWriter textWriter, TimeSeriesHierarchy[] hierarchies)
        {
            JsonUtils.WriteJson(textWriter, new PutTimeSeriesHierarchiesRequest(hierarchies));
        }

        private static BatchResult[] ParsePutTimeSeriesHierarchiesResponse(TextReader textReader)
        {
            PutTimeSeriesHierarchiesResponse putTimeSeriesHierarchiesResponse = JsonUtils.ParseJson<PutTimeSeriesHierarchiesResponse>(textReader);
            return (putTimeSeriesHierarchiesResponse
                   ?.put
                   ?.Select(timeSeriesHierarchyInfo => new BatchResult(timeSeriesHierarchyInfo?.timeSeriesHierarchy?.id,
                                                                       timeSeriesHierarchyInfo?.error?.ToString()))
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

        private static BatchResult[] ParsePutTimeSeriesInstancesResponse(TextReader textReader)
        {
            PutTimeSeriesInstancesResponse putTimeSeriesInstancesResponse = JsonUtils.ParseJson<PutTimeSeriesInstancesResponse>(textReader);
            return (putTimeSeriesInstancesResponse
                   ?.put
                   ?.Select(timeSeriesInstanceInfo => new BatchResult(timeSeriesInstanceInfo?.instance?.timeSeriesId,
                                                                      timeSeriesInstanceInfo?.error?.ToString()))
                   .ToArray())
                   ?? new BatchResult[0];
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
                await GetAccessTokenAsync(),
                "TsiDataClient",
                textWriter => { writeRequestBody(textWriter); return "application/json"; },
                consumeResponseTextReader,
                new string[] { "api-version=2018-11-01-preview" }); // v1: api-version=2016-12-12 
        }

        private async Task<string> GetAccessTokenAsync()
        {
            if (DateTime.Now - _accessTokenAge >= AccessTokenRefreshAge)
            {
                _accessToken = await _refreshAccessTokenAsync();
                _accessTokenAge = DateTime.Now;
            }

            return _accessToken;
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

        private sealed class GetTimeSeriesHierarchiesResponse
        {
            public TimeSeriesHierarchy[] hierarchies;
        }

        private sealed class PutTimeSeriesHierarchiesRequest
        {
            public PutTimeSeriesHierarchiesRequest(TimeSeriesHierarchy[] put)
            {
                this.put = put;
            }

            public TimeSeriesHierarchy[] put;
        }

        private sealed class PutTimeSeriesHierarchiesResponse
        {
            public TimeSeriesHierarchyInfo[] put;

            public sealed class TimeSeriesHierarchyInfo
            {
                public TimeSeriesHierarchyId timeSeriesHierarchy;
                public JToken error;

                public sealed class TimeSeriesHierarchyId
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
                    public string timeSeriesId;
                }
            }
        }
        #pragma warning restore 649
    }
}