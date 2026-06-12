using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

class VerifyDots
{
    static void Main()
    {
        string[] mapFiles = {
            @"E:\飞牛同步\Workspace\Bitfun\img\艾伦格.png",
            @"E:\飞牛同步\Workspace\Bitfun\img\米拉玛.png",
            @"E:\飞牛同步\Workspace\Bitfun\img\泰戈.png",
            @"E:\飞牛同步\Workspace\Bitfun\img\维寒迪.png",
            @"E:\飞牛同步\Workspace\Bitfun\img\帕拉莫.png",
            @"E:\飞牛同步\Workspace\Bitfun\img\帝斯顿.png",
            @"E:\飞牛同步\Workspace\Bitfun\img\荣都.png",
        };
        string[] mapNames = { "艾伦格", "米拉玛", "泰戈", "维寒迪", "帕拉莫", "帝斯顿", "荣都" };

        for (int m = 0; m < mapFiles.Length; m++)
        {
            using (Bitmap bmp = new Bitmap(mapFiles[m]))
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
                        int offset = y * data.Stride + x * 4;
                        byte pb = pixels[offset];
                        byte pg = pixels[offset + 1];
                        byte pr = pixels[offset + 2];

                        if (pr > 200 && pg < 100 && pb < 100)
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
                                            if (pixels[noff + 2] > 200 && pixels[noff + 1] < 100 && pixels[noff] < 100)
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

                Console.WriteLine("{0} ({1}x{2}): {3} dots", mapNames[m], w, h, dots.Count);
                Console.Write("Dots = new int[,] {");
                for (int i = 0; i < dots.Count; i++)
                {
                    if (i % 5 == 0) Console.Write("\n    ");
                    Console.Write("{{{0},{1}}}", dots[i].X, dots[i].Y);
                    if (i < dots.Count - 1) Console.Write(", ");
                }
                Console.WriteLine("\n};\n");
            }
        }
    }
}

class MyPoint { public int X, Y; public MyPoint(int x, int y) { X = x; Y = y; } }
