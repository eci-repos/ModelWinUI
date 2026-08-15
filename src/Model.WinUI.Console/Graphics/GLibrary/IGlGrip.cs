using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Foundation;

namespace ModelConsole.Graphics.GLibrary
{

   public interface IGlGrip
   {
      bool Selected { get; set; }
      Shape Shape { get; }
      List<GlGrip> GetGripNodes();
      GlGrip GetGripNode(Point point);
   }

}
