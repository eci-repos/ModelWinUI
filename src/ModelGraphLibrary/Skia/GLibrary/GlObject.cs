using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelConsole.Skia.GLibrary
{

    public class GlObject : GlObjectInfo
    {
        private object m_Instance { get; set; } = null;
        public object Instance
        {
            get { return m_Instance; }
        }
        public GlObject(object instance)
        {
            m_Instance = instance;
        }
    }

}
