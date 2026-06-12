using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

class ScanNew
{
    static void Main()
    {
        string[][] images = new string[][] {
            new[] { @"E:\飞牛同步\Workspace\Bitfun\img\维寒迪密室.png", "密室", "0" },
            new[] { @"E:\飞牛同步\Workspace\Bitfun\img\维寒迪撬棍房.png", "撬棍房", "1" },
            new[] { @"E:\飞牛同步\Workspace\Bitfun\img\维寒迪熊洞.png", "熊洞", "2" },
        };

        List<string> allDots = new List<string>();
        List<string> allTypes = new List<string>();

        foreach (var img in images)
        {
            string file = img[0];
            string name = img[1];
            int targetType = int.Parse(img[2]);

            using (Bitmap bmp = new Bitmap(file))
            {
                int w = bmp.Width, h = bmp.Height;
                Console.WriteLine("{0} ({1}x{2})", name, w, h);

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
                        byte pb = pixels[off], pg = pixels[off+1], pr = pixels[off+2];

                        if (pr > 180 && pg < 130 && pb < 130)
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
                                            if (pixels[noff+2] > 180 && pixels[noff+1] < 130 && pixels[noff] < 130)
                                            {
                                                visited[nx, ny] = true;
                                                queue.Enqueue(new MyPoint(nx, ny));
                                            }
                                        }
                                    }
                            }

                            if (cluster.Count > 5)
                            {
                                int sx = 0, sy = 0;
                                foreach (MyPoint p in cluster) { sx += p.X; sy += p.Y; }
                                dots.Add(new MyPoint(sx / cluster.Count, sy / cluster.Count));
                            }
                        }
                        else visited[x, y] = true;
                    }
                }

                Console.WriteLine("  Found {0} red dots", dots.Count);
                dots.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));

                foreach (var d in dots)
                {
                    allDots.Add(string.Format("{{{0},{1}}}", d.X, d.Y));
                    allTypes.Add(targetType.ToString());
                }
            }
        }

        Console.WriteLine("\n=== Combined ({0} total) ===", allDots.Count);
        Console.WriteLine("Dots:");
        for (int i = 0; i < allDots.Count; i++)
        {
            if (i % 5 == 0) Console.Write("    ");
            Console.Write(allDots[i]);
            if (i < allDots.Count - 1) Console.Write(", ");
            if (i % 5 == 4) Console.WriteLine();
        }
        Console.WriteLine("\n\nTypes:");
        for (int i = 0; i < allTypes.Count; i++)
        {
            if (i > 0 && i % 10 == 0) Console.WriteLine();
            Console.Write(allTypes[i]);
            if (i < allTypes.Count - 1) Console.Write(",");
        }
    }
}

class MyPoint { public int X, Y; public MyPoint(int x, int y) { X = x; Y = y; } }
