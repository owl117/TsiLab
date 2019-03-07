using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Engine
{
    public sealed class AzureMapsClient
    {
        private const string SearchAddressReversePartitionKey = "SearchAddressReverse";
        private readonly string _subscriptionKey;
        private readonly CloudTable _cacheCloudTable;

        public AzureMapsClient(string subscriptionKey, CloudTable cacheCloudTable)
        {
            _subscriptionKey = subscriptionKey;
            _cacheCloudTable = cacheCloudTable;
        }

        public async Task<Address> SearchAddressReverseAsync(double latitude, double longitude)
        {
            string key = $"{latitude:N6},{longitude:N6}";

            Address address = await TryGetSearchAddressReverseFromCacheAsync(key);
            if (address == null)
            {
                address = await HttpUtils.MakeHttpCallAsync(
                $"https://atlas.microsoft.com/search/address/reverse/json?subscription-key={_subscriptionKey}&api-version=1.0&query=" + key,
                ParseGetSearchAddressReverseResponse);

                await CacheSearchAddressReverseAsync(key, address);
            }

            return address;
        }
        private Address ParseGetSearchAddressReverseResponse(TextReader textReader)
        {
            GetSearchAddressReverseResponse getSearchAddressReverseResponse = JsonUtils.ParseJson<GetSearchAddressReverseResponse>(textReader);
            return getSearchAddressReverseResponse?.addresses?.FirstOrDefault()?.address;
        }

        private async Task<Address> TryGetSearchAddressReverseFromCacheAsync(string key)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<CacheEntry>(SearchAddressReversePartitionKey, key);

            TableResult retrievedResult = await _cacheCloudTable.ExecuteAsync(retrieveOperation);

            if (retrievedResult.Result != null)
            {
                using (var stringReader = new StringReader(((CacheEntry)retrievedResult.Result).Value))
                {
                    return JsonUtils.ParseJson<Address>(stringReader);
                }
            }
            else
            {
                return null;
            }
        }

        private async Task CacheSearchAddressReverseAsync(string key, Address address)
        {
            if (address != null)
            {
                using (var stringWriter = new StringWriter())
                {
                    JsonUtils.WriteJson(stringWriter, address);
                    stringWriter.Flush();

                    TableOperation replaceOperation = TableOperation.InsertOrMerge(
                        new CacheEntry(SearchAddressReversePartitionKey, key, stringWriter.GetStringBuilder().ToString()));

                    await _cacheCloudTable.ExecuteAsync(replaceOperation);
                }
            }
            else
            {
                await AzureUtils.DeleteTableRowIfExistsAsync(_cacheCloudTable, SearchAddressReversePartitionKey, key);
            }
        }

        private sealed class CacheEntry : TableEntity
        {
            public CacheEntry(string partitionKey, string rowKey, string value) 
                : base(partitionKey: partitionKey, rowKey: rowKey)
            {
                Value = value;
            }

            public CacheEntry()
            {
            }

            public string Value { get; set; }
        }

        #pragma warning disable 649
        private sealed class GetSearchAddressReverseResponse
        {

            public AddressInfo[] addresses;

            public sealed class AddressInfo
            {
                public Address address;
            }
        }
        #pragma warning restore 649

        public sealed class Address
        {
            public Address(
                string buildingNumber,
                string streetNumber,
                string street,
                string streetName,
                string streetNameAndNumber,
                string countryCode,
                string countrySubdivision,
                string countrySecondarySubdivision,
                string countryTertiarySubdivision,
                string municipality,
                string postalCode,
                string extendedPostalCode,
                string municipalitySubdivision,
                string country,
                string countryCodeISO3,
                string freeformAddress,
                string countrySubdivisionName)
            {
                BuildingNumber = buildingNumber;
                StreetNumber = streetNumber;
                Street = street;
                StreetName = streetName;
                StreetNameAndNumber = streetNameAndNumber;
                CountryCode = countryCode;
                CountrySubdivision = countrySubdivision;
                CountrySecondarySubdivision = countrySecondarySubdivision;
                CountryTertiarySubdivision = countryTertiarySubdivision;
                Municipality = municipality;
                PostalCode = postalCode;
                ExtendedPostalCode = extendedPostalCode;
                MunicipalitySubdivision = municipalitySubdivision;
                Country = country;
                CountryCodeISO3 = countryCodeISO3;
                FreeformAddress = freeformAddress;
                CountrySubdivisionName = countrySubdivisionName;
            }

            [JsonProperty(PropertyName = "buildingNumber")]
            public string BuildingNumber { get; private set; }

            [JsonProperty(PropertyName = "streetNumber")]
            public string StreetNumber { get; private set; }

            [JsonProperty(PropertyName = "street")]
            public string Street { get; private set; }

            [JsonProperty(PropertyName = "streetName")]
            public string StreetName { get; private set; }

            [JsonProperty(PropertyName = "streetNameAndNumber")]
            public string StreetNameAndNumber { get; private set; }

            [JsonProperty(PropertyName = "countryCode")]
            public string CountryCode { get; private set; }

            [JsonProperty(PropertyName = "countrySubdivision")]
            public string CountrySubdivision { get; private set; }

            [JsonProperty(PropertyName = "countrySecondarySubdivision")]
            public string CountrySecondarySubdivision { get; private set; }

            [JsonProperty(PropertyName = "countryTertiarySubdivision")]
            public string CountryTertiarySubdivision { get; private set; }

            [JsonProperty(PropertyName = "municipality")]
            public string Municipality { get; private set; }

            [JsonProperty(PropertyName = "postalCode")]
            public string PostalCode { get; private set; }

            [JsonProperty(PropertyName = "extendedPostalCode")]
            public string ExtendedPostalCode { get; private set; }

            [JsonProperty(PropertyName = "municipalitySubdivision")]
            public string MunicipalitySubdivision { get; private set; }

            [JsonProperty(PropertyName = "country")]
            public string Country { get; private set; }

            [JsonProperty(PropertyName = "countryCodeISO3")]
            public string CountryCodeISO3 { get; private set; }

            [JsonProperty(PropertyName = "freeformAddress")]
            public string FreeformAddress { get; private set; }

            [JsonProperty(PropertyName = "countrySubdivisionName")]
            public string CountrySubdivisionName { get; private set; }
        }
    }
}