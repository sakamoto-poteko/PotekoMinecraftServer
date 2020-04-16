namespace MinecraftServerDaemon.Settings
{
    public class GrpcServerInfo
    {
        public string Host { get; set; }

        public int Port { get; set; }

        public string Certificate { get; set; }

        public string Key { get; set; }

        public string RootClientCertificate { get; set; }

        public bool Enforce { get; set; }
    }
}