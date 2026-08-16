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

using Model.Data;
using ModelConsole.Graph;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModelConsole.Controls
{
   public sealed partial class ModelEditorControl : UserControl
   {
      public ModelEditorControl()
      {
         this.InitializeComponent();

         // Route clicks on the canvas to the inspector, and inspector
         // actions (edit / delete) back to the model panel for a re-render.
         ModelPanel.EntitySelected += (s, entity) =>
         {
            if (entity is TableInfo table)
            {
               InspectorPanel.ShowTable(table);
            }
            else if (entity is FkRelation edge)
            {
               InspectorPanel.ShowConnector(edge);
            }
         };

         InspectorPanel.ModelEdited += (s, e) => ModelPanel.Refresh();
         InspectorPanel.DeleteRequested += (s, edge) => ModelPanel.DeleteConnector(edge);
      }
   }
}
