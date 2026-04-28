using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeXtudio.Metadata
{
    /// <summary>
    /// Minimal logging service compatibility shim.
    /// </summary>
    public static class LoggerService
    {
        private static ILogger _logger = NullLogger.Instance;

        public static ILogger Logger => _logger;

        public static void SetLogger(ILogger logger)
        {
            _logger = logger ?? NullLogger.Instance;
        }
    }
}
