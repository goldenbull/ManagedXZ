using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ManagedXZ
{
    public class XZCompressStream : Stream
    {
        public XZCompressStream(Stream stream) : this(stream, 1)
        {
        }

        public XZCompressStream(Stream stream, int threads) : this(stream, threads, 1024*64)
        {
        }

        public XZCompressStream(Stream stream, int threads, int bufferSize)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite) throw new ArgumentException("stream is not writable");
            if (threads <= 0) throw new ArgumentOutOfRangeException(nameof(threads));
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));

            // adjust thread numbers
            if (threads > Environment.ProcessorCount)
            {
                Trace.TraceWarning("it's not reasonable to have more threads than processors");
                threads = Environment.ProcessorCount;
            }

            _stream = stream;
            _threads = threads;
            _bufferSize = bufferSize;
            Init();
        }

        private readonly Stream _stream;
        private readonly int _threads;
        private readonly int _bufferSize;
        private readonly lzma_stream _lzma_stream = new lzma_stream();
        private IntPtr _inbuf;
        private IntPtr _outbuf;
        private const int OUTBUFSIZE = 1024*64; // we do not need a big outbuf

        private void Init()
        {
            uint preset = 6; // default, TODO

            lzma_ret ret;
            if (_threads == 1)
            {
                // single thread compress
                ret = Native.lzma_easy_encoder(_lzma_stream, preset, lzma_check.LZMA_CHECK_CRC64);
            }
            else
            {
                // multi thread compress
                var mt = new lzma_mt
                         {
                             threads = (uint)_threads,
                             check = lzma_check.LZMA_CHECK_CRC64,
                             preset = preset
                         };
                var p = GCHandle.Alloc(mt, GCHandleType.Pinned);
                ret = Native.lzma_stream_encoder_mt(_lzma_stream, mt);
                p.Free();
            }

            if (ret != lzma_ret.LZMA_OK)
                throw new Exception($"Can not create lzma stream: {ret}");

            _inbuf = Marshal.AllocHGlobal(_bufferSize);
            _outbuf = Marshal.AllocHGlobal(OUTBUFSIZE);

            // init lzma_stream
            _lzma_stream.next_in = _inbuf;
            _lzma_stream.next_out = _outbuf;
            _lzma_stream.avail_in = UIntPtr.Zero;
            _lzma_stream.avail_out = (UIntPtr)OUTBUFSIZE;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

        /// <summary>
        /// learn from 01_compress_easy.c
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset >= buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (count + offset > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count), "offset+count>buffer.length");
            if (count == 0) return;

            var pin = GCHandle.Alloc(_lzma_stream, GCHandleType.Pinned);
            try
            {
                int bytesProcessed = 0;
                while (true)
                {
                    // Fill the input buffer if it is empty.
                    if (_lzma_stream.avail_in == UIntPtr.Zero)
                    {
                        int bytesToProcess = Math.Min(count - bytesProcessed, _bufferSize);
                        if (bytesToProcess == 0) break; // no more data to compress
                        _lzma_stream.next_in = _inbuf;
                        _lzma_stream.avail_in = (UIntPtr)bytesToProcess;
                        Marshal.Copy(buffer, offset + bytesProcessed, _inbuf, bytesToProcess);
                        bytesProcessed += bytesToProcess;
                    }

                    // do compress, RUN action should return LZMA_OK on success
                    var ret = Native.lzma_code(_lzma_stream, lzma_action.LZMA_RUN);
                    if (ret != lzma_ret.LZMA_OK)
                        throw new Exception($"lzma_code returns {ret}");

                    // check output buffer
                    if (_lzma_stream.avail_out == UIntPtr.Zero)
                    {
                        byte[] data = new byte[OUTBUFSIZE];
                        Marshal.Copy(_outbuf, data, 0, data.Length);
                        _stream.Write(data, 0, data.Length);

                        // Reset next_out and avail_out.
                        _lzma_stream.next_out = _outbuf;
                        _lzma_stream.avail_out = (UIntPtr)OUTBUFSIZE;
                    }
                }
            }
            finally
            {
                pin.Free();
            }
        }

        protected override void Dispose(bool disposing)
        {
            // compress all remaining data
            var pin = GCHandle.Alloc(_lzma_stream, GCHandleType.Pinned);
            try
            {
                while (true)
                {
                    // do compress, LZMA_FINISH action should return LZMA_OK or LZMA_STREAM_END on success
                    var ret = Native.lzma_code(_lzma_stream, lzma_action.LZMA_FINISH);
                    if (ret != lzma_ret.LZMA_STREAM_END && ret != lzma_ret.LZMA_OK)
                        throw new Exception($"lzma_code returns {ret}");

                    // write output buffer to underlying stream
                    if (_lzma_stream.avail_out == UIntPtr.Zero || ret == lzma_ret.LZMA_STREAM_END)
                    {
                        byte[] data = new byte[OUTBUFSIZE - (uint)_lzma_stream.avail_out];
                        Marshal.Copy(_outbuf, data, 0, data.Length);
                        _stream.Write(data, 0, data.Length);

                        // Reset next_out and avail_out.
                        _lzma_stream.next_out = _outbuf;
                        _lzma_stream.avail_out = (UIntPtr)OUTBUFSIZE;
                    }

                    if (ret == lzma_ret.LZMA_STREAM_END)
                        break;
                }
            }
            finally
            {
                pin.Free();
            }

            Native.lzma_end(_lzma_stream);
            Marshal.FreeHGlobal(_inbuf);
            Marshal.FreeHGlobal(_outbuf);
            _stream.Close();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }
}