using System.Diagnostics;
using System.IO;
using ManagedXZ;

namespace Tests
{
    public class DecompressTest
    {
        private bool TestFile(string xzFilename, string binFilename)
        {
            var tmpfile = Path.GetTempFileName();
            File.Delete(tmpfile);
            XZUtils.DecompressFile(xzFilename, tmpfile);
            var same = Utils.CompareContent(binFilename, tmpfile);
            return same;
        }

        public void ZeroBytes()
        {
            Trace.Assert(TestFile(@"Files\0byte.bin.xz", @"Files\0byte.bin"));
        }

        public void OneBytes0()
        {
            Trace.Assert(TestFile(@"Files\1byte.0.bin.xz", @"Files\1byte.0.bin"));
        }

        public void OneBytes1()
        {
            Trace.Assert(TestFile(@"Files\1byte.1.bin.xz", @"Files\1byte.1.bin"));
        }

        private bool TestInMemory(byte[] input, string binFilename)
        {
            var data1 = XZUtils.DecompressBytes(input, 0, input.Length);
            var data2 = File.ReadAllBytes(binFilename);
            return Utils.SequenceEqual(data1, data2);
        }

        public void ZeroByteInMemory()
        {
            Trace.Assert(TestInMemory(XZUtils.CompressBytes(new byte[0], 0, 0), "Files\\0byte.bin"));
        }

        public void OneByteInMemory0()
        {
            Trace.Assert(TestInMemory(XZUtils.CompressBytes(new byte[1] {0}, 0, 1), "Files\\1byte.0.bin"));
        }

        public void OneByteInMemory1()
        {
            Trace.Assert(TestInMemory(XZUtils.CompressBytes(new byte[1] {1}, 0, 1), "Files\\1byte.1.bin"));
        }
    }
}