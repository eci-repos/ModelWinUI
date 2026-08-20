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
using Windows.UI;

using ModelConsole.Controls.Helpers;
using ModelConsole.Controls.ViewModels;
using ModelConsole.Palette;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace ModelConsole.Controls
{
   public sealed partial class DiagnosticsLogControl : UserControl
   {
      DiagnosticsLogViewModel m_ViewModel;

      /// <summary>
      /// The log area's panel color (backlog 041) — the panel body follows
      /// the drawing-surface "Base:" color the renderer bar selects; defaults
      /// to the shared canvas background (white).
      /// </summary>
      private Color _backgroundColor =
         HexColor.FromHex(TablePalette.CanvasBackgroundHex);

      public DiagnosticsLogControl()
      {
         this.InitializeComponent();
         m_ViewModel = Ioc.Default.GetRequiredService<DiagnosticsLogViewModel>();
         DataContext = m_ViewModel;

         // The header strip keeps its pastel chrome; the panel body (root +
         // the log list itself) paints the base color so the working area
         // matches the drawing surface.
         RootGrid.Background = new SolidColorBrush(_backgroundColor);
         LogList.Background = new SolidColorBrush(_backgroundColor);
      }

      /// <summary>
      /// The log panel's background color (backlog 041) — follows the
      /// renderer bar's "Base:" selection, defaulting to white.
      /// </summary>
      public Color BackgroundColor
      {
         get { return _backgroundColor; }
         set
         {
            _backgroundColor = value;
            if (RootGrid != null)
            {
               RootGrid.Background = new SolidColorBrush(value);
               LogList.Background = new SolidColorBrush(value);
            }
         }
      }

      private void ClearViewButton_Click(object sender, RoutedEventArgs e)
      {
         m_ViewModel.ClearView();
      }

   }
}
