using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

class ScanVikendi2
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
            List<DotCluster> allDots = new List<DotCluster>();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (visited[x, y]) continue;
                    int off = y * data.Stride + x * 4;
                    byte pb = pixels[off];
                    byte pg = pixels[off + 1];
                    byte pr = pixels[off + 2];

                    // Determine color type (wider thresholds for detection)
                    int type = -1;
                    if (pr > 180 && pg < 130 && pb < 130) type = 0; // Red
                    else if (pr > 110 && pg < 80 && pb > 140) type = 1; // Purple  
                    else if (pr < 120 && pg > 150 && pb < 120) type = 2; // Green

                    if (type >= 0)
                    {
                        List<MyPoint> cluster = new List<MyPoint>();
                        Queue<MyPoint> queue = new Queue<MyPoint>();
                        queue.Enqueue(new MyPoint(x, y));
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
                                        if (type == 0 && nr > 180 && ng < 130 && nb < 130) match = true;
                                        else if (type == 1 && nr > 110 && ng < 80 && nb > 140) match = true;
                                        else if (type == 2 && nr < 120 && ng > 150 && nb < 120) match = true;
                                        if (match) { visited[nx, ny] = true; queue.Enqueue(new MyPoint(nx, ny)); }
                                    }
                                }
                        }

                        if (cluster.Count >= 5)
                        {
                            int sx = 0, sy = 0;
                            foreach (MyPoint p in cluster) { sx += p.X; sy += p.Y; }
                            allDots.Add(new DotCluster(sx / cluster.Count, sy / cluster.Count, type, cluster.Count));
                        }
                    }
                    else visited[x, y] = true;
                }
            }

            string[] typeNames = { "Red", "Purple", "Green" };
            Console.WriteLine("维寒迪 ({0}x{1}): {2} raw clusters\n", w, h, allDots.Count);

            // Print all raw clusters for debugging
            Console.WriteLine("=== Raw clusters ===");
            foreach (var d in allDots)
                Console.WriteLine("  {0} ({1},{2}) size={3}", typeNames[d.Type], d.X, d.Y, d.Size);

            // Merge clusters that are within 10px of each other (same type)
            bool merged;
            do
            {
                merged = false;
                for (int i = 0; i < allDots.Count && !merged; i++)
                {
                    for (int j = i + 1; j < allDots.Count && !merged; j++)
                    {
                        if (allDots[i].Type == allDots[j].Type)
                        {
                            int dx = allDots[i].X - allDots[j].X;
                            int dy = allDots[i].Y - allDots[j].Y;
                            double dist = Math.Sqrt(dx * dx + dy * dy);
                            if (dist < 12)
                            {
                                // Merge j into i
                                int totalSize = allDots[i].Size + allDots[j].Size;
                                allDots[i] = new DotCluster(
                                    (allDots[i].X * allDots[i].Size + allDots[j].X * allDots[j].Size) / totalSize,
                                    (allDots[i].Y * allDots[i].Size + allDots[j].Y * allDots[j].Size) / totalSize,
                                    allDots[i].Type, totalSize);
                                allDots.RemoveAt(j);
                                merged = true;
                            }
                        }
                    }
                }
            } while (merged);

            Console.WriteLine("\n=== After merging (within 12px) ===");
            allDots.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
            foreach (var d in allDots)
                Console.WriteLine("  {0} ({1},{2}) size={3}", typeNames[d.Type], d.X, d.Y, d.Size);
            Console.WriteLine("\nTotal after merge: {0}", allDots.Count);

            // Output as code
            Console.WriteLine("\nDots with types:");
            Console.Write("Dots = new int[,] {\n");
            Console.Write("    //            type\n");
            for (int i = 0; i < allDots.Count; i++)
            {
                string comma = (i < allDots.Count - 1) ? "," : "";
                Console.WriteLine("    {{{0},{1}}}, // {2}", allDots[i].X, allDots[i].Y, allDots[i].Type);
            }
            Console.WriteLine("};\n");
            Console.Write("DotTypes = new byte[] {\n    ");
            for (int i = 0; i < allDots.Count; i++)
            {
                if (i > 0 && i % 10 == 0) Console.Write("\n    ");
                Console.Write("{0}", allDots[i].Type);
                if (i < allDots.Count - 1) Console.Write(",");
            }
            Console.WriteLine("\n};");
        }
    }
}

struct DotCluster { public int X, Y, Type, Size; public DotCluster(int x, int y, int t, int s) { X = x; Y = y; Type = t; Size = s; } }
class MyPoint { public int X, Y; public MyPoint(int x, int y) { X = x; Y = y; } }
