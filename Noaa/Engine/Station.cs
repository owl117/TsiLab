namespace Engine
{
    public sealed class Station
    {
        public Station(string id, string shortId, string name, string timeZone)
        {
            Id = id;
            ShortId = shortId;
            Name = name;
            TimeZone = timeZone;
        }

        public string Id { get; private set; }
        public string ShortId { get; private set; }
        public string Name { get; private set; }
        public string TimeZone { get; private set; }
    }
}