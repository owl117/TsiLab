namespace Engine
{
    public sealed class Station
    {
        public const string InstanceFieldName_Country = "Country";
        public const string InstanceFieldName_CountrySubdivisionName = "CountrySubdivisionName";
        public const string InstanceFieldName_CountrySecondarySubdivision = "CountrySecondarySubdivision";
        public const string InstanceFieldName_Municipality = "Municipality";
        public const string InstanceFieldName_PostalCode = "PostalCode";

        public static TimeSeriesHierarchy TimeSeriesHierarchy = new TimeSeriesHierarchy(
            id: "cde07f8f-ca64-4843-85d7-97df37b0a21e",
            name: "Stations",
            source: new TimeSeriesHierarchySource(instanceFieldNames: new [] 
                {
                    InstanceFieldName_Country,
                    InstanceFieldName_CountrySubdivisionName,
                    InstanceFieldName_CountrySecondarySubdivision 
                }));

        public Station(string id, string shortId, string name, string timeZone, double? latitude, double? longitude)
        {
            Id = id;
            ShortId = shortId;
            Name = name;
            TimeZone = timeZone;
            Latitude = latitude;
            Longitude = longitude;
        }

        public string Id { get; private set; }
        public string ShortId { get; private set; }
        public string Name { get; private set; }
        public string TimeZone { get; private set; }
        public double? Latitude { get; private set; }
        public double? Longitude { get; private set; }
    }
}