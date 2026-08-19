using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ModelConsole.Model.Helpers;
using ModelConsole.Diagnostics;

namespace ModelConsole.ViewModels
{

   public class DiagnosticsLogViewModel : ObservableObject
   {
      private readonly ILogService m_Log;
      private readonly LogMessageEvent m_MessageEvent;
      private readonly ObservableCollection<IMessageLogEntry> m_Items;

      public ObservableCollection<IMessageLogEntry> Items
      {
         get { return m_Items; }
      }

      public DiagnosticsLogViewModel(ILogService log)
      {
         m_Items = new ObservableCollection<IMessageLogEntry>();
         m_Log = log;
         m_MessageEvent = new LogMessageEvent(HandleNotification);

         m_Log.LogMessageHandler -= m_MessageEvent;
         m_Log.LogMessageHandler += m_MessageEvent;

         m_Log.Write(
            MessageLogEntry.GetEntry("Diagnostics Log Started", SeverityLevel.Info));
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
