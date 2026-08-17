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

namespace ModelWinUI
{
   /// <summary>
   /// An empty window that can be used on its own or navigated to within a Frame.
   /// </summary>
   public sealed partial class MainWindow : Window
   {
      public MainWindow()
      {
         this.InitializeComponent();
         Title = "EDAM Studio";
      }

      /// <summary>
      /// Switch the main view between the XAML ERD and the Skia ERD (backlog
      /// 003). The two toggle buttons are mutually exclusive; the other
      /// control's <see cref="Visibility"/> is collapsed so it keeps its
      /// state (zoom/pan for the XAML path, the cached diagram for the Skia
      /// path) across switches.
      /// </summary>
      private void RendererToggle_Click(object sender, RoutedEventArgs e)
      {
         bool skia = ReferenceEquals(sender, SkiaToggle);

         XamlToggle.IsChecked = !skia;
         SkiaToggle.IsChecked = skia;
         XamlEditor.Visibility = skia ? Visibility.Collapsed : Visibility.Visible;
         SkiaEditor.Visibility = skia ? Visibility.Visible : Visibility.Collapsed;
      }
   }
}
