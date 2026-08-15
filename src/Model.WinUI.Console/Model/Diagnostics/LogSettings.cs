using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// -----------------------------------------------------------------------------
// 2012-04-28 The following code was copied verbatum from the Open Knowledge -
// KIFv3r0 Framework Project.

namespace ModelConsole.Model.Diagnostics
{

   /// <summary>
   /// Define the diagnostics log settings
   /// </summary>
   //[Serializable]
   public class LogSettings
   {

      public SeverityLevel Severity { get; set; }
      public String SystemLogName { get; set; }
      public String SystemSourceName { get; set; }
      public Boolean SendToSystemLog { get; set; }
      public String ApplicationLogPath { get; set; }
      public Boolean SendToApplicationLog { get; set; }
      public LogStatus Status { get; set; }
      public Verbosity Verbosity { get; set; }
      public String KeyId { get; set; }
      public LogFormat Format { get; set; }

      public LogSettings()
      {
         ClearFields();
      }

      public void ClearFields()
      {
         Severity = SeverityLevel.Unknown;
         SystemLogName = String.Empty;
         SystemSourceName = String.Empty;
         SendToSystemLog = false;
         ApplicationLogPath = String.Empty;
         SendToApplicationLog = false;
         Status = LogStatus.Disable;
         Verbosity = Diagnostics.Verbosity.Unknown;
         KeyId = String.Empty;
         Format = LogFormat.XML;
      }

   }

}
