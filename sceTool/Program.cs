using System;
using System.Text;
using sceTool;

class Program
{
    static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        foreach (var arg in args)
        {
            var scb = new scbFile();
            if (Path.GetExtension(arg).ToLower() == ".scb")
            {
                var filename = Path.GetFileNameWithoutExtension(arg) + ".txt";
                var outpath = Path.Combine(Path.GetDirectoryName(arg)!, filename);
                scb.ExportScb(arg, outpath);
                Console.WriteLine($"Exported {filename}");
            }
            else if (Path.GetExtension(arg).ToLower() == ".txt")
            {
                var scbpath = Path.ChangeExtension(arg, ".scb");
                var sf0path = Path.ChangeExtension(arg, ".sf0");
                scb.ImportScb(arg, scbpath, sf0path);
                Console.WriteLine($"Imported {Path.GetFileName(scbpath)}");
            }
        }

        Console.WriteLine($"Convert Finish......");
        Console.ReadKey();
    }
}