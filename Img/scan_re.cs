using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

class ScanRe
{
    static void Main()
    {
        using (Bitmap bmp = new Bitmap(@"E:\飞牛同步\Workspace\Bitfun\img\re.png"))
        {
            int w = bmp.Width, h = bmp.Height;
            Console.WriteLine("Image: {0}x{1}", w, h);

            // LockBits for fast pixel access
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] pixels = new byte[data.Stride * h];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            bmp.UnlockBits(data);

            bool[,] visited = new bool[w, h];
            List<Point> dots = new List<Point>();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (visited[x, y]) continue;
                    int offset = y * data.Stride + x * 4;
                    byte b = pixels[offset];
                    byte g = pixels[offset + 1];
                    byte r = pixels[offset + 2];

                    if (r > 200 && g < 100 && b < 100)
                    {
                        // BFS cluster finder
                        List<Point> cluster = new List<Point>();
                        Queue<Point> queue = new Queue<Point>();
                        queue.Enqueue(new Point(x, y));
                        visited[x, y] = true;

                        while (queue.Count > 0)
                        {
                            Point p = queue.Dequeue();
                            cluster.Add(p);

                            for (int dy = -1; dy <= 1; dy++)
                            {
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    int nx = p.X + dx;
                                    int ny = p.Y + dy;
                                    if (nx >= 0 && nx < w && ny >= 0 && ny < h && !visited[nx, ny])
                                    {
                                        int noff = ny * data.Stride + nx * 4;
                                        if (pixels[noff + 2] > 200 && pixels[noff + 1] < 100 && pixels[noff] < 100)
                                        {
                                            visited[nx, ny] = true;
                                            queue.Enqueue(new Point(nx, ny));
                                        }
                                    }
                                }
                            }
                        }

                        if (cluster.Count > 5)
                        {
                            int sumX = 0, sumY = 0;
                            foreach (Point p in cluster) { sumX += p.X; sumY += p.Y; }
                            int cx = sumX / cluster.Count;
                            int cy = sumY / cluster.Count;
                            dots.Add(new Point(cx, cy));
                        }
                    }
                    else
                    {
                        visited[x, y] = true;
                    }
                }
            }

            Console.WriteLine("\nTotal dots found: {0}", dots.Count);
            if (dots.Count > 0)
            {
                Console.WriteLine("\nC# array format:");
                Console.Write("Dots = new int[,] {\n");
                for (int i = 0; i < dots.Count; i++)
                {
                    string comma = (i < dots.Count - 1) ? "," : "";
                    Console.Write("    {{{0}, {1}}}{2}\n", dots[i].X, dots[i].Y, comma);
                }
                Console.WriteLine("}");
            }
        }
    }
}
