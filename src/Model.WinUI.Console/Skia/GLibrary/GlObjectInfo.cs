using System;
using System.Collections.Generic;
using System.Text;

namespace ModelConsole.Skia.GLibrary
{

    public class GlObjectInfo
    {
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }
    }

}
