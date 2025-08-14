using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ScreenNavigation.Tests")]

#if !USE_ZLOGGER

namespace Microsoft.Extensions.Logging
{
    using UnityEngine;

    internal interface ILogger
    {
    }

    internal interface ILogger<out TCategoryName> : ILogger
    {
    }

    internal static class LoggerExtensions
    {
        public static void LogDebug<T>(this ILogger<T> logger, string? message, params object?[] args)
        {
            if (logger is UnityLogger<T> unity)
            {
                unity.Log(message, args);
            }
        }
    }

    internal class UnityLogger<TCategoryName> : ILogger<TCategoryName>
    {
        public void Log(string message, params object[] args)
        {
            Debug.Log($"{typeof(TCategoryName).Name} | {string.Format(message, args)}");
        }
    }
}

#endif