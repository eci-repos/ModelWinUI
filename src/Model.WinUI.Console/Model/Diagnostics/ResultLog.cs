using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// -----------------------------------------------------------------------------

namespace ModelConsole.Model.Diagnostics
{
   /// <summary>
   /// Result Log used to get the messages on a process. Note that we don't
   /// expect to have that many messages since if all goes as planned there
   /// will not be any. Therefore we reallocate the messages vector if needed
   /// hopefully never.
   /// </summary>
   public class ResultLog : IResultsLog
   {

      #region    -- 1.11 Success Flag Property Support

      protected Boolean m_Success = true;
      public Boolean Success
      {
         get { return (m_Success); }
      }

      public Verbosity Verbosity { get; set; }

      #endregion
      #region    -- 1.12 Returned Value Property Support

      private Int32 m_ReturnValue = 0;
      public Int32 ReturnValue
      {
         get { return (m_ReturnValue); }
         set { m_ReturnValue = value; }
      }

      public string ReturnText { get; set; }

      #endregion
      #region    -- 1.13 ResultValueObject and Tag Properties Support

      public Object Tag { get; set; }
      public Object ResultValueObject { get; set; }
      public Object DataObject
      {
         get { return ResultValueObject; }
      }

      #endregion
      #region    -- 1.14 Log Messages Property Support

      public static ResultLog DefaultLog { get; } = new ResultLog();

      protected Exception m_LastException;
      protected List<IMessageLogEntry> m_Messages = null;
      public List<IMessageLogEntry> Messages
      {
         get { return m_Messages; }
      }

      public Exception LastException
      {
         get { return m_LastException; }
      }

      /// <summary>
      /// return the message in the given Index position.
      /// </summary>
      /// <param name="Index">position / index of message to retreive</param>
      /// <returns>returns the MessageLogEntry at given index</returns>
      public IMessageLogEntry this[int index]
      {
         get
         {
            if (m_Messages != null)
               if ((index >= 0) && (index < m_Messages.Count))
                  return ((MessageLogEntry)m_Messages[index]);

            IMessageLogEntry m = CreateMessageLogEntry();
            m.Severity = SeverityLevel.Unknown;
            m.Source = String.Empty;
            m.Message = String.Empty;
            m.LoggedDateTime = DateTime.Now;
            m.Exception = null;

            return (m);
         }
         set
         {
            Add(value);
         }
      }  // end of Messages

      /// <summary>
      /// return the number of messages stored if any
      /// </summary>
      public Int32 Count
      {
         get
         {
            if (m_Messages == null)
               return (0);
            return (m_Messages.Count);
         }
      }  // end of Count

      /// <summary>
      /// Get the String Text representation of the collective results
      /// logged messages...
      /// </summary>
      public String MessageText
      {
         get
         {
            if (m_Messages == null)
               return (String.Empty);

            StringBuilder sb = new StringBuilder();
            Int32 i;
            String mess;
            for (i = 0; i < Count; i++)
            {
               mess = m_Messages[i].ErrorMessage;
               if (!String.IsNullOrEmpty(mess))
                  sb.Append(mess + " ");
            }
            return (sb.ToString());
         }
      }

      /// <summary>
      /// Get the String Text representation of the collective results
      /// logged messages...
      /// </summary>
      public String Message
      {
         get
         {
            if (m_Messages == null)
               return (String.Empty);

            StringBuilder sb = new StringBuilder();
            Int32 i;
            String mess;
            for (i = 0; i < Count; i++)
            {
               mess = m_Messages[i].ErrorMessage;
               if (!String.IsNullOrEmpty(mess))
                  sb.Append(mess + " ");
            }
            return (sb.ToString());
         }
      }

      /// <summary>
      /// Create Message Log Entry.
      /// </summary>
      /// <returns>return instance of the new IMessageLogEntry</returns>
      public IMessageLogEntry CreateMessageLogEntry()
      {
         return new MessageLogEntry();
      }

