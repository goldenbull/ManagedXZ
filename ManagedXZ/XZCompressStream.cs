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

        public XZCompressStream(Stream stream, int threads) : this(stream, threads, 1024*256)
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
            _outbuf = Marshal.AllocHGlobal(_bufferSize);
            _lzma_stream.next_in = _inbuf;
            _lzma_stream.next_out = _outbuf;
            _lzma_stream.avail_out = (UIntPtr)_bufferSize;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            // compress all remaining data
            Native.lzma_end(_lzma_stream);
            Marshal.FreeHGlobal(_inbuf);
            Marshal.FreeHGlobal(_outbuf);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }
}