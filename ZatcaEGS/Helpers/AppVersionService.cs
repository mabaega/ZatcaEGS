namespace ZatcaEGS.Helpers
{
    public class AppVersionService
    {
        public string Version { get; }
        public AppVersionService(string version)
        {
            Version = "v" + version;
        }

    }
}
