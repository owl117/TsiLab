using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Engine
{
    public sealed class AzureMapsClient
    {
        private readonly string _subscriptionKey;

        public AzureMapsClient(string subscriptionKey)
        {
            _subscriptionKey = subscriptionKey;
        }

        public async Task<Address> SearchAddressReverseAsync(double latitude, double longitude)
        {
            return await HttpUtils.MakeHttpCallAsync(
                $"https://atlas.microsoft.com/search/address/reverse/json?subscription-key={_subscriptionKey}&api-version=1.0" +
                $"&query={latitude:N6},{longitude:N6}",
                ParseGetSearchAddressReverseResponse);
        }
        private static Address ParseGetSearchAddressReverseResponse(TextReader textReader)
        {
            GetSearchAddressReverseResponse getSearchAddressReverseResponse = JsonUtils.ParseJson<GetSearchAddressReverseResponse>(textReader);
            return getSearchAddressReverseResponse?.addresses?.FirstOrDefault()?.address;
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