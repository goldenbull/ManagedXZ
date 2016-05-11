using System;
using System.IO;
using ManagedXZ;

namespace Tests
{
    internal class Program
    {
        private void Check(bool condition, string message)
        {
            if (condition)
                Console.WriteLine($"[passed] {message}");
            else
            {
                Console.WriteLine($"[failed] {message}");
                Console.ReadLine();
            }
        }

        private bool CompareBytes(byte[] arr1, byte[] arr2)
        {
            if (arr1.Length != arr2.Length) return false;
            for (int i = 0; i < arr1.Length; i++)
                if (arr1[i] != arr2[i]) return false;
            return true;
        }

        private bool CompareFile(string file1, string file2)
        {
            var f1 = new FileInfo(file1);
            var f2 = new FileInfo(file2);
            if (f1.Length != f2.Length) return false;

            using (var fs1 = f1.OpenRead())
            using (var fs2 = f2.OpenRead())
            {
                const int SIZE = 1024*1024;
                var buffer1 = new byte[SIZE];
                var buffer2 = new byte[SIZE];
                while (true)
                {
                    var cnt = fs1.Read(buffer1, 0, SIZE);
                    fs2.Read(buffer2, 0, SIZE);
                    if (!CompareBytes(buffer1, buffer2)) return false;
                    if (cnt < SIZE) break;
                }
                return true;
            }
        }

        private bool TestCompressFile(string srcFilename, string xzFilename)
        {
            var tmpfile = Path.GetTempFileName();
            XZUtils.CompressFile(srcFilename, tmpfile, FileMode.Append);
            var isSame = CompareFile(xzFilename, tmpfile);
            File.Delete(tmpfile);
            return isSame;
        }

        private bool TestCompressInMemory(byte[] input, string xzFilename)
        {
            var data1 = XZUtils.CompressBytes(input, 0, input.Length);
            var data2 = File.ReadAllBytes(xzFilename);
            return CompareBytes(data1, data2);
        }


        private bool TestDecompressFile(string xzFilename, string binFilename)
        {
            var tmpfile = Path.GetTempFileName();
            File.Delete(tmpfile);
            XZUtils.DecompressFile(xzFilename, tmpfile);
            var isSame = CompareFile(binFilename, tmpfile);
            File.Delete(tmpfile);
            return isSame;
        }

        private bool TestDecompressInMemory(byte[] input, string binFilename)
        {
            var data1 = XZUtils.DecompressBytes(input, 0, input.Length);
            var data2 = File.ReadAllBytes(binFilename);
            return CompareBytes(data1, data2);
        }

        private void TestDispose()
        {
            var c = new XZCompressStream("temp1.xz");
            c.Close();
            c.Close();

            c = new XZCompressStream("temp2.xz");
            c.Dispose();
            c.Dispose();

            var d = new XZDecompressStream("temp1.xz");
            d.Close();
            d.Close();

            d = new XZDecompressStream("temp2.xz");
            d.Dispose();
            d.Dispose();
        }

        private void RunTests()
        {
            Check(TestCompressFile("0byte.bin", "0byte.bin.xz"), "compress 0byte");
            Check(TestCompressFile("1byte.0.bin", "1byte.0.bin.xz"), "compress 1byte[0x00]");
            Check(TestCompressFile("1byte.1.bin", "1byte.1.bin.xz"), "compress 1byte[0x01]");
            Check(TestCompressInMemory(new byte[0], "0byte.bin.xz"), "compress 0byte in memory");
            Check(TestCompressInMemory(new byte[1] {0}, "1byte.0.bin.xz"), "compress 1byte[0x00] in memory");
            Check(TestCompressInMemory(new byte[1] {1}, "1byte.1.bin.xz"), "compress 1byte[0x00] in memory");

            Check(TestDecompressFile("0byte.bin.xz", "0byte.bin"), "decompress 0byte");
            Check(TestDecompressFile("1byte.0.bin.xz", "1byte.0.bin"), "decompress 1byte[0x00]");
            Check(TestDecompressFile("1byte.1.bin.xz", "1byte.1.bin"), "decompress 1byte[0x01]");
            Check(TestDecompressInMemory(XZUtils.CompressBytes(new byte[0], 0, 0), "0byte.bin"), "decompress 0byte in memory");
            Check(TestDecompressInMemory(XZUtils.CompressBytes(new byte[1] {0}, 0, 1), "1byte.0.bin"), "decompress 1byte[0x00] in memory");
            Check(TestDecompressInMemory(XZUtils.CompressBytes(new byte[1] {1}, 0, 1), "1byte.1.bin"), "decompress 1byte[0x00] in memory");

            TestDispose();

            Console.WriteLine("test finished");
            Console.ReadLine();
        }

        private static void Main(string[] args)
        {
            new Program().RunTests();
        }
    }
}