using System.Reflection;
using System.Runtime.InteropServices;

namespace CreateMatrix;

internal static class AssemblyInfo
{
    internal static string AppVersion => $"{AppName} : {FileVersion} ({BuildType})";

    internal static string RuntimeVersion =>
        $"{RuntimeInformation.FrameworkDescription} : {RuntimeInformation.RuntimeIdentifier}";

    internal static string BuildType =>
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    internal static string AppName => GetAssembly().GetName().Name ?? string.Empty;

    internal static string InformationalVersion =>
        // E.g. 1.2.3+abc123.abc123
        GetAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? string.Empty;

    internal static string FileVersion =>
        // E.g. 1.2.3.4
        GetAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
        ?? string.Empty;

    internal static string ReleaseVersion =>
        // E.g. 1.2.3 part of 1.2.3+abc123.abc123
        // Use major.minor.build from informational version
        InformationalVersion.Split('+', '-')[0];

    private static Assembly GetAssembly()
    {
        Assembly? assembly = Assembly.GetEntryAssembly();
        assembly ??= Assembly.GetExecutingAssembly();
        return assembly;
    }
}
