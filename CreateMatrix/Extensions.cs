using Serilog;

namespace CreateMatrix;

public static class Extensions
{
    public static bool LogAndHandle(this ILogger logger, Exception exception, string? function)
    {
        logger.Error(exception, "{Function}", function);
        return true;
    }
}