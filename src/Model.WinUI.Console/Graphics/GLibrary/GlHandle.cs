using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Devices.PointOfService;
using Windows.Foundation;

using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace ModelConsole.Graphics.GLibrary
{

   /// <summary>
   /// Handles allow to select and drag/move the object around.  Handles are 
   /// represented as circles.
   /// </summary>
   public class GlHandle : GlGrabberBase<Ellipse>, IGlGrabber
   {

   }

}
