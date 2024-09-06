using System.Diagnostics;
using System.Reflection;

namespace ZatcaEGS.Helpers
{
    public static class VersionHelper
    {
        public static string GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return "v" + fvi.FileVersion;
        }
    }
}