using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// -----------------------------------------------------------------------------
// 2012-04-28 The following code was copied verbatum from the Open Knowledge -
// KIFv3r0 Framework Project.

namespace ModelConsole.Model.Diagnostics
{

   /// <summary>
   /// Define the Log class used to manage a log that will help to write to a
   /// system log or to a file based on the application configuration section
   /// defined as "LogsConfig".
   /// </summary>
   /// <author>Eduardo Sobrino</author>
   /// <date>Oct/2k3</date>
   public class Log
   {

      /// <summary>
      /// Given an expection error messages by building a full message going
      /// through all inner exceptions...
      /// </summary>
      /// <param name="exception">original exection</param>
      /// <returns>A string message is returned.</returns>
      static public String GetErrorMessages(Exception exception)
      {
         if (exception == null)
            return String.Empty;
         StringBuilder sb = new StringBuilder();
         sb.Append(exception.Message);
         while (exception.InnerException != null)
         {
            exception = exception.InnerException;
            sb.AppendLine(exception.Message);
         }
         return sb.ToString();
      }  // end of GetErrorMessages

      private String m_LogPathName = String.Empty;

      public String LogPathName
      {
         get { return m_LogPathName; }
         set
         {
            if (value == null)
               m_LogPathName = String.Empty;
            else
               m_LogPathName = value;
         }
      }

      public SeverityLevel DefaultSeverityLevel = SeverityLevel.Unknown;
      public String DefaultLocation = String.Empty;

      public bool WriteToFile(String location, Exception exception)
      {
         return Log.WriteToFile(m_LogPathName, location, exception);
      }  // end of WriteToFile

      public bool WriteToFile(Exception exception)
      {
         return Log.WriteToFile(m_LogPathName, String.Empty, exception);
      }  // end of WriteToFile

      public bool WriteToFile(SeverityLevel severity,
         String location, String message)
      {
         return Log.WriteToFile(m_LogPathName, severity,
            location, message);
      }  // end of WriteToFile

      public bool WriteToFile(String location, String message)
      {
         return Log.WriteToFile(m_LogPathName, DefaultSeverityLevel,
            location, message);
      }  // end of WriteToFile

      public bool WriteToFile(String message)
      {
         return Log.WriteToFile(m_LogPathName, DefaultSeverityLevel,
            DefaultLocation, message);
      }  // end of WriteToFile

      //public bool WriteToFile(ResultLog resultLog)
      //{
      //   if (resultLog == null)
      //      return false;

      //   Int32 i = 0;
      //   for (i = 0; i < resultLog.Count; i++)
      //   {
      //      Log.WriteToFile(m_LogPathName, resultLog[i].Severity,
      //         resultLog[i].Source, resultLog[i].Message);
      //   }

      //   return i > 0;
      //}

      public Log(String logPathName)
      {
         LogPathName = logPathName;
      }  // end of Log (ctor)

      public Log()
      {
         LogPathName = String.Empty;
      }  // end of Log (ctor)

      /// <summary>
      /// Write/Log message to given file.
      /// </summary>
      /// <param name="FileFullPathName">Full file path name to log message
      /// </param>
      /// <param name="Level">SeverityLevel of message</param>
      /// <param name="Location">Place where error occured (use this to put the
      /// function/method where error - issue happends</param>
      /// <param name="Message">String message to log</param>
      /// <returns>true is returned if message was logged</returns>
      static public bool WriteToFile(
         String fileFullPathName,
         SeverityLevel level,
         String location,
         String message)
      {
         if (String.IsNullOrEmpty(message) ||
             String.IsNullOrEmpty(fileFullPathName))
            return false;

         LogSettings log = new LogSettings();

         if (String.IsNullOrEmpty(location))
            location = String.Empty;

         // setup default log

         log.SendToSystemLog = false;
         log.SendToApplicationLog = true;
         log.ApplicationLogPath = fileFullPathName;
         log.Status = LogStatus.Enabled;
         log.SystemLogName = String.Empty;
         log.SystemSourceName = String.Empty;
         log.Verbosity = Verbosity.ErrorsOnly;

         return (Write(log, level, location, message));
      }  // end of Write to log

