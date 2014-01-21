using System;
using System.Diagnostics;
using log4net;
using System.Collections.Generic;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace CarMediaServer
{
    /// <summary>
    /// The logger class is a central repository for logging from all components.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Dfines a dictionary tying loggers to the declaring class so that each class gets its own logger.
        /// </summary>
        private static Dictionary<Type, ILog> _loggers = new Dictionary<Type, ILog>();

        /// <summary>
        /// Gets an instance of ILog based upon the calling method's type.  It is assumed that the calling method
        /// will always be the second frame of the stack trace.
        /// </summary>
        /// <returns>
        /// An instance of ILog based upon the calling method.
        /// </returns>
        private static ILog GetLogger ()
		{
			Type t = new StackTrace ().GetFrame (2).GetMethod ().DeclaringType;
			if (!_loggers.ContainsKey (t))
				try {
					_loggers.Add (t, LogManager.GetLogger (t));
				} catch {
				} // We do this instead of lock for speed on subsequent calls
            return _loggers[t];
        }

        /// <summary>
        /// Write an informational message to the log.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        public static void Info(string message)
        {
            ILog log = GetLogger();
            if (!log.IsInfoEnabled)
                return;
            log.Info(message);
        }

        /// <summary>
        /// Write an informational message to the log.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        /// <param name="exception">
        /// The exception to log.
        /// </param>
        public static void Info(string message, Exception exception)
        {
            ILog log = GetLogger();
            if (!log.IsInfoEnabled)
                return;
            log.Info(message, exception);
        }

        /// <summary>
        /// Write a warning message to the log.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        public static void Warn(string message)
        {
            ILog log = GetLogger();
            if (!log.IsWarnEnabled)
                return;
            log.Warn(message);
        }

        /// <summary>
        /// Write a warning message to the log.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        /// <param name="exception">
        /// The exception to log.
        /// </param>
        public static void Warn(string message, Exception exception)
        {
            ILog log = GetLogger();
            if (!log.IsWarnEnabled)
                return;
            log.Warn(message, exception);
        }

        /// <summary>
        /// Write a fatal message to the log.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        public static void Fatal(string message)
        {
            ILog log = GetLogger();
            if (!log.IsFatalEnabled)
                return;
            log.Fatal(message);
        }

        /// <summary>
        /// Write a fatal message to the log.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        /// <param name="exception">
        /// The exception to log.
        /// </param>
        public static void Fatal(string message, Exception exception)
        {
            ILog log = GetLogger();
            if (!log.IsFatalEnabled)
                return;
            log.Fatal(message, exception);
        }

        /// <summary>
        /// Write an error message to the log.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        public static void Error(string message)
        {
            ILog log = GetLogger();
            if (!log.IsErrorEnabled)
                return;
            log.Error(message);
        }

        /// <summary>
        /// Write an error message to the log.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        /// <param name="exception">
        /// The exception to log.
        /// </param>
        public static void Error(string message, Exception exception)
        {
            ILog log = GetLogger();
            if (!log.IsErrorEnabled)
                return;
            log.Error(message, exception);
        }

        /// <summary>
        /// Write a debug message to the log.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        public static void Debug(string message)
        {
            ILog log = GetLogger();
            if (!log.IsDebugEnabled)
                return;
            log.Debug(message);
        }

        /// <summary>
        /// Write a debug message to the log.
        /// </summary>
        /// <param name="message">
        /// The message to log.
        /// </param>
        /// <param name="exception">
        /// The exception to log.
        /// </param>
        public static void Debug(string message, Exception exception)
        {
            ILog log = GetLogger();
            if (!log.IsDebugEnabled)
                return;
            log.Debug(message, exception);
        }
    }
}