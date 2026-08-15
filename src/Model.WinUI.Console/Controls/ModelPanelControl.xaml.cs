using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI;

using Model.Test;
using ModelConsole.Graphics.GLibrary;
using ModelConsole.Graphics.Primitives;
using ModelConsole.Graphics.GLibrary.GlOrtho;
using ModelConsole.Model.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModelConsole.Controls
{
    public sealed partial class ModelPanelControl : UserControl
   {
      private GlContext _context;

      public ModelPanelControl()
      {
         this.InitializeComponent();
         _context = new GlContext(ModelCanvas);

         DrawRectangle();
         WriteMessage("GL Context Ready.");
      }

      public void WriteMessage(string message)
      {
         if (_context.Writer != null)
         {
            _context.Writer.WriteMessage(message);
         }
      }

      public void DrawRectangle()
      {
         //GlRectangle r = GlRectangle.Draw(_frame, 10, 10, 300, 600, 10);
         //GlRectangle.AddBanner(_frame, r, "THIS IS THE TITLE");

         GlModel model = new GlModel();

         var e1 = Data_Table_Entity.GetPersonTable();
         var t1 = Table.DrawTable(_context, 10, 80, 40, e1);
         t1.SetBackground(Colors.LightYellow);
         model.Add(t1);

         var e2 = Data_Table_Entity.GetPersonNameTable();
         var t2 = Table.DrawTable(_context, 500, 80, 40, e2);
         t2.SetBackground(Colors.Honeydew);
         model.Add(t2);

         GlOrthoPath.Draw(_context, 10, 600, 100, 800);
         GlOrthoPath.Draw(_context, 200, 600, 110, 800);
         GlOrthoPath.Draw(_context, 410, 600, 500, 800, GlSide.Top);
         GlOrthoPath.Draw(_context, 500, 600, 410, 800, GlSide.Top);
      }

   }
}