      /// <summary>
      /// Prepare an XML message document with all logged entries.
      /// </summary>
      /// <param name="rootTag">root tag (default = "ResultsLog" if none is
      /// given)</param>
      /// <param name="rootAttributes">optional root attributes list
      /// (key - values) else leave it as null</param>
      /// <param name="innerXmlMessage">inner XML message</param>
      /// <returns>An XML message is returned</returns>
      public String GetXmlDocument(String rootTag,
         Dictionary<String, String> rootAttributes,
         String innerXmlMessage)
      {
         StringBuilder sb = new StringBuilder();
         String tag = String.IsNullOrEmpty(rootTag) ? "ResultsLog" : rootTag;
         String startTag = "<" + tag + ">", endTag = "</" + tag + ">";

         // if root-Attributes are given, then prepare start tag...
         if (rootAttributes != null)
         {
            if (rootAttributes.Count > 0)
            {
               startTag = "<" + tag;
               foreach (
                  KeyValuePair<string, string> kvp
                  in rootAttributes)
                  startTag += " " + kvp.Key + "=\"" + kvp.Value + "\"";
               startTag += ">";
            }
         }
         else
            startTag = "<" + tag + ">";

         sb.Append("<?xml version=\"1.0\" encoding=\"windows-1252\"?>");
         sb.Append(startTag);
         sb.Append(innerXmlMessage);
         sb.Append(endTag);

         return (sb.ToString());
      }  // end of GetXmlMessage

      /// <summary>
      /// Prepare an XML message individual entries only with no XML envelop.
      /// </summary>
      /// <returns>An XML message entries list is returned</returns>
      public String GetXmlMessageEntries()
      {
         StringBuilder sb = new StringBuilder();

         if (m_Messages != null)
         {
            Int32 i;
            String mess;
            for (i = 0; i < Count; i++)
            {
               mess = m_Messages[i].XmlMessage;
               if (!String.IsNullOrEmpty(mess))
                  sb.Append(mess + " ");
            }
         }

         return sb.ToString();
      }  // end of GetXmlMessageEntries

      /// <summary>
      /// Prepare an XML message document with all logged entries.  This method
      /// prepares all XML entries by calling the GetXmlMessageEntries method
      /// then calls the GetXmlDocument to wrap the entries with a document
      /// envelop.
      /// </summary>
      /// <param name="rootTag">root tag (default = "ResultsLog" if none is
      /// given)</param>
      /// <param name="rootAttributes">optional root attributes list
      /// (key - values) else leave it as null</param>
      /// <returns>An XML message is returned</returns>
      public String GetXmlMessageDocument(String rootTag,
         Dictionary<String, String> rootAttributes)
      {
         return GetXmlDocument(rootTag, rootAttributes, GetXmlMessageEntries());
      }  // end of GetXmlMessage

      public String GetXmlMessageDocument(
         Dictionary<String, String> rootAttributes)
      {
         return GetXmlDocument(null, rootAttributes, GetXmlMessageEntries());
      }  // end of GetXmlMessage

      /// <summary>
      /// Get the XML representation of the collective results
      /// logged messages...
      /// </summary>
      public String XmlMessage
      {
         get
         {
            return GetXmlMessageDocument(null, null);
         }
      }

      /// <summary>
      /// Log Message without Adding it to result...
      /// </summary>
      /// <param name="message">Instance of MessageLogEntry to add</param>
      /// <returns>true is returned if message was added, else it's addition
      /// was canceled</returns>
      public Boolean LogMessage(IMessageLogEntry message)
      {
         if (LogMessageHandler != null)
         {
            LogMessageEventArgs e = new LogMessageEventArgs(this);
            e.Message = message;
            e.Cancel = false;
            e.Verbosity = Verbosity;
            LogMessageHandler(this, e);
            if (e.Cancel)
               return false;
         }
         return true;
      }  // end of LogMessage

      public Boolean LogMessage(String location, String messageText)
      {
         if (String.IsNullOrEmpty(messageText))
            return false;
         if (String.IsNullOrEmpty(location))
            location = null;

         IMessageLogEntry m = CreateMessageLogEntry();
         m.ClearFields();
         m.Message = location != null ?
            location + ":" + messageText : messageText;
         return LogMessage(m);
      }  // end of LogMessage

      public Boolean LogMessage(String location, Exception exception)
      {
         if (String.IsNullOrEmpty(location))
            location = null;

         IMessageLogEntry m = CreateMessageLogEntry();
         m.ClearFields();
         m.Message = location != null ? location : String.Empty;
         m.Exception = exception;
         return LogMessage(m);
      }  // end of LogMessage

      #endregion
      #region    -- 1.14 Failed Items