      /// <summary>
      /// Write/Log message to given file.
      /// </summary>
      /// <param name="fileFullPathName">Full file path name to log message
      /// </param>
      /// <param name="location">Place where error occured (use this to put the
      /// function/method where error - issue happends</param>
      /// <param name="exception">Exception to log</param>
      /// <returns>true is returned if message was logged</returns>
      static public bool WriteToFile(
         String fileFullPathName,
         String location,
         Exception exception)
      {
         LogSettings log = new LogSettings();
         SeverityLevel level = SeverityLevel.Fatal;

         // setup default log

         log.SendToSystemLog = false;
         log.SendToApplicationLog = true;
         log.ApplicationLogPath = fileFullPathName;
         log.Status = LogStatus.Enabled;
         log.SystemLogName = String.Empty;
         log.SystemSourceName = String.Empty;
         log.Verbosity = Verbosity.ErrorsOnly;

         Int32 i = 0;
         StringBuilder sb = new StringBuilder();
         Exception ex = exception;
         while (true)
         {
            if (ex == null)
               break;
            sb.Append("Exception[" + i.ToString() + "]: " + ex.Message);
            if (ex.InnerException == null)
               break;
            ex = ex.InnerException;
            i++;
         }

         return (Write(log, level, location, sb.ToString()));
      }  // end of WriteToFile

#if EVENT_LOG_SUPPORT

      /// <summary>
      /// Log given message with given severity level based on how the 
      /// specification for the log for the particular severity level.
      /// </summary>
      /// <param name="Log">severity log</param>
      /// <param name="Level">SeverityLevel (Fatal, Critical, Warning,
      /// Info, Debug)</param>
      /// <param name="Location">The location of the error.</param>
      /// <param name="Message">Message to log.</param>
      /// <returns>true is returned if message was logged on the system log
      /// or application log.</returns>
      static public bool Write(
         LogSettings   Log,
         SeverityLevel Level,
         String Location,
         String Message)
      {

         // make sure that severity level is supported

         bool done = false ;
         if (Level == SeverityLevel.Unknown)
            return(done) ;

         // see if this log is active, if not just return without doing
         // anything ...

         if (Log.Status != LogStatus.Enabled)
            return(done) ;

         // log into a system log if needed (as configured)

         if (Log.SendToSystemLog)
         {
            // Create the source, if it does not already exist.
            Boolean found = false;
            String errMess = null;
            try
            {
               found = EventLog.SourceExists(
                  Log.SystemSourceName);
            }
            catch(Exception ex)
            {
               errMess = ex.Message;
            }

            if (!found)
            {
               try
               {
                  EventLog.CreateEventSource(
                     Log.SystemSourceName,
                     Log.SystemLogName);
                  found = true;
               }
               catch(Exception ex)
               {
                  errMess = ex.Message;
               }
            }

            if (!found)
               goto _ContinueWithApplicationLog;
  
            // Create an EventLog instance and assign its source.

            EventLog sysLog ;
            sysLog = new System.Diagnostics.EventLog() ;
            sysLog.Source = Log.SystemSourceName;

            // map severity to log entry type ...

            EventLogEntryType stype ;
            switch (Log.Severity) {
               case SeverityLevel.Fatal:
                  stype = System.Diagnostics.EventLogEntryType.Error ;
                  break ;
               case SeverityLevel.Critical:
                  stype = System.Diagnostics.EventLogEntryType.Error ;
                  break ;
               case SeverityLevel.Warning:
                  stype = System.Diagnostics.EventLogEntryType.Warning ;
                  break ;
               case SeverityLevel.Info:
                  stype = System.Diagnostics.EventLogEntryType.Information ;
                  break ;
               case SeverityLevel.Debug:
                  stype = System.Diagnostics.EventLogEntryType.Information ;
                  break ;
               default:
                  stype = System.Diagnostics.EventLogEntryType.Information ;
                  break ;
            }

            // Write an informational entry to the event log.   

            string outMess = "(" + Log.Severity.ToString() + ") ";
            if (Location != null)
               if (Location.Trim().Length > 0)
                  outMess += " "+Location+": " ;
            outMess += Message ;
            sysLog.WriteEntry(outMess,stype);
            sysLog.Dispose() ;

            done = true ;
         }

         // log into the application log if needed (as configured)
         _ContinueWithApplicationLog:
         if (Log.SendToApplicationLog)
         {  StreamWriter sw ;

            // let's check the AppLogPath and see if one was given
            String logPath = Log.ApplicationLogPath;

            // if no path was given try to use the defaualt diagnostics
            // directory path if any is available...
            if (String.IsNullOrEmpty(logPath))
            {
               String ddir = Edam.Application.
                  Defaults.DefaultDiagnosticsDirectoryPath;
               if (!String.IsNullOrEmpty(ddir))
                  logPath = ddir;
            }

            // if log path is still null or empty assign the current directory
            // as default
            if (String.IsNullOrEmpty(logPath))
            {
               String cdir = Directory.GetCurrentDirectory();
               logPath = cdir + "/";
            }

            // prepare the file name to be used for the log
            string fname = Level.ToString() + "_"
            +  DateTime.Today.Year.ToString("d4")
            +  DateTime.Today.Month.ToString("d2")
            +  DateTime.Today.Day.ToString("d2")
            +  ".xml" ;
            string fpath = logPath + fname;

            // create file if needed, else open to append
            if (!File.Exists(fpath))
               sw = File.CreateText(fpath);
            else
               sw = File.AppendText(fpath);

            // investigate if we do have permission to write to disk
            //Edam.Security.ResourceAccessRights.AccessRights accr =
            //   new Edam.Security.ResourceAccessRights.AccessRights(fpath);
            //if (!(accr.CanWrite && accr.CanModify))
            //   return(false);

            if (sw != null)
            {
               sw.WriteLine(ToXmlMessage(Location, Level, Message));
               sw.Flush() ;
               sw.Close() ;

               done = true ;
            }
         }

         return(done) ;
      }  // end of Write to log based on severity level
#else

