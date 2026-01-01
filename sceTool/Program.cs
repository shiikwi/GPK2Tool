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
        }

        Console.WriteLine($"Convert Finish......");
        Console.ReadKey();
    }
}