using Microsoft.UI.Xaml.Controls;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelConsole.Skia.GLibrary
{

    public class GlText
    {

        public static void DrawText(GlFrame frame, string text, float x, float y)
        {
            frame.Canvas.DrawText(text, x + 10, y + 10, frame.DefaultFont);
        }

    }

}
