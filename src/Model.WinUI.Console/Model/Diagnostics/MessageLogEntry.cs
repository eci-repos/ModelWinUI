using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// -----------------------------------------------------------------------------
// 2012-04-28 The following code was copied verbatum from the Open Knowledge -
// KIFv3r0 Framework Project.

namespace ModelConsole.Model.Diagnostics
{

   public interface IMessageLogEntry
   {
      Guid Guid { get; }
      EventCode ResultCode { get; set; }
      Object Tag { get; set; }
      SeverityLevel Severity { get; set; }
      String Source { get; set; }
      String Message { get; set; }

      DateTime LoggedDateTime { get; set; }
      String LoggedDateTimeText { get; }

      Object Exception { get; set; }
      String ErrorMessage { get; }
      String XmlMessage { get; }
      void ClearFields();
   }

   public class MessageLogInfo
   {
      public SeverityLevel Severity { get; set; }
      public System.String Source { get; set; }
      public String Message { get; set; }
      public System.DateTime CreatedDateTime { get; set; }
      public EventCode ResultCode { get; set; }
   }

   /// <summary>
   /// Message log entry supported fields record.
   /// </summary>
   public class MessageLogEntry : IMessageLogEntry
   {

      public Guid Guid { get; }
      public Object Tag { get; set; }
      public SeverityLevel Severity { get; set; }
      public String Source { get; set; }
      public String Message { get; set; }

      public DateTime LoggedDateTime { get; set; }
      public string LoggedDateTimeText
      {
         get { return LoggedDateTime.ToString("yyyy-MM-dd HH:mm:ss"); }
      }

      public Object Exception { get; set; }
      public EventCode ResultCode { get; set; }

      public String ErrorMessage
      {
         get
         {
            StringBuilder sb = new StringBuilder();
            if (!String.IsNullOrEmpty(Message))
               sb.Append(Message + " ");
            if (Exception != null)
            {
               Exception ex = (Exception)this.Exception;
               sb.Append(ex.Message);
               GetInnerErrorMessage(sb, ex);
            }
            return (sb.ToString());
         }
      }

      public String XmlMessage
      {
         get
         {
            return Log.ToXmlMessage(
               Source, Severity, ErrorMessage, LoggedDateTime);
         }
      }

      /// <summary>
      /// Initialize entry with default values.
      /// </summary>
      public MessageLogEntry()
      {
         ClearFields();
      }

      /// <summary>
      /// Get an entry with the result-code set...
      /// </summary>
      /// <param name="code">code to set</param>
      public MessageLogEntry(EventCode code)
      {
         ClearFields();
         SetEventCode(code);
      }

      public static MessageLogEntry GetEntry(string message,
         SeverityLevel severity = SeverityLevel.Info)
      {
         MessageLogEntry e = new MessageLogEntry();
         e.ResultCode = severity == SeverityLevel.Info ?
            EventCode.Success : EventCode.Failed;
         e.Message = message;
         return e;
      }

      /// <summary>
      /// Set the Event / Result code and corresponding severity.
      /// </summary>
      /// <param name="code">code to set</param>
      public void SetEventCode(EventCode code)
      {
         ResultCode = code;
         Severity = ((Int32)code < 0) ?
            SeverityLevel.Critical : SeverityLevel.Info;
      }

      /// <summary>
      /// Reset values to defaults...
      /// </summary>
      public void ClearFields()
      {
         Exception = null;
         Message = String.Empty;
         LoggedDateTime = DateTime.Now;
         Source = String.Empty;
         Severity = SeverityLevel.Info;
         ResultCode = EventCode.Unknown;
      }  // end of ClearFields

      /// <summary>
      /// Recursive function that will traverse through all inner exceptions
      /// and build string with all issues found.
      /// </summary>
      /// <param name="stringBuilder">string builder to append error messages
      /// from</param>
      /// <param name="ex">exception to extract inner exception messages
      /// </param>
      /// <returns>the inner exception message of the curretn given execption
      /// is returned</returns>
      static public String GetInnerErrorMessage(
         StringBuilder stringBuilder, Exception ex)
      {
         if (stringBuilder == null)
            return (String.Empty);
         if (ex == null)
            return (String.Empty);
         if (ex.InnerException != null)
         {
            String emess = String.Empty;
            if (!String.IsNullOrEmpty(ex.InnerException.Message))
            {
               emess = ex.InnerException.Message + " ";
               stringBuilder.Append(emess);
            }
            GetInnerErrorMessage(stringBuilder, ex.InnerException);
            return (emess);
         }
         return (string.Empty);
      }

   }

}
