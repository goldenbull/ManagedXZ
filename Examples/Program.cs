using System;
using System.IO;
using System.Text;
using ManagedXZ;

namespace Examples
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            DoCompress();
            DoDecompress();
        }

        private static void DoCompress()
        {
            var fs = File.Create("test.txt.xz");
            var xz = new XZCompressStream(fs, 4, 1 << 20);
            using (var writer = new StreamWriter(xz, Encoding.UTF8))
            {
                for (int i = 0; i < 100000; i++)
                {
                    writer.WriteLine($"this is line {i} written in {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff")}");
                    if (i%1000 == 0)
                        Console.WriteLine($"writing {i} lines");
                }
            }
        }

        private static void DoDecompress()
        {
        }
    }
}