using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
using ModelConsole.ViewModels;
using ModelConsole.Model.Diagnostics;

namespace ModelConsole.Controls
{
   public sealed partial class DiagnosticsLogControl : UserControl
   {
      DiagnosticsLogViewModel m_ViewModel = new DiagnosticsLogViewModel();

      public DiagnosticsLogControl()
      {
         this.InitializeComponent();
         DataContext = m_ViewModel;
      }

      private void ClearViewButton_Click(object sender, RoutedEventArgs e)
      {
         m_ViewModel.ClearView();
      }

   }
}
