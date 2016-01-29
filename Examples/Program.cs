using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using ManagedXZ;

namespace Examples
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Compress_SingleStream(1);
            Compress_SingleStream(4);
            Compress_MultiStream(1);
            Compress_MultiStream(4);
            DoDecompress();
        }

        private const int CNT = 10000;

        private static void Compress_SingleStream(int threads)
        {
            var timer = Stopwatch.StartNew();
            var fs = File.Create("test.txt.xz");
            var xz = new XZCompressStream(fs, threads);
            using (var writer = new StreamWriter(xz, Encoding.UTF8))
            {
                for (int i = 0; i < CNT; i++)
                {
                    writer.WriteLine($"this is line {i} written in {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff")}");
                    if (i%(CNT/10) == 0)
                        Console.WriteLine($"writing {i} lines, {timer.Elapsed}");
                }
            }
            Console.WriteLine($"finished, {timer.Elapsed}");
        }

        private static void Compress_MultiStream(int threads)
        {
            var timer = Stopwatch.StartNew();

            // create a new xz file
            var filename = "test.txt.xz";
            var fs = File.Create(filename);
            var xz = new XZCompressStream(fs, threads);
            using (var writer = new StreamWriter(xz, Encoding.UTF8))
            {
                for (int i = 0; i < CNT; i++)
                {
                    writer.WriteLine($"this is line {i} written in {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff")}");
                    if (i%(CNT/10) == 0)
                        Console.WriteLine($"writing {i} lines, {timer.Elapsed}");
                }
            }

            // open the same xz file and append new data
            fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.None);
            xz = new XZCompressStream(fs, threads);
            using (var writer = new StreamWriter(xz, Encoding.UTF8))
            {
                for (int i = 0; i < CNT; i++)
                {
                    writer.WriteLine($"DATA APPENDED: this is line {i} written in {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff")}");
                    if (i%(CNT/10) == 0)
                        Console.WriteLine($"writing {i} lines, {timer.Elapsed}");
                }
            }
            Console.WriteLine($"finished, {timer.Elapsed}");
        }

        private static void DoDecompress()
        {
        }
    }
}