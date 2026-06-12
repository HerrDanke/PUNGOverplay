using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

class ExportCoords
{
    struct DotInfo { public int X, Y, Type; }

    static void Main()
    {
        string imgDir = @"E:\飞牛同步\Workspace\Bitfun\img";
        string outDir = @"E:\飞牛同步\Workspace\Bitfun\坐标";
        Directory.CreateDirectory(outDir);

        // 7 maps as separate calls
        Export(imgDir, outDir, "艾伦格", "艾伦格.png", false);
        Export(imgDir, outDir, "米拉玛", "米拉玛.png", false);
        Export(imgDir, outDir, "泰戈",   "泰戈.png",   false);
        ExportVikendi(imgDir, outDir);
        Export(imgDir, outDir, "帕拉莫", "帕拉莫.png", false);
        Export(imgDir, outDir, "帝斯顿", "帝斯顿.png", false);
        Export(imgDir, outDir, "荣都",   "荣都.png",   false);

        // Description file
        File.WriteAllText(Path.Combine(outDir, "说明.txt"),
            "坐标文件说明\n" +
            "───────────\n" +
            "每行格式: 序号. (X坐标, Y坐标) [类型]\n" +
            "类型: 红-密室, 紫-撬棍房, 绿-熊洞（仅维寒迪有颜色区分）\n" +
            "坐标是原始图片像素坐标，程序会自动根据屏幕分辨率缩放\n",
            Encoding.UTF8);

        Console.WriteLine("Done! All files written to: " + outDir);
    }

    static void Export(string imgDir, string outDir, string name, string file, bool unused)
    {
        string imgPath = Path.Combine(imgDir, file);
        var dots = ScanImage(imgPath);
        WriteFile(Path.Combine(outDir, name + ".txt"), name, dots);
        Console.WriteLine("{0}: {1} dots", name, dots.Count);
    }

    static void ExportVikendi(string imgDir, string outDir)
    {
        // 密室 (type 0)
        var red = ScanImage(Path.Combine(imgDir, "维寒迪密室.png"));
        // 撬棍房 (type 1)
        var purple = ScanImage(Path.Combine(imgDir, "维寒迪撬棍房.png"));
        // 熊洞 (type 2)
        var green = ScanImage(Path.Combine(imgDir, "维寒迪熊洞.png"));

        var all = new List<DotInfo>();
        foreach (var d in red)    all.Add(new DotInfo { X = d.X, Y = d.Y, Type = 0 });
        foreach (var d in purple) all.Add(new DotInfo { X = d.X, Y = d.Y, Type = 1 });
        foreach (var d in green)  all.Add(new DotInfo { X = d.X, Y = d.Y, Type = 2 });
        all.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));

        WriteFile(Path.Combine(outDir, "维寒迪.txt"), "维寒迪", all);
        Console.WriteLine("维寒迪: 密室{0}+撬棍房{1}+熊洞{2}={3} dots",
            red.Count, purple.Count, green.Count, all.Count);
    }

    static List<DotInfo> ScanImage(string path)
    {
        var dots = new List<DotInfo>();
        using (Bitmap bmp = new Bitmap(path))
        {
            int w = bmp.Width, h = bmp.Height;
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] pixels = new byte[data.Stride * h];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            bmp.UnlockBits(data);

            bool[,] visited = new bool[w, h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (visited[x, y]) continue;
                    int off = y * data.Stride + x * 4;
                    if (pixels[off + 2] > 180 && pixels[off + 1] < 130 && pixels[off] < 130)
                    {
                        var cluster = new List<MyPoint>();
                        var queue = new Queue<MyPoint>();
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
                                        if (pixels[noff + 2] > 180 && pixels[noff + 1] < 130 && pixels[noff] < 130)
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
                            dots.Add(new DotInfo { X = sx / cluster.Count, Y = sy / cluster.Count, Type = 0 });
                        }
                    }
                    else visited[x, y] = true;
                }
            }
        }
        dots.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
        return dots;
    }

    static void WriteFile(string path, string title, List<DotInfo> dots)
    {
        var sb = new StringBuilder();
        sb.AppendLine(title);
        sb.AppendLine(new string('─', title.Length));
        sb.AppendLine();

        if (dots.Count == 0)
            sb.AppendLine("（无标记点）");
        else
        {
            string[] typeColors = { "红", "紫", "绿" };
            string[] typeNames = { "密室", "撬棍房", "熊洞" };

            for (int i = 0; i < dots.Count; i++)
            {
                string typeInfo = "";
                if (dots[i].Type > 0)
                    typeInfo = string.Format(" [{0}-{1}]", typeColors[dots[i].Type], typeNames[dots[i].Type]);
                sb.AppendLine(string.Format("{0,2}. ({1,4}, {2,4}){3}",
                    i + 1, dots[i].X, dots[i].Y, typeInfo));
            }
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    class MyPoint { public int X, Y; public MyPoint(int x, int y) { X = x; Y = y; } }
}
