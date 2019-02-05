namespace Engine
{
    public sealed class Station
    {
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