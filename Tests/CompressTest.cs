using System.IO;
using System.Linq;
using ManagedXZ;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class CompressTest
    {
        private bool TestFile(string srcFilename, string xzFilename)
        {
            var tmpfile = Path.GetTempFileName();
            XZUtils.CompressFile(srcFilename, tmpfile, FileMode.Append);
            var same = FileUtils.CompareContent(xzFilename, tmpfile);
            File.Delete(tmpfile);
            return same;
        }

        [TestMethod]
        public void ZeroByteFile()
        {
            Assert.IsTrue(TestFile("Files\\0byte.bin", "Files\\0byte.bin.xz"));
        }

        [TestMethod]
        public void OneByteFile0()
        {
            Assert.IsTrue(TestFile("Files\\1byte.0.bin", "Files\\1byte.0.bin.xz"));
        }

        [TestMethod]
        public void OneByteFile1()
        {
            Assert.IsTrue(TestFile("Files\\1byte.1.bin", "Files\\1byte.1.bin.xz"));
        }

        private bool TestInMemory(byte[] input, string xzFilename)
        {
            var data1 = XZUtils.CompressBytes(input, 0, input.Length);
            var data2 = File.ReadAllBytes(xzFilename);
            return data1.SequenceEqual(data2);
        }

        [TestMethod]
        public void ZeroByteInMemory()
        {
            Assert.IsTrue(TestInMemory(new byte[0], "Files\\0byte.bin.xz"));
        }

        [TestMethod]
        public void OneByteInMemory0()
        {
            Assert.IsTrue(TestInMemory(new byte[1] {0}, "Files\\1byte.0.bin.xz"));
        }

        [TestMethod]
        public void OneByteInMemory1()
        {
            Assert.IsTrue(TestInMemory(new byte[1] {1}, "Files\\1byte.1.bin.xz"));
        }
    }
}