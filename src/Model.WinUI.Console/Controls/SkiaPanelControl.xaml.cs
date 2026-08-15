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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236
using SkiaSharp.Views;
using SkiaSharp;
using Model.Data;
using Model.Test;
using ModelConsole.Skia.GLibrary;
using ModelConsole.Skia.Primitives;

namespace ModelConsole.Controls
{

    public sealed partial class SkiaPanelControl : UserControl
   {
      public SkiaPanelControl()
      {
         this.InitializeComponent();
      }

      private void SkiaCanvas_PaintSurface(
         object sender, SkiaSharp.Views.Windows.SKPaintSurfaceEventArgs e)
      {
         GlFrame frame = new GlFrame(e.Surface);
         GlModel model = new GlModel();

         var e1 = Data_Table_Entity.GetPersonTable();
         var t1 = Table.DrawTable(frame, 10, 80, 30, e1);
         model.Add(t1);

         var e2 = Data_Table_Entity.GetPersonNameTable();
         var t2 = Table.DrawTable(frame, 500, 80, 30, e2);
         model.Add(t2);
      }

   }

}
