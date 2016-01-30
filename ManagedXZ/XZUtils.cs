using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ManagedXZ
{
    public static class XZUtils
    {
        #region helper functions

        internal static lzma_ret CreateEncoder(lzma_stream stream, int threads, uint preset)
        {
            // adjust thread numbers
            if (threads > Environment.ProcessorCount)
            {
                Trace.TraceWarning("it's not reasonable to have more threads than processors");
                threads = Environment.ProcessorCount;
            }

            if (threads == 1)
            {
                // single thread compress
                return Native.lzma_easy_encoder(stream, preset, lzma_check.LZMA_CHECK_CRC64);
            }
            else
            {
                // multi thread compress
                var mt = new lzma_mt
                         {
                             threads = (uint)threads,
                             check = lzma_check.LZMA_CHECK_CRC64,
                             preset = preset,
                         };
                return Native.lzma_stream_encoder_mt(stream, mt);
            }
        }

        /// <summary>
        /// liblzma has provided lzma_stream_buffer_encode and lzma_stream_buffer_decode, but here I re-invent the wheel again...
        /// </summary>
        /// <param name="_lzma_stream"></param>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private static byte[] CodeBuffer(lzma_stream _lzma_stream, byte[] data, int offset, int count)
        {
            const int BUFSIZE = 4096;
            var outStream = new MemoryStream(BUFSIZE);
            var inbuf = Marshal.AllocHGlobal(BUFSIZE);
            var outbuf = Marshal.AllocHGlobal(BUFSIZE);
            try
            {
                var action = lzma_action.LZMA_RUN;
                _lzma_stream.next_in = IntPtr.Zero;
                _lzma_stream.avail_in = UIntPtr.Zero;
                _lzma_stream.next_out = outbuf;
                _lzma_stream.avail_out = (UIntPtr)BUFSIZE;
                int read_pos = offset;
                while (true)
                {
                    if (_lzma_stream.avail_in == UIntPtr.Zero && read_pos < offset + count)
                    {
                        int bytesToProcess = Math.Min(BUFSIZE, offset + count - read_pos);
                        _lzma_stream.next_in = inbuf;
                        _lzma_stream.avail_in = (UIntPtr)bytesToProcess;
                        Marshal.Copy(data, read_pos, inbuf, bytesToProcess);
                        read_pos += bytesToProcess;
                        Trace.Assert(read_pos <= offset + count);
                        if (read_pos == offset + count)
                            action = lzma_action.LZMA_FINISH;
                    }

                    var ret = Native.lzma_code(_lzma_stream, action);
                    if (_lzma_stream.avail_out == UIntPtr.Zero || ret == lzma_ret.LZMA_STREAM_END)
                    {
                        int write_size = BUFSIZE - (int)(uint)_lzma_stream.avail_out;
                        var tmp = new byte[write_size];
                        Marshal.Copy(outbuf, tmp, 0, write_size);
                        outStream.Write(tmp, 0, write_size);
                        _lzma_stream.next_out = outbuf;
                        _lzma_stream.avail_out = (UIntPtr)BUFSIZE;
                    }

                    if (ret != lzma_ret.LZMA_OK)
                    {
                        if (ret == lzma_ret.LZMA_STREAM_END)
                            break;

                        throw new Exception($"lzma_code returns {ret}");
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(inbuf);
                Marshal.FreeHGlobal(outbuf);
            }

            return outStream.ToArray();
        }

        #endregion

        /// <summary>
        /// Compress data in-memory
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="threads"></param>
        /// <param name="level">0-9, default is 6, bigger number needs more time and produces smaller compressed data</param>
        /// <returns></returns>
        public static byte[] CompressBytes(byte[] data, int offset, int count, int threads = 1, int level = 6)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset >= data.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (count + offset > data.Length) throw new ArgumentOutOfRangeException(nameof(count), "offset+count > data.length");
            if (level < 0 || level > 9) throw new ArgumentOutOfRangeException(nameof(level));
            if (count == 0) return new byte[0];

            var _lzma_stream = new lzma_stream();
            var ret = CreateEncoder(_lzma_stream, threads, (uint)level);
            if (ret != lzma_ret.LZMA_OK)
                throw new Exception($"Can not create lzma stream: {ret}");

            return CodeBuffer(_lzma_stream, data, offset, count);
        }

        public static byte[] DecompressBytes(byte[] data, int offset, int count)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset >= data.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (count + offset > data.Length) throw new ArgumentOutOfRangeException(nameof(count), "offset+count > data.length");
            if (count == 0) return new byte[0];

            var _lzma_stream = new lzma_stream();
            var ret = Native.lzma_auto_decoder(_lzma_stream, ulong.MaxValue, Native.LZMA_CONCATENATED);
            if (ret != lzma_ret.LZMA_OK)
                throw new Exception($"Can not create lzma stream: {ret}");

            return CodeBuffer(_lzma_stream, data, offset, count);
        }

        public static void CompressFile(string inFile, string outFile, int threads = 1, int level = 6)
        {
            var buffer = new byte[1 << 20];
            using (var ins = new FileStream(inFile, FileMode.Open))
            using (var outs = new XZCompressStream(outFile, threads, level))
            {
                while (true)
                {
                    var cnt = ins.Read(buffer, 0, buffer.Length);
                    outs.Write(buffer, 0, cnt);
                    if (cnt < buffer.Length)
                        break;
                }
            }
        }

        public static void DecompressFile(string inFile, string outFile)
        {
            var buffer = new byte[1 << 20];
            using (var ins = new XZDecompressStream(inFile))
            using (var outs = new FileStream(outFile, FileMode.CreateNew))
            {
                while (true)
                {
                    var cnt = ins.Read(buffer, 0, buffer.Length);
                    outs.Write(buffer, 0, cnt);
                    if (cnt < buffer.Length)
                        break;
                }
            }
        }
    }
}