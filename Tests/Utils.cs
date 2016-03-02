using System;
using System.Collections.Generic;
using System.IO;

namespace Tests
{
    internal static class Utils
    {
        public static bool SequenceEqual<T>(IList<T> list1, IList<T> list2)
        {
            return false;
        }

        public static bool CompareContent(string file1, string file2)
        {
            try
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
                        var cnt1 = fs1.Read(buffer1, 0, SIZE);
                        var cnt2 = fs2.Read(buffer2, 0, SIZE);
                        if (cnt1 != cnt2) throw new Exception("impossible!");
                        for (int i = 0; i < cnt1; i++)
                        {
                            if (buffer1[i] != buffer2[i])
                                return false;
                        }

                        if (cnt1 < SIZE) break;
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}