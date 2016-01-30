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
        private static readonly Random rnd = new Random();

        private static void Main(string[] args)
        {
            Compress_SingleStream("test1.txt.xz", 1);
            Compress_SingleStream("test1.txt.xz", 4);
            Decompress("test1.txt.xz");

            Compress_MultiStream("test2.txt.xz", 1);
            Compress_MultiStream("test2.txt.xz", 4);
            Decompress("test2.txt.xz");

            PerfCompare();

            CompressInMemory();
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

        private static void Decompress(string filename)
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

        private static void PerfCompare()
        {
            // generate a big file
            var filename = "data.txt";
            using (var writer = new StreamWriter(filename))
            {
                for (int i = 0; i < 1000000; i++)
                    writer.WriteLine($"{i}: generate random numbers {rnd.NextDouble()}, {rnd.NextDouble()}, {rnd.NextDouble()}");
            }

            for (int i = 0; i <= 9; i++)
            {
                var t = Stopwatch.StartNew();
                XZUtils.CompressFile(filename, $@"{filename}.L{i}.xz", 6, i);
                Console.WriteLine($"compress level={i}, time={t.Elapsed}");
            }
            for (int i = 0; i <= 9; i++)
            {
                var t = Stopwatch.StartNew();
                XZUtils.DecompressFile($@"{filename}.L{i}.xz", $@"restore_L{i}_{filename}");
                Console.WriteLine($"decompress level={i}, time={t.Elapsed}");
            }
        }

        private static void CompressInMemory()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 1000; i++)
                sb.AppendLine($"{i}: a random number {rnd.NextDouble()}");
            var str = sb.ToString();
            var bytes = Encoding.UTF8.GetBytes(str);
            var compressed = XZUtils.CompressBytes(bytes, 0, bytes.Length);
            var bytes2 = XZUtils.DecompressBytes(compressed, 0, compressed.Length);
            var str2 = Encoding.UTF8.GetString(bytes2);
            Console.WriteLine($"identical = {str == str2}");
        }
    }
}