      static public bool Write(
         LogSettings Log,
         SeverityLevel Level,
         String Location,
         String Message)
      {
         return false;
      }

#endif

      /// <summary>
      /// Write a message to the message log.
      /// </summary>
      /// <param name="Log">Instance of Log where to write message</param>
      /// <param name="Level">Severity Level</param>
      /// <param name="Message">Message to log.</param>
      /// <returns></returns>
      static public bool Write(
      LogSettings Log, SeverityLevel Level, String Message)
      {
         return (Write(Log, Level, null, Message));
      }  // end of Write a log entry (no location)

      /// <summary>
      /// Allow to call Write(SeverityLevel ...) but specifying the severity
      /// as a string.
      /// </summary>
      /// <param name="Level">String representation of a severity level
      /// ("Fatal", "Critical", "Warning", "Info", "Debug")</param>
      /// <param name="Location">The location of the error.</param>
      /// <param name="Message"></param>
      /// <returns>true is returned if logged</returns>
      static public bool Write(
         LogSettings Log, string Level, string Location, String Message)
      {
         SeverityLevel level = ToSeverityLevel(Level);
         return (Write(Log, level, Message));
      }  // end of Write to log based on severity level

      /// <summary>
      /// Allow to call Write(SeverityLevel ...) but providing a
      /// system exception to logg info of.
      /// </summary>
      /// <param name="Log">Log in where to send the message.</param>
      /// <param name="Location">The location of the error.</param>
      /// <param name="Exception">Exception to be logged</param>
      /// <returns>true is returned if logged</returns>
      static public bool Write(LogSettings log, string location,
         Exception exception)
      {
         if (exception == null)
            return (false);

         return (Write(log, SeverityLevel.Fatal, location, exception.Message));
      }  // end of Write to log an exception info

      /// <summary>
      /// Allow to call Write(SeverityLevel ...) but providing a
      /// system exception to logg info of.
      /// </summary>
      /// <param name="location">The location of the error.</param>
      /// <param name="exception">Exception to be logged</param>
      /// <returns>true is returned if logged</returns>
      static public bool Write(string location, Exception exception)
      {
         LogSettings log = new LogSettings();

         log.SendToApplicationLog = true;
         log.SendToSystemLog = false;
         log.ApplicationLogPath = String.Empty;
         log.Severity = SeverityLevel.Fatal;
         log.Status = LogStatus.Enabled;
         log.SystemLogName = String.Empty;
         log.SystemSourceName = String.Empty;
         log.Verbosity = Verbosity.ErrorsOnly;

         return (Write(log, location, exception));
      }  // end of Write to log an exception info

