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

using CommunityToolkit.Mvvm.DependencyInjection;
using ModelConsole.Graphics.GLibrary;
using ModelConsole.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModelConsole.Controls
{
   public sealed partial class ModelPanelControl : UserControl
   {
      private readonly GlContext _context;
      private readonly IModelDataProvider _dataProvider;
      private readonly ITableFactory _tableFactory;
      private readonly IConnectorFactory _connectorFactory;

      public ModelPanelControl()
      {
         this.InitializeComponent();

         _dataProvider = Ioc.Default.GetRequiredService<IModelDataProvider>();
         _tableFactory = Ioc.Default.GetRequiredService<ITableFactory>();
         _connectorFactory = Ioc.Default.GetRequiredService<IConnectorFactory>();
         _context = new GlContext(
            ModelCanvas, Ioc.Default.GetRequiredService<ILogService>());

         DrawRectangle();
         WriteMessage("GL Context Ready.");
      }

      public void WriteMessage(string message)
      {
         _context.WriteMessage(message);
      }

      public void DrawRectangle()
      {
         //GlRectangle r = GlRectangle.Draw(_frame, 10, 10, 300, 600, 10);
         //GlRectangle.AddBanner(_frame, r, "THIS IS THE TITLE");

         IGlModel model = Ioc.Default.GetRequiredService<IGlModel>();

         var e1 = _dataProvider.GetPersonTable();
         var t1 = _tableFactory.Create(_context, 10, 80, 40, e1);
         t1.SetBackground(Colors.LightYellow);
         model.Add(t1);

         var e2 = _dataProvider.GetPersonNameTable();
         var t2 = _tableFactory.Create(_context, 500, 80, 40, e2);
         t2.SetBackground(Colors.Honeydew);
         model.Add(t2);

         _connectorFactory.Create(_context, 10, 600, 100, 800);
         _connectorFactory.Create(_context, 200, 600, 110, 800);
         _connectorFactory.Create(_context, 410, 600, 500, 800, GlSide.Top);
         _connectorFactory.Create(_context, 500, 600, 410, 800, GlSide.Top);
      }

   }
}
