using System.IO;
using System.Linq;
using ManagedXZ;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class DecompressTest
    {
        private bool TestFile(string xzFilename, string binFilename)
        {
            var tmpfile = Path.GetTempFileName();
            File.Delete(tmpfile);
            XZUtils.DecompressFile(xzFilename, tmpfile);
            var same = FileUtils.CompareContent(binFilename, tmpfile);
            return same;
        }

        [TestMethod]
        public void ZeroBytes()
        {
            Assert.IsTrue(TestFile(@"Files\0byte.bin.xz", @"Files\0byte.bin"));
        }

        [TestMethod]
        public void OneBytes0()
        {
            Assert.IsTrue(TestFile(@"Files\1byte.0.bin.xz", @"Files\1byte.0.bin"));
        }

        [TestMethod]
        public void OneBytes1()
        {
            Assert.IsTrue(TestFile(@"Files\1byte.1.bin.xz", @"Files\1byte.1.bin"));
        }

        private bool TestInMemory(byte[] input, string binFilename)
        {
            var data1 = XZUtils.DecompressBytes(input, 0, input.Length);
            var data2 = File.ReadAllBytes(binFilename);
            return data1.SequenceEqual(data2);
        }

        [TestMethod]
        public void ZeroByteInMemory()
        {
            Assert.IsTrue(TestInMemory(XZUtils.CompressBytes(new byte[0], 0, 0), "Files\\0byte.bin"));
        }

        [TestMethod]
        public void OneByteInMemory0()
        {
            Assert.IsTrue(TestInMemory(XZUtils.CompressBytes(new byte[1] {0}, 0, 1), "Files\\1byte.0.bin"));
        }

        [TestMethod]
        public void OneByteInMemory1()
        {
            Assert.IsTrue(TestInMemory(XZUtils.CompressBytes(new byte[1] {1}, 0, 1), "Files\\1byte.1.bin"));
        }
    }
}