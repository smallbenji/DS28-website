using System.Globalization;
using DbUp.Engine.Output;

namespace DS.Website.Services
{
    public class MigrationLog(ILogger<MigrationLog> logger) : IUpgradeLog
    {
        public void LogTrace(string format, params object[] args)
        {
            Write(LogLevel.Trace, null, format, args);
        }

        public void LogDebug(string format, params object[] args)
        {
            Write(LogLevel.Debug, null, format, args);
        }

        public void LogInformation(string format, params object[] args)
        {
            Write(LogLevel.Information, null, format, args);
        }

        public void LogWarning(string format, params object[] args)
        {
            Write(LogLevel.Warning, null, format, args);
        }

        public void LogError(string format, params object[] args)
        {
            Write(LogLevel.Error, null, format, args);
        }

        public void LogError(Exception exception, string format, params object[] args)
        {
            Write(LogLevel.Error, exception, format, args);
        }

        private void Write(LogLevel level, Exception exception, string format, object[] args)
        {
            var message = args.Length == 0 ? format : string.Format(CultureInfo.InvariantCulture, format, args);
            logger.Log(level, exception, "{MigrationMessage}", message);
        }
    }
}
