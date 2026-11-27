using System.Reflection;

namespace DS.Domain
{
    public static class DSVersion
    {
        public const string Version = "0.3.3";
        public const string Name = "DS28 Website";

        public static string BuildVersion { get; } = typeof(DSVersion).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "DSBuildVersion")?.Value ?? Version;
    }
}
