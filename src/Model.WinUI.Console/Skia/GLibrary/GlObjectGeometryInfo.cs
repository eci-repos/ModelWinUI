using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModelConsole.Skia.GLibrary
{

    public class GlObjectGeometryInfo
    {

        public SKPoint Origin { get; set; } = new SKPoint(0, 0);

        /// <summary>
        /// Space around the object
        /// </summary>
        public SKRect Margin { get; set; } = new SKRect();

        /// <summary>
        /// Space around the inside of the perimiter of the object
        /// </summary>
        public SKRect Padding { get; set; } = new SKRect();

    }

}
