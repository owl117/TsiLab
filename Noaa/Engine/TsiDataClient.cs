using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
            return await MakeTsiDataApiCall("GET", "environments", ParseGetEnvironmentsResponse);
        }

        public async Task<TimeSeriesType[]> GetTimeSeriesTypesAsync()
        {
            return await MakeTsiDataApiCall("GET", "timeseries/types", ParseGetTimeSeriesTypesResponse);
        }

        public async Task<TimeSeriesType[]> PutTimeSeriesTypesAsync()
        {
            return await MakeTsiDataApiCall("GET", "timeseries/types", ParseGetTimeSeriesTypesResponse);
        }

        public async Task<TimeSeriesInstance[]> GetTimeSeriesInstancesAsync()
        {
            return await MakeTsiDataApiCall("GET", "timeseries/instances", ParseGetTimeSeriesInstancesResponse);
        }

        private static EnvironmentInfo[] ParseGetEnvironmentsResponse(TextReader textReader)
        {
            GetEnvironmentsResponse getEnvironmentsResponse = JsonUtils.ParseJson<GetEnvironmentsResponse>(textReader);
            return (getEnvironmentsResponse?.environments)?? new EnvironmentInfo[0];
        }

        private static TimeSeriesType[] ParseGetTimeSeriesTypesResponse(TextReader textReader)
        {
            GetTimeSeriesTypesResponse getTimeSeriesTypesResponse = JsonUtils.ParseJson<GetTimeSeriesTypesResponse>(textReader);
            return (getTimeSeriesTypesResponse?.types)?? new TimeSeriesType[0];
        }

        private static TimeSeriesInstance[] ParseGetTimeSeriesInstancesResponse(TextReader textReader)
        {
            GetTimeSeriesInstancesResponse getTimeSeriesInstancesResponse = JsonUtils.ParseJson<GetTimeSeriesInstancesResponse>(textReader);
            return (getTimeSeriesInstancesResponse?.instances)?? new TimeSeriesInstance[0];
        }

        private async Task<TResult> MakeTsiDataApiCall<TResult>(
            string method,
            string path,
            Func<TextReader, TResult> consumeTextReader)
        {
            return await HttpUtils.MakeHttpCallAsync(
                "api.timeseries.azure.com",
                method,
                path,
                _accessToken,
                "TsiDataClient",
                consumeTextReader,
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
            public TimeSeriesType[] put;
        }

        private sealed class GetTimeSeriesInstancesResponse
        {
            public TimeSeriesInstance[] instances;
            public string continuationToken;
        }

        private sealed class PutTimeSeriesInstancesRequest
        {
            public TimeSeriesInstance[] put;
        }
        #pragma warning restore 649
    }
}