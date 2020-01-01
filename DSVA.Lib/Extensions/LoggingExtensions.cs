using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DSVA.Lib.Extensions
{
    public static class LoggingExtensions
    {
        private static string Clock(IDictionary<int, long> clock) =>
            string.Join(',', clock.OrderBy(x => x.Key).Select(x => x.Value.ToString()));

        public static void LogException(this ILogger log, IDictionary<int, long> clock, int id, Exception ex, string message) =>
            log.LogError(ex, $"{id}: <{Clock(clock)}> - {message}");

        public static void LogMessage(this ILogger log, IDictionary<int, long> clock, int id, string message) =>
            log.LogInformation($"{id}: <{Clock(clock)}> - {message}");

        public static void LogMessage(this ILogger log, IDictionary<int, long> clock, int id, object message) =>
            log.LogInformation("{id}: <{Clock}> - {@message}", id, Clock(clock), message);

        public static void LogWarn(this ILogger log, IDictionary<int, long> clock, int id, string message) =>
            log.LogWarning($"{id}: <{Clock(clock)}> - {message}");
    }
}
