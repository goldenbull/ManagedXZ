using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using ManagedXZ;

namespace Examples
{
    internal class Program
    {
        private const int CNT = 100;

        private static void Main(string[] args)
        {
            Compress_SingleStream("test1.txt.xz", 1);
            Compress_SingleStream("test1.txt.xz", 4);
            Compress_MultiStream("test2.txt.xz", 1);
            Compress_MultiStream("test2.txt.xz", 4);
            DoDecompress("test1.txt.xz");
            DoDecompress("test2.txt.xz");
            RatioCompare();
        }

        private static void RatioCompare()
        {
            for (int i = 0; i <= 9; i++)
                CompressFile(@"D:\test\data.bin", $@"D:\test\data{i}.bin.xz", i);
        }

        private static void CompressFile(string src, string dst, int level)
        {
            var t = Stopwatch.StartNew();
            var buffer = new byte[1 << 20];
            using (var ins = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 24))
            using (var outs = new XZCompressStream(dst, 6, level))
            {
                while (true)
                {
                    var cnt = ins.Read(buffer, 0, buffer.Length);
                    outs.Write(buffer, 0, cnt);
                    if (cnt < buffer.Length)
                        break;
                }
            }
            Console.WriteLine($"level={level}, time={t.Elapsed}");
        }

        private static void Compress_SingleStream(string filename, int threads)
        {
            var timer = Stopwatch.StartNew();
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
            Console.WriteLine($"finished, {timer.Elapsed}");
        }

        private static void Compress_MultiStream(string filename, int threads)
        {
            var timer = Stopwatch.StartNew();

            // create a new xz file
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
            using (var writer = new StreamWriter(xz, new UTF8Encoding(false, true))) // append data should go without BOM
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

        private static void DoDecompress(string filename)
        {
            using (var reader = new StreamReader(new XZDecompressStream(filename)))
            {
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null) break;

                    Console.WriteLine($"read from {filename}: {line}");
                }
            }
        }
    }
}