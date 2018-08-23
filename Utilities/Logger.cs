using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using log4net;

namespace UI.Test.Common.Utilities
{
    /// <summary>
    /// Extends log4net logging functionality with the help of reflection to achieve fewer lines of code.
    /// </summary>
    public static class Logger
    {
        private const string ExecMsg = "[EXECUTING]:";

        /// <summary>
        /// Reuse of <see cref="ILog.Warn(object)"/>.
        /// </summary>
        /// <param name="message">Message to log.</param>
        public static void Warn(string message) => Log(LogLevel.Warn, message);

        /// <summary>
        /// Reuse of <see cref="ILog.Error(object)"/>.
        /// </summary>
        /// <param name="message">Message to log.</param>
        public static void Error(string message) => Log(LogLevel.Error, message);

        /// <summary>
        /// Reuse of <see cref="ILog.Debug(object)"/>.
        /// </summary>
        /// <param name="message">Message to log.</param>
        public static void Debug(string message) => Log(LogLevel.Debug, message);

        /// <summary>
        /// Reuse of <see cref="ILog.Info(object)"/>.
        /// </summary>
        /// <param name="message">Message to log.</param>
        public static void Info(string message) => Log(LogLevel.Info, message);

        /// <summary>
        /// Reuse of <see cref="ILog.Info(object)"/> for logging method execution.
        /// <para>Special execution message is appended by the automatically detected caller method's name with the aggregated arguments at the end.</para>
        /// </summary>
        /// <param name="args">Calling method's arguments.</param>
        public static void Exec(params object[] args) => Exec(LogLevel.Info, args);

        /// <summary>
        /// Reuse of <see cref="ILog.Info(object)"/> for logging method execution.
        /// <para>Special execution message is appended by the automatically detected caller method name with the aggregated arguments at the end.</para>
        /// </summary>
        /// <param name="logLevel">Logging level.</param>
        /// <param name="args">Calling method's arguments.</param>
        public static void Exec(LogLevel logLevel, params object[] args)
        {
            char[] separators = {',', ' '};
            var callingMethod = GetCallingMethod();
            var argLine = args.Aggregate(string.Empty, (tot, cur) =>
            {
                // If an argument is a collection, aggregate all its members.
                var objects = cur as IEnumerable<object>;
                if (objects != null)
                {
                    var subArgLine =
                        objects.Aggregate(string.Empty, (subTot, subCur) => $"{subTot}{separators[0]}{separators[1]}{subCur}")
                            .Trim(separators);
                    // Decorate with curly braces to show they're arguments.
                    cur = $"[{subArgLine}]";
                }
                return $"{tot}{cur}{separators[0]}{separators[1]}";
            }).Trim(separators);
            var methodName = callingMethod.Name;
            var message = $"{ExecMsg} {methodName}({argLine})";
            Log(logLevel, message, callingMethod.DeclaringType);
        }

        public static void Log(LogLevel logLevel, string message, Type callerType = null)
        {
            var logger = LogManager.GetLogger(callerType ?? GetCallingMethod().DeclaringType);
            switch (logLevel)
            {
                case LogLevel.Info:
                    logger.Info(message);
                    break;
                case LogLevel.Debug:
                    logger.Debug(message);
                    break;
                case LogLevel.Error:
                    logger.Error(message);
                    break;
                case LogLevel.Warn:
                    logger.Warn(message);
                    break;
            }
        }

        private static MethodBase GetCallingMethod()
        {
            MethodBase method;
            // Find the first caller outside of this class.
            var skipFrames = 2;
            do
            {
                method = new StackFrame(skipFrames++, false).GetMethod();
            } while (method.DeclaringType == typeof(Logger));
            return method;
        }

        public enum LogLevel
        {
            Info,
            Debug,
            Error,
            Warn
        }
    }
}
