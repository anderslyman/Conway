using Microsoft.Extensions.Hosting;

namespace Conway.Library.Settings
{
    public static class GlobalHosting
    {
        public static IHostEnvironment? HostingEnvironment { get; set; }

        public static string? ConnectionString { get; set; }

        public static StaticAppSettings? AppSettings { get; set; }
    }
}
