using System.Reflection;

namespace HylianGrimoire;

internal static class AppMetadata
{
    public static string DisplayName =>
        typeof(AppMetadata).Assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
        ?? "Hylian Grimoire";

    public static string DisplayVersion
    {
        get
        {
            string? version = typeof(AppMetadata).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            if (!string.IsNullOrWhiteSpace(version))
            {
                int metadataIndex = version.IndexOf('+', StringComparison.Ordinal);
                return metadataIndex >= 0 ? version[..metadataIndex] : version;
            }

            Version? assemblyVersion = typeof(AppMetadata).Assembly.GetName().Version;
            return assemblyVersion is null
                ? "1.0"
                : $"{assemblyVersion.Major}.{assemblyVersion.Minor}";
        }
    }

    public static string MainWindowTitle => $"{DisplayName} {DisplayVersion}";
}
