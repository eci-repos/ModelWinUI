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
using Windows.Storage.Pickers;

using Model.Data;

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

      /// <summary>
      /// File → Open Model…: pick a JSON model file and render it in both
      /// renderers (backlog 004).
      /// </summary>
      private async void OpenModel_Click(object sender, RoutedEventArgs e)
      {
         var picker = new FileOpenPicker();
         picker.FileTypeFilter.Add(".json");

         // Unpackaged apps must initialize the picker with the window handle,
         // otherwise it throws (0x80070005 / "class not registered").
         var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
         WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

         var file = await picker.PickSingleFileAsync();
         if (file == null)
         {
            return;
         }

         try
         {
            var tables = ModelFile.Load(file.Path);
            XamlEditor.SetModel(tables);
            SkiaEditor.SetModel(tables);
         }
         catch (Exception ex)
         {
            var dialog = new ContentDialog
            {
               Title = "Could not open model",
               Content = ex.Message,
               CloseButtonText = "OK",
               XamlRoot = RootGrid.XamlRoot
            };
            await dialog.ShowAsync();
         }
      }
   }
}
