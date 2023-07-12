namespace ContosoTeamStats
{
    public class RedisSettings
    {
        public HostDetail[] Hosts { get; set; } = System.Array.Empty<HostDetail>();
        public string Password { get; set; }
        public bool AbortOnConnectFail { get; set; }
        public int SyncTimeout { get; set; }
        public bool Enabled { get; set; }

        public record HostDetail(string Host, int Port);
    }
}
