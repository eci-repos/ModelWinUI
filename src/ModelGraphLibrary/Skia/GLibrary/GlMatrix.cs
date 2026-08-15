using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModelConsole.Skia.GLibrary
{

    public class GlMatrix
    {
        public SKMatrix Matrix { get; set; }
        public GlMatrix()
        {
            Matrix = SKMatrix.Identity;
        }
        public void SetCoordinateSystem(SKCanvas canvas)
        {
            canvas.SetMatrix(Matrix);
        }
    }

}
