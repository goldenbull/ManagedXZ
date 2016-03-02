using System.Diagnostics;
using System.IO;
using ManagedXZ;

namespace Tests
{
    public class CompressTest
    {
        private bool TestFile(string srcFilename, string xzFilename)
        {
            var tmpfile = Path.GetTempFileName();
            XZUtils.CompressFile(srcFilename, tmpfile, FileMode.Append);
            var same = Utils.CompareContent(xzFilename, tmpfile);
            File.Delete(tmpfile);
            return same;
        }

        public void ZeroByteFile()
        {
            Trace.Assert(TestFile("Files\\0byte.bin", "Files\\0byte.bin.xz"));
        }

        public void OneByteFile0()
        {
            Trace.Assert(TestFile("Files\\1byte.0.bin", "Files\\1byte.0.bin.xz"));
        }

        public void OneByteFile1()
        {
            Trace.Assert(TestFile("Files\\1byte.1.bin", "Files\\1byte.1.bin.xz"));
        }

        private bool TestInMemory(byte[] input, string xzFilename)
        {
            var data1 = XZUtils.CompressBytes(input, 0, input.Length);
            var data2 = File.ReadAllBytes(xzFilename);
            return Utils.SequenceEqual(data1, data2);
        }

        public void ZeroByteInMemory()
        {
            Trace.Assert(TestInMemory(new byte[0], "Files\\0byte.bin.xz"));
        }

        public void OneByteInMemory0()
        {
            Trace.Assert(TestInMemory(new byte[1] {0}, "Files\\1byte.0.bin.xz"));
        }

        public void OneByteInMemory1()
        {
            Trace.Assert(TestInMemory(new byte[1] {1}, "Files\\1byte.1.bin.xz"));
        }
    }
}