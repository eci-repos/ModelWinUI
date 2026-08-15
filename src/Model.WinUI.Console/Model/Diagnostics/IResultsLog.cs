using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelConsole.Model.Diagnostics
{

   public interface IResultsLog
   {

      Verbosity Verbosity { get; set; }
      Object DataObject { get; }
      Boolean Success { get; }
      List<IMessageLogEntry> Messages { get; }
      String MessageText { get; }
      Exception LastException { get; }
      Int32 ReturnValue { get; set; }
      string ReturnText { get; }

      IMessageLogEntry CreateMessageLogEntry();

      void Add(Exception exception);
      void Add(String message);
      void Add(String source, Exception exception);
      void Add(String source, String message);
      void Add(EventCode code, String details);
      //void Add(DataObjects.CodeProcess.EventCode code);

      void Succeeded();

      void Failed(Exception exception);
      void Failed(String message);
      void Failed(String source, Exception exception);
      void Failed(String source, String message);
      //void Failed(DataObjects.CodeProcess.EventCode code, String details);
      //void Failed(DataObjects.CodeProcess.EventCode code);

      void Copy(ResultLog log);
      void Copy(IResultsLog log);
      void Copy(List<Diagnostics.IMessageLogEntry> messages);
      void Copy(List<String> messages);

      void Write(IMessageLogEntry entry);

      void Clear();

   }

}