      /// <summary>
      /// Set all as OK.  If there was a critical or faltal message logged
      /// success is not set to true but to false.
      /// </summary>
      public void Succeeded()
      {
         Boolean allGood = m_ReturnValue >= 0;
         foreach (Diagnostics.IMessageLogEntry e in m_Messages)
         {
            if ((e.Severity == Diagnostics.SeverityLevel.Critical ||
                 e.Severity == Diagnostics.SeverityLevel.Fatal) ||
               e.Exception != null)
            {
               allGood = false;
               break;
            }
         }
         m_Success = allGood;
      }  // end of Succeeded

      /// <summary>
      /// Allow to use an expression to set the value.  Remember that if there
      /// was an exception of critical/fatal issue before this will return false
      /// always.
      /// </summary>
      /// <param name="allIsGood">expression value (true or false)</param>
      public void Succeeded(Boolean allIsGood)
      {
         m_Success = allIsGood;
         if (!m_Success)
            return;
         Succeeded();
      }

      /// <summary>
      /// Set the success flag to false.
      /// </summary>
      /// <param name="source">source</param>
      /// <param name="exception">exception to register...</param>
      public void Failed(String source, Exception exception)
      {
         m_Success = false;
         if (exception != null)
         {
            m_LastException = exception;
            Add(source, exception.Message);
         }
      }  // end of Failed

      /// <summary>
      /// Set the success flag to false.
      /// </summary>
      /// <param name="source">source</param>
      /// <param name="exception">exception to register...</param>
      public void Failed(String source, EventCode eventCode)
      {
         m_Success = false;
         Add(source, eventCode.ToString());
      }  // end of Failed

      /// <summary>
      /// Set the success flag to false.
      /// </summary>
      /// <param name="source">source</param>
      /// <param name="message">message to be recorded</param>
      public void Failed(String source, String message)
      {
         m_Success = false;
         Add(source, message);
      }

      /// <summary>
      /// Set the success flag to false.
      /// </summary>
      /// <param name="source">source</param>
      /// <param name="exception">exception to register...</param>
      public void Failed(Exception exception)
      {
         m_Success = false;
         if (exception != null)
         {
            m_LastException = exception;
            Add(String.Empty, exception);
         }
      }  // end of Failed

      /// <summary>
      /// Set the success flag to false.
      /// </summary>
      /// <param name="source">source</param>
      /// <param name="message">message to be recorded</param>
      public void Failed(String message)
      {
         m_Success = false;
         Add(String.Empty, message);
      }

      public void Failed(Diagnostics.EventCode code)
      {
         if (code == EventCode.Success)
         {
            m_Success = true;
            return;
         }

         Add(new MessageLogEntry(code));
      }

      #endregion
      #region -- 1.14 Write to Log and Trace

      /// <summary>
      /// If a notification handler has been specify then try to 
      /// </summary>
      /// <param name="message">message to notify</param>
      public virtual void Write(IMessageLogEntry message)
      {
         if (LogMessageHandler == null)
         {
            return;
         }

         LogMessageEventArgs e = new LogMessageEventArgs(this);
         e.Message = message;
         e.Verbosity = ResultLog.DefaultVerbosity;

         LogMessageHandler(this, e);
      }

      /// <summary>
      /// Trace message 
      /// </summary>
      /// <param name="message">message to trace</param>
      /// <param name="source">source</param>
      public static void Trace(string message, string source = null)
      {
         IMessageLogEntry entry = new MessageLogEntry();
         entry.Message = message;
         entry.Source = source;
         entry.Severity = SeverityLevel.Info;

         DefaultLog.Write(entry);
      }

      #endregion
      #region    -- 1.14 Add Items

      /// <summary>
      /// Add a MessageLogEntry message.
      /// </summary>
      /// <param name="message">Instance of MessageLogEntry to add</param>
      /// <returns>true is returned if message was added, else it's addition
      /// was canceled</returns>
      public void Add(IMessageLogEntry message)
      {
         if (message == null)
            return;

         // see if there is a delegate that will take care of message...
         if (LogMessageHandler != null)
         {
            LogMessageEventArgs e = new LogMessageEventArgs(this);
            e.Message = message;
            e.Cancel = false;
            e.Verbosity = Verbosity;
            LogMessageHandler(this, e);
            if (e.Cancel)
               return;
         }

         // setup severity
         m_Success = ((message.Severity != SeverityLevel.Critical) &&
             (message.Severity != SeverityLevel.Fatal));

         // add message
         m_Messages.Add(message);
         Write(message);
      }  // end of Add message

