using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

class ConvertCoords
{
    static void Main()
    {
        float imgW = 1026, imgH = 1022;
        float sw = 1920, sh = 1080;
        float scale = Math.Min(sw / imgW, sh / imgH);
        float offX = (sw - imgW * scale) / 2f;
        float offY = (sh - imgH * scale) / 2f;

        Console.WriteLine("scale={0} offX={1} offY={2}", scale, offX, offY);

        // Scanner results from re.png (screen coords at 1920x1080)
        int[,] screenDots = new int[,] {
            {1097, 88}, {600, 242}, {965, 262}, {1284, 275}, {762, 293},
            {1284, 364}, {1142, 455}, {614, 470}, {817, 498}, {1161, 548},
            {1036, 587}, {1314, 651}, {1085, 663}, {781, 684}, {584, 734},
            {1002, 786}, {856, 890}, {933, 890}, {1170, 894}, {912, 922},
            // {1705, 982}, // UI element, discard
        };

        Console.WriteLine("\nImage coordinates (for code):");
        Console.Write("Dots = new int[,] {\n");
        for (int i = 0; i < screenDots.GetLength(0); i++)
        {
            float sx = screenDots[i, 0];
            float sy = screenDots[i, 1];
            float imgX = (sx - offX) / scale;
            float imgY = sy / scale;
            int ix = (int)Math.Round(imgX);
            int iy = (int)Math.Round(imgY);
            string comma = (i < screenDots.GetLength(0) - 1) ? "," : "";
            Console.WriteLine("    {{{0}, {1}}}{2} // screen({3},{4})", ix, iy, comma, sx, sy);
        }
        Console.WriteLine("}");
    }
}
