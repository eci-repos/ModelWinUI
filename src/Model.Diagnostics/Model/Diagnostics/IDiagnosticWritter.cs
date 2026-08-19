using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelConsole.Diagnostics
{

   public interface IDiagnosticWritter
   {
      void WriteMessage(string message);
   }

}
