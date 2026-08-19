namespace ModelConsole.Diagnostics
{
   /// <summary>
   /// Abstraction over the diagnostics log. Components depend on this
   /// interface instead of reaching for the process-wide <see cref="ResultLog"/>
   /// statics directly.
   /// </summary>
   public interface ILogService
   {
      /// <summary>
      /// Write a full log entry.
      /// </summary>
      /// <param name="entry">entry to write</param>
      void Write(IMessageLogEntry entry);

      /// <summary>
      /// Write an informational message.
      /// </summary>
      /// <param name="message">message to write</param>
      void WriteMessage(string message);

      /// <summary>
      /// Raised for every message written to the log.
      /// </summary>
      event LogMessageEvent LogMessageHandler;
   }
}
