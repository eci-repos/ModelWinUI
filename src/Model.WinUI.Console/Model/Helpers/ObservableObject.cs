using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Core;

using mvvm = CommunityToolkit.Mvvm;
using Microsoft.UI.Xaml;

// -----------------------------------------------------------------------------
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using ModelConsole.Model.DataObjects;

namespace ModelConsole.Model.Helpers
{

   /// <summary>
   /// Observable object with INotifyPropertyChanged implemented
   /// </summary>
   public class ObservableObject : mvvm.ComponentModel.ObservableObject
   {
      public const string STATUS_MESSAGE_TEXT = "StatusMessageText";

      public DispatcherQueue Dispatcher = null;

      private Visibility m_Editing = Visibility.Collapsed;
      public Visibility Editing
      {
         get { return m_Editing; }
         set
         {
            if (m_Editing != value)
            {
               m_Editing = value;
               OnPropertyChanged("Editing");
            }
         }
      }

      private Visibility m_InEditor = Visibility.Collapsed;
      public Visibility InEditor
      {
         get { return m_InEditor; }
         set
         {
            if (m_InEditor != value)
            {
               m_InEditor = value;
               OnPropertyChanged(DataElementName.InEditor);
            }
         }
      }

      private Visibility m_InSearch = Visibility.Visible;
      public Visibility InSearch
      {
         get { return m_InSearch; }
         set
         {
            if (m_InSearch != value)
            {
               m_InSearch = value;
               OnPropertyChanged(DataElementName.InSearch);
            }
         }
      }

      private Visibility m_InViewer = Visibility.Visible;
      public Visibility InViewer
      {
         get { return m_InViewer; }
         set
         {
            if (m_InViewer != value)
            {
               m_InViewer = value;
               OnPropertyChanged(DataElementName.InViewer);
            }
         }
      }

      private Visibility m_IsAdding = Visibility.Visible;
      public Visibility IsAdding
      {
         get { return m_IsAdding; }
         set
         {
            if (m_IsAdding != value)
            {
               m_IsAdding = value;
               OnPropertyChanged(DataElementName.IsAdding);
               //OnPropertyChanged(DataElementName.NotAdding);
            }
         }
      }

      public Visibility NotAdding
      {
         get
         {
            return (IsAdding == Visibility.Visible) ?
            Visibility.Collapsed : Visibility.Visible;
         }
      }

      protected string m_StatusMessage;

      public bool InvokeOnMainThread(
         Microsoft.UI.Dispatching.DispatcherQueueHandler handler)
      {
         return Dispatcher.TryEnqueue(handler);
      }

      private Visibility Visible(bool value)
      {
         return value ? Visibility.Visible : Visibility.Collapsed;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="inViewer"></param>
      /// <param name="inSearch"></param>
      /// <param name="inEditor"></param>
      /// <param name="isAdding"></param>
      public void SetIndicators(bool inViewer = false, bool inSearch = false,
         bool inEditor = false, bool isAdding = false, bool editing = false)
      {
         InViewer = Visible(inViewer);
         InSearch = Visible(inSearch);
         InEditor = Visible(inEditor);
         IsAdding = Visible(isAdding);
         Editing = Visible(editing);
      }

      //[Ignore]
      //public string StatusMessageText
      //{
      //   get { return m_StatusMessage; }
      //   set
      //   {
      //      m_StatusMessage = value;
      //      OnPropertyChanged(STATUS_MESSAGE_TEXT);
      //   }
      //}

      /// <summary>
      /// Sets the property.
      /// </summary>
      /// <returns><c>true</c>, if property was set, <c>false</c> otherwise.</returns>
      /// <param name="backingStore">Backing store.</param>
      /// <param name="value">Value.</param>
      /// <param name="propertyName">Property name.</param>
      /// <param name="onChanged">On changed.</param>
      /// <typeparam name="T">The 1st type parameter.</typeparam>
      //protected bool SetProperty<T>(
      //   ref T backingStore, T value,
      //   [CallerMemberName]string propertyName = "",
      //   Action onChanged = null)
      //{
      //   if (EqualityComparer<T>.Default.Equals(backingStore, value))
      //      return false;

      //   backingStore = value;
      //   onChanged?.Invoke();
      //   OnPropertyChanged(propertyName);
      //   return true;
      //}

#if USING_MICROSOFT_TOOLKIT
      /// <summary>
      /// Occurs when property changed.
      /// </summary>
      public event PropertyChangedEventHandler PropertyChanged;

      /// <summary>
      /// Raises the property changed event.
      /// </summary>
      /// <param name="propertyName">Property name.</param>
      protected void OnPropertyChanged([CallerMemberName]string propertyName = "")
      {
         if (PropertyChanged != null)
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
#endif

   }

}
