// Copyright (c) 2019-2022 Gui.H. https://github.com/FastTunnel/FastTunnel
// The FastTunnel licenses this file to you under the Apache License Version 2.0.
// For more details,You may obtain License file at: https://github.com/FastTunnel/FastTunnel/blob/v2/LICENSE

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FastTunnel.Core.Forwarder
{
    public class TranStream : Stream
    {
        private readonly Stream readStream;
        private readonly Stream wirteStream;

        public TranStream(HttpContext context)
        {
            readStream = context.Request.BodyReader.AsStream();
            wirteStream = context.Response.BodyWriter.AsStream();
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            wirteStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return wirteStream.FlushAsync(cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return readStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            wirteStream.Write(buffer, offset, count);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return readStream.ReadAsync(buffer, cancellationToken);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var len = await readStream.ReadAsync(buffer, offset, count, cancellationToken);
            if (len == 0) { Console.WriteLine("==========ReadAsync END=========="); }
            return len;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            wirteStream.Write(buffer);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return wirteStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await wirteStream.WriteAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            Console.WriteLine("========Dispose=========");
        }

        public override ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public override void Close()
        {
            Console.WriteLine("========Close=========");
            base.Close();
        }
    }
}
