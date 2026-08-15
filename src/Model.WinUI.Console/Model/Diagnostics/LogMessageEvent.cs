using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// -----------------------------------------------------------------------------

namespace ModelConsole.Model.Diagnostics
{

   /// <summary>
   /// Provide for an application to support the handling of event log messages
   /// handling...
   /// </summary>
   public class LogMessageEventArgs : EventArgs
   {
      private ResultLog m_ParentLog;
      public ResultLog ParentLog
      {
         get { return m_ParentLog; }
      }
      public IMessageLogEntry Message;
      public Verbosity Verbosity;

      /// <summary>
      /// True to cancel further login of message since it will be fully
      /// handled or has been already handled by event subscriber...
      /// </summary>
      public Boolean Cancel { get; set; }

      public LogMessageEventArgs(ResultLog parentLog)
      {
         m_ParentLog = parentLog;
         Cancel = false;
         Verbosity = Verbosity.Debugging;
      }

   }  // end of LogMessageEventArgs

   /// <summary>
   /// Log Message Event Delegate to provide an opportunity to intervine.
   /// </summary>
   /// <param name="sender">instance of sender</param>
   /// <param name="e">expected instance of LogMessageEventArgs</param>
   public delegate void LogMessageEvent(Object sender, LogMessageEventArgs e);

   /// <summary>
   /// Request to output a message...
   /// </summary>
   /// <param name="location">from where the message to output is being
   /// submitted</param>
   /// <param name="message">message to log</param>
   public delegate void OutputMessageEvent(MessageLogEntry message);

}
