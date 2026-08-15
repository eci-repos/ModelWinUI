using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ModelConsole.Model.Helpers;
using ModelConsole.Model.Diagnostics;

namespace ModelConsole.ViewModels
{

   public class DiagnosticsLogViewModel : ObservableObject
   {
      private ResultLog m_ResultLog = new ResultLog();
      private readonly LogMessageEvent m_MessageEvent;
      private readonly ObservableCollection<IMessageLogEntry> m_Items;

      public ObservableCollection<IMessageLogEntry> Items
      {
         get { return m_Items; }
      }

      public DiagnosticsLogViewModel()
      {
         m_Items = new ObservableCollection<IMessageLogEntry>();
         m_MessageEvent = new LogMessageEvent(HandleNotification);

         ResultLog.LogMessageHandler -= m_MessageEvent;
         ResultLog.LogMessageHandler += m_MessageEvent;

         MessageLogEntry entry = new MessageLogEntry();
         entry.Message = "Diagnostics Log Started";
         entry.Severity = SeverityLevel.Info;
         m_ResultLog.Write(entry);
      }

      public void ClearView()
      {
         Items.Clear();
      }

      /// <summary>
      /// Show provided message in the log List View control.
      /// </summary>
      /// <param name="sender">sender</param>
      /// <param name="e">event arguments</param>
      private void HandleNotification(object sender, LogMessageEventArgs e)
      {
         if (e.Message == null)
         {
            return;
         }

         if (String.IsNullOrWhiteSpace(e.Message.Source))
         {
            e.Message.Source = sender == null ?
               null : sender.GetType().FullName;
         }

         m_Items.Add(e.Message);
      }

   }

}
