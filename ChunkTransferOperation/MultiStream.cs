using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UWP_Testing.ChunkTransferOperation
{    
	// Original code credit:
    // https://github.com/Tyrrrz/YoutubeExplode
    // If you a warning tell me !
    public class MultiStream : Stream
    {
        private readonly Queue<Func<Task<Stream>>> _queue;
        private Stream _currentStream;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public MultiStream(IEnumerable<Func<Task<Stream>>> streamTaskResolvers)
        {
            _queue = new Queue<Func<Task<Stream>>>(streamTaskResolvers);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_currentStream == null)
            {
                if (_queue.Any())
                    _currentStream = await _queue.Dequeue().Invoke();
                else
                    return 0;
            }
            var bytesRead = await _currentStream.ReadAsync(buffer, offset, count, cancellationToken);

            if (bytesRead == 0)
            {
                _currentStream.Dispose();
                _currentStream = null;
                bytesRead = await ReadAsync(buffer, offset, count, cancellationToken);
            }

            return bytesRead;
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            ReadAsync(buffer, offset, count).GetAwaiter().GetResult();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
                _currentStream?.Dispose();
        }

        #region Not supported

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        #endregion
    }
}
