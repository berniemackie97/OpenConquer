
namespace OpenConquer.Infrastructure.POCO
{
    // This should/could be moved to the Domain project
    public class NetworkSettings
    {
        public int LoginPort { get; set; }
        public int GamePort { get; set; }
        public string ExternalIp { get; set; } = "";
    }
}
