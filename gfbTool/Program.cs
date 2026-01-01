using System;
using gfbTool;

class Program
{
    static void Main(string[] args)
    {
        foreach (var arg in args)
        {
            var gfb = new gfbFile();
            if (Path.GetExtension(arg).ToLower() == ".gfb")
            {
                var bmpData = gfb.gfb2bmp(arg);
                var filename = Path.GetFileNameWithoutExtension(arg) + ".bmp";
                File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(arg)!, filename), bmpData);
                Console.WriteLine($"Converted {filename}");
            }
            else if (Path.GetExtension(arg).ToLower() == ".bmp")
            {
                var filename = Path.GetFileNameWithoutExtension(arg) + ".gfb";
                gfb.bmp2gfb(arg, Path.Combine(Path.GetDirectoryName(arg)!, filename));
                Console.WriteLine($"Pack {filename}");
            }
        }

        Console.WriteLine($"Convert Finish......");
        Console.ReadKey();
    }
}