      /// <summary>
      /// Add a string message.
      /// </summary>
      /// <param name="location">message source location</param>
      /// <param name="message">string message to be added</param>
      public void Add(String location, String message)
      {
         if (String.IsNullOrEmpty(message))
            return;

         IMessageLogEntry m = CreateMessageLogEntry();

         m.Severity = SeverityLevel.Fatal;
         m.Source = location;
         m.Message = message;
         m.Exception = null;

         Add(m);
      }

      public void Add(String Message)
      {
         Add(String.Empty, Message);
      }

      public void Add(String location, SeverityLevel severity, String message)
      {
         IMessageLogEntry lEntry = CreateMessageLogEntry();

         lEntry.Exception = null;
         lEntry.Source = location;
         lEntry.LoggedDateTime = DateTime.Now;
         lEntry.Severity = severity;
         lEntry.Message = message;

         Add(lEntry);
      }  // end of LogMessage

      public void Add(String location, SeverityLevel severity,
         Exception exception)
      {
         IMessageLogEntry lEntry = CreateMessageLogEntry();

         lEntry.Exception = exception;
         lEntry.Source = location;
         lEntry.LoggedDateTime = DateTime.Now;
         lEntry.Severity = severity;
         lEntry.Message = String.Empty;

         Add(lEntry);
      }  // end of LogMessage

      /// <summary>
      /// Add a message.
      /// </summary>
      /// <param name="source">source</param>
      /// <param name="exception">exception to add</param>
      public void Add(String location, Exception exception)
      {
         IMessageLogEntry m = CreateMessageLogEntry();
         m_Success = false;

         if (location != null)
         {
            if (location.Trim().Length > 0)
               m.Source = location.Trim() + ":: " + exception.Source;
            else
               m.Source = exception.Source;
         }
         else
            m.Source = exception.Source;

         m.LoggedDateTime = DateTime.Now;
         m.Severity = SeverityLevel.Fatal;
         m.Message = exception.Message;
         m.Exception = exception;

         Add(m);
      }  // end of Add message

      /// <summary>
      /// Add the exceptions message in and set the LastExceptions
      /// to this...  Exceptions are not added but are used to set
      /// failure and the internal LastException...
      /// </summary>
      /// <param name="exception">register exception...</param>
      public void Add(Exception exception)
      {
         Failed(exception);
      }  // end of Add

      //public void Add(
      //   String location, Application.ApplicationException exception)
      //{
      //   IMessageLogEntry m = CreateMessageLogEntry();
      //   m_Success = false;

      //   if (location != null)
      //   {
      //      if (location.Trim().Length > 0)
      //         m.Source = location.Trim() + ":: " + exception.Source;
      //      else
      //         m.Source = exception.Source;
      //   }
      //   else
      //      m.Source = exception.Source;

      //   m.LoggedDateTime = DateTime.Now;
      //   m.Severity = SeverityLevel.Fatal;
      //   m.Message = exception.Message;
      //   m.Exception = exception;

      //   Add(m);
      //}  // end of Add message

      //public void Add(Application.ApplicationException exception)
      //{
      //   Add(null, exception);
      //}  // end of Add Message

      /// <summary>
      /// Add a message based on Code Process Event codes...
      /// </summary>
      /// <param name="code">code to add</param>
      /// <param name="details">additional details</param>
      public void Add(EventCode code, String details)
      {
         SeverityLevel severity = ((Int32)code) < 0 ?
            SeverityLevel.Fatal : SeverityLevel.Info;
         if (severity == SeverityLevel.Fatal)
            m_Success = false;

         IMessageLogEntry m = CreateMessageLogEntry();
         m.Message = String.IsNullOrWhiteSpace(details) ?
            code.ToString() : details;
         m.Severity = severity;
         m.LoggedDateTime = DateTime.Now;

         Add(m);
      }

      public Exception GetLastException()
      {
         if (Count <= 0)
            return (null);
         return ((Exception)m_Messages[Count - 1].Exception);
      }  // end of GetLastException

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public override String ToString()
      {
         StringBuilder sb = new StringBuilder();
         foreach (IMessageLogEntry m in m_Messages)
         {
            sb.Append(m.Message + " ");
         }
         return sb.ToString();
      }

