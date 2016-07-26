using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using System;
using System.Diagnostics;
using System.IO;
using System.Web.Configuration;

namespace SAPWebApi.Log
{
    public class SAPLogger
    {
        #region Consts

        /// <summary>
        /// The name of the folder logging information is written to.
        /// </summary>
        private const string cLogFolderName = "Log";

        /// <summary>
        /// The extension of log files (with dot prefix).
        /// </summary>
        private const string cLogFileExtension = ".log";

        /// <summary>
        /// The layout pattern which is used when logging into a file.
        /// </summary>
        private const string cLogFileLayoutPattern = "%-5level %date{dd-MM-yyyy HH:mm:ss,fff} - %message%newline";

        /// <summary>
        /// The maximal size of a log file.
        /// </summary>
        private const string cMaxLogFileSize = "100KB";

        /// <summary>
        /// The maximum number of backup files that are kept before the oldest is erased.
        /// </summary>
        private const int cMaxSizeRollBackups = 2;

        #endregion

        #region Private Fields

        /// <summary>
        /// Lock for using logging system
        /// 
        /// It must be static to syncronize access
        /// from different web instances
        /// </summary>
        private static object m_LockLogObject = new object();

        #endregion

        #region Logging methods

        /// <summary>
        /// Path to log file from configuration
        /// </summary>
        private static string m_LogFilePath = WebConfigurationManager.AppSettings["LogFilePath"];

        /// <summary>
        /// Flag for enable write log to file.
        /// </summary>
        private static bool m_EnableWriteLogToFile = bool.Parse(WebConfigurationManager.AppSettings["EnableWriteLogToFile"]);

        /// <summary>
        /// Returns path to log file for this web service
        /// </summary>
        private string GetPathToLogFile()
        {
            return m_LogFilePath;
        }

        /// <summary>
        /// Returns special lock object for
        /// access to logger.
        /// 
        /// By default used static object.
        /// </summary>
        private object LockLogObject
        {
            get { return m_LockLogObject; }
        }

        /// <summary>
        /// Logger writes log into the log-file
        /// </summary>
        /// <param name="text">String to write into the log</param>
        public string WriteLoggerLogInfo(string text)
        {
            string lvResult = text;

            lock (LockLogObject)
            {
                WriteLog(text, EventLogEntryType.Information);
            }

            return lvResult;
        }

        /// <summary>
        /// Logger writes log into the log-file
        /// </summary>
        /// <param name="text">String to write into the log</param>
        public string WriteLoggerLogError(string text)
        {
            string lvResult = text;

            lock (LockLogObject)
            {
                WriteLog(text, EventLogEntryType.Error);
            }

            return lvResult;
        }

        /// <summary>
        /// Logger writes log into the log-file
        /// </summary>
        /// <param name="text">String to write into the log</param>
        /// <param name="exception">Exception to log</param>
        public string WriteLoggerLogError(string text, Exception exception)
        {
            string lvResult;

            lock (LockLogObject)
            {
                WriteLog(text + ": " + exception.Message, EventLogEntryType.Error, exception);
                lvResult = StackTraceException(exception);
            }

            return lvResult;
        }

        /// <summary>	Writes a log. </summary>
        ///
        /// <param name="text">					String to write into the log. </param>
        /// <param name="eventLogEntryType">	Type of the event log entry. </param>
        private void WriteLog(string text, EventLogEntryType eventLogEntryType, Exception exception = null)
        {
            if (m_EnableWriteLogToFile)
            {
                if (eventLogEntryType == EventLogEntryType.Information)
                    Logger.Info(text, exception);
                else if (eventLogEntryType == EventLogEntryType.Warning)
                    Logger.Warn(text, exception);
                else if (eventLogEntryType == EventLogEntryType.Error)
                    Logger.Error(text, exception);
            }
        }

        /// <summary>
        /// Get path to log file on file system and create all 
        /// directories in this path which not yet exists.
        /// </summary>
        private string GetLogFile()
        {
            // get the path to log file from config
            string lvFilePath = GetPathToLogFile();

            if (lvFilePath.Contains(Path.DirectorySeparatorChar.ToString()))
            {
                string lvDir = lvFilePath.Substring(0, lvFilePath.LastIndexOf(Path.DirectorySeparatorChar));
                if (!Directory.Exists(lvDir))
                {
                    // try to create dir
                    try
                    {
                        Directory.CreateDirectory(lvDir);
                    }
                    catch
                    {
                        throw new Exception(string.Format("Unable to create directory '{0}'", lvDir));
                    }
                }
            }

            return lvFilePath;
        }

        /// <summary>
        /// String representation of given exception stack trace
        /// (with all inner exceptions)
        /// </summary>
        public string StackTraceException(Exception exception)
        {
            string lvStackTrace = "";

            if (exception != null)
            {
                lvStackTrace += "\n" + exception.Message + "\n" + exception.StackTrace;

                if (exception.InnerException != null)
                {
                    lvStackTrace += StackTraceException(exception.InnerException);
                }
            }

            return lvStackTrace;
        }

        #endregion

        #region Properties

        #region Logger

        /// <summary>
        /// The value of the Logger property.
        /// </summary>
        private static ILog m_Logger = LogManager.GetLogger(typeof(SAPLogger));

        /// <summary>
        /// Gets the object which is used to write logs.
        /// </summary>
        private static ILog Logger
        {
            get
            {
                return m_Logger;
            }
        }

        #endregion

        #endregion

        #region Singleton

        /// <summary>	The instance. </summary>
        private static SAPLogger instance;

        /// <summary>	Gets the instance. </summary>
        ///
        /// <value>	The instance. </value>

        public static SAPLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SAPLogger();
                }
                return instance;
            }
        }

        #region Constructors

        /// <summary>	Static constructor. </summary>
        static SAPLogger()
        {
            ConfigureFileLogger(m_LogFilePath);
        }

        /// <summary>	Default constructor. </summary>
        public SAPLogger()
        {
        }

        /// <summary>
        /// Configures the default file logger.
        /// </summary>
        public static void ConfigureFileLogger(string logFilePath)
        {
            #region Check arguments

            if (string.IsNullOrEmpty(logFilePath))
                throw new ArgumentNullException("logFilePath");

            #endregion

            RollingFileAppender lvFileAppender = new RollingFileAppender();

            lvFileAppender.File = logFilePath;

            lvFileAppender.Layout = new PatternLayout(cLogFileLayoutPattern);
            lvFileAppender.AppendToFile = true;
            lvFileAppender.MaximumFileSize = cMaxLogFileSize;
            lvFileAppender.MaxSizeRollBackups = cMaxSizeRollBackups;

            lvFileAppender.Threshold = Level.Debug;

            lvFileAppender.ActivateOptions();

            BasicConfigurator.Configure(lvFileAppender);
        }

        #endregion

        #endregion
    }
}