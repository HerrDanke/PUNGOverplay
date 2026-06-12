using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

class ScanVikendi
{
    static void Main()
    {
        string file = @"E:\飞牛同步\Workspace\Bitfun\img\维寒迪.png";
        using (Bitmap bmp = new Bitmap(file))
        {
            int w = bmp.Width, h = bmp.Height;
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] pixels = new byte[data.Stride * h];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            bmp.UnlockBits(data);

            bool[,] visited = new bool[w, h];
            List<MyPoint> dots = new List<MyPoint>();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (visited[x, y]) continue;
                    int off = y * data.Stride + x * 4;
                    byte pb = pixels[off];
                    byte pg = pixels[off + 1];
                    byte pr = pixels[off + 2];

                    // Detect colored dots: red, purple, green
                    int type = -1;
                    if (pr > 180 && pg < 120 && pb < 120) type = 0; // Red
                    else if (pr > 120 && pg < 80 && pb > 140) type = 1; // Purple
                    else if (pr < 100 && pg > 160 && pb < 100) type = 2; // Green

                    if (type >= 0)
                    {
                        List<MyPoint> cluster = new List<MyPoint>();
                        Queue<MyPoint> queue = new Queue<MyPoint>();
                        queue.Enqueue(new MyPoint(x, y, type));
                        visited[x, y] = true;

                        while (queue.Count > 0)
                        {
                            MyPoint p = queue.Dequeue();
                            cluster.Add(p);
                            for (int dy = -1; dy <= 1; dy++)
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    int nx = p.X + dx, ny = p.Y + dy;
                                    if (nx >= 0 && nx < w && ny >= 0 && ny < h && !visited[nx, ny])
                                    {
                                        int noff = ny * data.Stride + nx * 4;
                                        byte nb = pixels[noff];
                                        byte ng = pixels[noff + 1];
                                        byte nr = pixels[noff + 2];
                                        bool match = false;
                                        if (type == 0 && nr > 180 && ng < 120 && nb < 120) match = true;
                                        else if (type == 1 && nr > 120 && ng < 80 && nb > 140) match = true;
                                        else if (type == 2 && nr < 100 && ng > 160 && nb < 100) match = true;
                                        if (match) { visited[nx, ny] = true; queue.Enqueue(new MyPoint(nx, ny, type)); }
                                    }
                                }
                        }

                        if (cluster.Count > 5)
                        {
                            int sx = 0, sy = 0;
                            foreach (MyPoint p in cluster) { sx += p.X; sy += p.Y; }
                            dots.Add(new MyPoint(sx / cluster.Count, sy / cluster.Count, type));
                        }
                    }
                    else visited[x, y] = true;
                }
            }

            string[] typeNames = { "Red", "Purple", "Green" };
            Console.WriteLine("维寒迪 ({0}x{1}): {2} dots", w, h, dots.Count);
            Console.WriteLine();
            
            // Count by type
            int[] counts = new int[3];
            foreach (MyPoint p in dots) counts[p.Type]++;
            Console.WriteLine("Red: {0}, Purple: {1}, Green: {2}", counts[0], counts[1], counts[2]);
            Console.WriteLine();

            Console.WriteLine("Dots array (sorted by Y then X):");
            dots.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
            Console.Write("Dots = new int[,] {\n");
            for (int i = 0; i < dots.Count; i++)
            {
                string typeStr = "RPG"[dots[i].Type].ToString();
                string comma = (i < dots.Count - 1) ? "," : "";
                Console.WriteLine("    {{{0},{1},{2}}} // {3}{4}", dots[i].X, dots[i].Y, dots[i].Type, typeStr, comma);
            }
            Console.WriteLine("};\n");

            // Also output separate arrays by type for clarity
            Console.WriteLine("\nBy type:");
            for (int t = 0; t < 3; t++)
            {
                Console.WriteLine("\nType {0} ({1}):", t, typeNames[t]);
                Console.Write("int[,] dots{0} = new int[,] {{", t);
                bool first = true;
                foreach (MyPoint p in dots)
                {
                    if (p.Type == t)
                    {
                        if (!first) Console.Write(",");
                        Console.Write("\n    {{{0},{1}}}", p.X, p.Y);
                        first = false;
                    }
                }
                Console.WriteLine("\n};");
            }
        }
    }
}

class MyPoint
{
    public int X, Y, Type;
    public MyPoint(int x, int y, int type = 0) { X = x; Y = y; Type = type; }
}