      #endregion
      #region    -- 1.14 Copy Items...

      /// <summary>
      /// Create a new instance of a document and copy all inner messages and
      /// status fields.
      /// </summary>
      /// <typeparam name="R">log type</typeparam>
      /// <returns>new results copy</returns>
      //public ResultsLog<R> Copy<R>()
      //{
      //   ResultsLog<R> resultsCopy = new ResultsLog<R>();
      //   foreach (IMessageLogEntry i in m_Messages)
      //   {
      //      resultsCopy.Add(i);
      //   }
      //   resultsCopy.m_LastException = m_LastException;
      //   resultsCopy.m_Success = m_Success;
      //   return resultsCopy;
      //}

      /// <summary>
      /// Copy results from one ResultsLog to this one.
      /// </summary>
      /// <typeparam name="R">log type</typeparam>
      /// <param name="results">log to copy</param>
      //public void Copy<R>(ResultsLog<R> results)
      //{
      //   foreach (IMessageLogEntry i in results.Messages)
      //   {
      //      Messages.Add(i);
      //   }
      //   m_LastException = results.LastException;
      //   m_Success = results.Success;
      //}

      /// <summary>
      /// Copy a results log into this instance.
      /// </summary>
      /// <param name="log">log to copy</param>
      public void Copy(ResultLog log)
      {
         for (Int32 i = 0; i < log.Count; i++)
         {
            Add(log[i].Message);
         }
         m_Success = log.Success;
         m_LastException = log.GetLastException();
      }

      /// <summary>
      /// Copy message list.
      /// </summary>
      /// <param name="messages">message entries</param>
      public void Copy(List<Diagnostics.IMessageLogEntry> messages)
      {
         IMessageLogEntry entry;
         foreach (Diagnostics.IMessageLogEntry e in messages)
         {
            entry = CreateMessageLogEntry();
            entry.Tag = e.Tag;
            entry.LoggedDateTime = e.LoggedDateTime;
            entry.Message = e.Message;
            entry.Exception = e.Exception;
            entry.Severity = e.Severity;
            entry.Source = e.Source;
            Add(entry);
         }
      }

      /// <summary>
      /// Copy messages.
      /// </summary>
      /// <param name="messages">messages to copy</param>
      public void Copy(List<String> messages)
      {
         IMessageLogEntry entry;
         foreach (String e in messages)
         {
            entry = CreateMessageLogEntry();
            entry.Message = e;
            entry.Severity = Diagnostics.SeverityLevel.Fatal;
            Add(entry);
         }
      }

      /// <summary>
      /// Copy a results log into this instance.
      /// </summary>
      /// <param name="log">log to copy</param>
      public void Copy(IResultsLog log)
      {
         foreach (IMessageLogEntry e in log.Messages)
            Add(e);
         m_Success = log.Success;
         m_LastException = log.LastException;
      }

      #endregion
      #region -- 1.20 Delegates and Callback Support

      /// <summary>
      /// Default Verbosity.
      /// </summary>
      public static Verbosity DefaultVerbosity = Verbosity.Debugging;

      /// <summary>
      /// Message Log Handler is the same for any log to allow sending the
      /// notification to others when a message is logged.
      /// </summary>
      private static LogMessageEvent m_LogMessageHandler = null;
      public static LogMessageEvent LogMessageHandler
      {
         get { return m_LogMessageHandler; }
         set
         {
            m_LogMessageHandler = value;
         }
      }

      #endregion
      #region    -- 1.30 Object Initialization - Configuration

      /// <summary>
      /// Clear all including the list of messages.
      /// </summary>
      public void Clear()
      {
         m_Success = false;
         m_ReturnValue = 0;
         Tag = null;
         if (m_Messages == null)
            return;
         m_Messages.Clear();
      }

      /// <summary>
      /// Initialize Log.
      /// </summary>
      protected void InitializeLog()
      {
         m_Messages = new List<IMessageLogEntry>();
         m_Success = false;
         m_LastException = null;
         ReturnText = String.Empty;
         Clear();
      }

      public ResultLog()
      {
         InitializeLog();
      }

      #endregion

   }

}
