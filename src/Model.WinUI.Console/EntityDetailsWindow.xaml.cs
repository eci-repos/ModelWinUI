using Microsoft.UI.Xaml;

using Windows.Graphics;

using Model.Data;
using ModelConsole.Controls;
using ModelConsole.Graph;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModelWinUI
{
   /// <summary>
   /// Backlog 042: the modeless entity-details window. Double-clicking a table
   /// (or FK connector) on the drawing opens it; File → Model info opens it in
   /// model mode. It hosts the reusable <see cref="EntityInspectorControl"/>
   /// (read-only by default, Edit/Done toggle in its header) and drives it
   /// over the model state the host's editor owns. The window is resizable by
   /// default; the host reuses one instance across double-clicks.
   /// </summary>
   public sealed partial class EntityDetailsWindow : Window
   {
      /// <summary>
      /// The editor the window's inspector is wired to. Attached once, when the
      /// window is first shown; the inspector then edits the shared model and
      /// re-renders the drawing + explorer through the editor exactly like the
      /// retired in-editor inspector.
      /// </summary>
      private ModelEditorControl _editor;

      public EntityDetailsWindow()
      {
         this.InitializeComponent();
         Title = "Entity details";

         // A usable default size for the modeless details window (resizable).
         AppWindow.Resize(new SizeInt32(440, 640));
      }

      /// <summary>
      /// Attach the editor once (idempotent): wire the window's inspector to
      /// the editor's model panel (edits + pins re-render both renderers) and
      /// sync it to the current visibility + base color.
      /// </summary>
      public void Attach(ModelEditorControl editor)
      {
         if (editor == null || ReferenceEquals(editor, _editor))
         {
            return;
         }
         _editor = editor;
         editor.WireInspector(Inspector);
      }

      /// <summary>
      /// Show a graphic entity — a <see cref="TableInfo"/> or an
      /// <see cref="FkRelation"/> — in the window's inspector.
      /// </summary>
      public void ShowEntity(object entity)
      {
         if (_editor == null)
         {
            return;
         }
         if (entity is TableInfo table)
         {
            Inspector.ShowTable(table, _editor.Tables, _editor.Enumerations);
         }
         else if (entity is FkRelation edge)
         {
            Inspector.ShowConnector(edge, _editor.Tables);
         }
      }

      /// <summary>
      /// Show the model-level readout (provenance + model metadata) — File →
      /// Model info.
      /// </summary>
      public void ShowModelInfo()
      {
         if (_editor == null)
         {
            return;
         }
         Inspector.ShowModel(_editor.CurrentProvenance, _editor.CurrentMetadata);
      }
   }
}
