using ModelConsole.Model.Diagnostics;

namespace ModelConsole.Services
{
   /// <summary>
   /// Log service backed by the process-wide <see cref="ResultLog.DefaultLog"/>.
   /// Must be registered as a singleton: the exposed event bridges to the
   /// static <see cref="ResultLog.LogMessageHandler"/> delegate, so more than
   /// one instance would double-wire the log.
   /// </summary>
   public class LogService : ILogService
   {
      private readonly ResultLog m_Log;

      public LogService()
      {
         m_Log = ResultLog.DefaultLog;
      }

      /// <summary>
      /// Write a full log entry.
      /// </summary>
      /// <param name="entry">entry to write</param>
      public void Write(IMessageLogEntry entry)
      {
         m_Log.Write(entry);
      }

      /// <summary>
      /// Write an informational message.
      /// </summary>
      /// <param name="message">message to write</param>
      public void WriteMessage(string message)
      {
         m_Log.Write(MessageLogEntry.GetEntry(message, SeverityLevel.Info));
      }

      /// <summary>
      /// Instance event that bridges to the static ResultLog handler so that
      /// subscribers written against the service keep receiving every message
      /// written to the process log.
      /// </summary>
      public event LogMessageEvent LogMessageHandler
      {
         add { ResultLog.LogMessageHandler += value; }
         remove { ResultLog.LogMessageHandler -= value; }
      }
   }
}
