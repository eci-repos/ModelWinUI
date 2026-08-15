using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelConsole.Model.Diagnostics
{

   /// <summary>
   /// Define supported severity levels.
   /// </summary>
   /// <author>Eduardo Sobrino</author>
   /// <date>Oct/2k3</date>
   public enum SeverityLevel
   {
      Fatal = 0,
      Critical = 1,
      Warning = 2,
      Info = 3,
      Debug = 4,
      Unknown = 99
   }

}
