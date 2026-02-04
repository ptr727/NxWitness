using System.Runtime.CompilerServices;

namespace CreateMatrix;

internal static class LogExtensions
{
    extension(ILogger logger)
    {
        internal bool LogAndPropagate(
            Exception exception,
            [CallerMemberName] string function = "unknown"
        )
        {
            logger.Error(exception, "{Function}", function);
            return false;
        }

        internal bool LogAndHandle(
            Exception exception,
            [CallerMemberName] string function = "unknown"
        )
        {
            logger.Error(exception, "{Function}", function);
            return true;
        }
    }
}

internal static class StringBuilderExtensions
{
    private const string CrLf = "\r\n";

    extension(StringBuilder sb)
    {
        internal StringBuilder AppendLineCrlf(string? value = null)
        {
            if (value is not null)
            {
                _ = sb.Append(value);
            }

            return sb.Append(CrLf);
        }
    }
}