      /// <summary>
      /// Allow to call Write(SeverityLevel ...) but providing a
      /// system exception to logg info of.
      /// </summary>
      /// <param name="severity">severity</param>
      /// <param name="message">message to log</param>
      /// <returns>true is returned if logged</returns>
      static public bool Write(SeverityLevel severity, string message)
      {
         LogSettings log = new LogSettings();

         log.SendToApplicationLog = true;
         log.SendToSystemLog = false;
         log.ApplicationLogPath = String.Empty;
         log.Severity = SeverityLevel.Fatal;
         log.Status = LogStatus.Enabled;
         log.SystemLogName = String.Empty;
         log.SystemSourceName = String.Empty;
         log.Verbosity = Verbosity.ErrorsOnly;

         return (Write(log, severity, String.Empty, message));
      }  // end of Write to log an exception info

      /// <summary>
      /// Allow to call Write(SeverityLevel ...) but providing a
      /// system exception to logg info of.
      /// </summary>
      /// <param name="message">message to log</param>
      /// <returns>true is returned if logged</returns>
      static public bool Write(string message)
      {
         return (Write(SeverityLevel.Unknown, message));
      }  // end of Write to log an exception info

      /// <summary>
      /// Allow to call Write(SeverityLevel ...) but providing a
      /// system exception to logg info of.
      /// </summary>
      /// <param name="Exception">Exception to be logged</param>
      /// <param name="Exception"></param>
      /// <returns>true is returned if logged</returns>
      static public bool Write(Exception exception)
      {
         return (Write(String.Empty, exception));
      }  // end of Write to log an exception info

      /// <summary>
      /// Convert a string to a SeverityLevel enumerator.
      /// </summary>
      /// <param name="LevelStr">String representation of severity level.
      /// </param>
      /// <returns>The SeverityLevel enumerator is returned else
      /// "SeverityLevel.Unknown" is returned.</returns>
      static public SeverityLevel ToSeverityLevel(string LevelStr)
      {
         SeverityLevel level;
         if (LevelStr.ToLower().CompareTo("fatal") == 0)
            level = SeverityLevel.Fatal;
         else
         if (LevelStr.ToLower().CompareTo("critical") == 0)
            level = SeverityLevel.Critical;
         else
         if (LevelStr.ToLower().CompareTo("warning") == 0)
            level = SeverityLevel.Warning;
         else
         if (LevelStr.ToLower().CompareTo("info") == 0)
            level = SeverityLevel.Info;
         else
         if (LevelStr.ToLower().CompareTo("debug") == 0)
            level = SeverityLevel.Debug;
         else
            level = SeverityLevel.Unknown;
         return (level);
      }  // end of convert given string to a SeverityLevel

      /// <summary>
      /// Given a caller location, severity level and a message get an XML
      /// representation of the messag.
      /// </summary>
      /// <param name="location">caller location (if any)</param>
      /// <param name="level">seveirty leve</param>
      /// <param name="message">message to log</param>
      /// <param name="dateTime">log date time</param>
      /// <returns>an XML message is returned...</returns>
      static public String ToXmlMessage(String location,
         SeverityLevel level,
         String message, DateTime dateTime)
      {
         if (String.IsNullOrEmpty(message))
            message = String.Empty;

         string outMess =
            "<Entry>"
         + "<Date>" + dateTime.ToString("yyyyMMdd HH:mm") + "</Date>"
         + "<Message>" + message.Trim() + "</Message>"
         + "<Severity>" + level.ToString() + "</Severity>";

         if (!String.IsNullOrEmpty(location))
            outMess += "<Location>" + location + "</Location>";

         outMess += "</Entry>";

         return outMess;
      }

      /// <summary>
      /// Given a caller location, severity level and a message get an XML
      /// representation of the messag.
      /// </summary>
      /// <param name="location">caller location (if any)</param>
      /// <param name="level">seveirty leve</param>
      /// <param name="message">message to log</param>
      /// <returns>an XML message is returned...</returns>
      static public String ToXmlMessage(String location,
         SeverityLevel level, String message)
      {
         DateTime dt = DateTime.Now;
         return ToXmlMessage(location, level, message, dt);
      }  // end of ToXmlMessage

   }

}
