using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.Win32.SafeHandles;

namespace UWP_Testing.ChunkTransferOperation
{
    // Original code credit:
    // https://github.com/Tyrrrz/YoutubeExplode
    // If you a warning tell me !
    public class TransferOperation
    {
        private readonly HttpClient _client;
        private long _fileSize = 0L;
        public TransferOperation()
        {
            _client = new HttpClient();
        }

        public class ChunkStream : IDisposable
        {
            public Uri Uri { get; set; }
            public MultiStream MultiStream { get; set; }
            public Stream Stream { get; set; }
            private readonly SafeHandle _handle = new SafeFileHandle(IntPtr.Zero, true);
            public void Dispose()
            {
                _handle.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        public class TransferOperationProgress
        {
            public long ReceiveBytes { get; set; }
            public long TotalBytes { get; set; }
        }
        public async Task CreateDownload(Uri uri, StorageFile outputFile, CancellationToken cancellationToken, IProgress<TransferOperationProgress> progress, long chunkSize = 10_485_760)
        {
            _fileSize = await GetContentLengthAsync(uri.AbsoluteUri) ?? 0;
            if (_fileSize == 0)
            {
                throw new Exception("File has no any content !");
            }
            using (var mediaStream = await GetMediaStreamAsync(uri, chunkSize))
            {
                var input = mediaStream.Stream ?? mediaStream.MultiStream;
                using (var output = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await CopyToAsync(input, output.AsStream(), progress, cancellationToken, fileSize: _fileSize);
                }
            }
        }
        private async Task CopyToAsync(Stream source, Stream destination, IProgress<TransferOperationProgress> progress, CancellationToken cancellationToken = default(CancellationToken), int bufferSize = 81920, long fileSize = 0)
        {
            var buffer = new byte[bufferSize];
            var totalBytesCopied = 0L;
            int bytesCopied;
            do
            {
                bytesCopied = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                await destination.WriteAsync(buffer, 0, bytesCopied, cancellationToken);
                totalBytesCopied += bytesCopied;
                progress.Report(new TransferOperationProgress { ReceiveBytes = totalBytesCopied, TotalBytes = fileSize });
            } while (bytesCopied > 0);
        }
        private async Task<ChunkStream> GetMediaStreamAsync(Uri uri, long chunkSize)
        {
            var url = uri.ToString();
            if (_fileSize > chunkSize)
            {
                var segmentCount = (int)Math.Ceiling(1.0 * _fileSize / chunkSize);
                var resolvers = new List<Func<Task<Stream>>>();
                for (var i = 0; i < segmentCount; i++)
                {
                    var from = i * chunkSize;
                    var to = (i + 1) * chunkSize - 1;
                    var resolver = new Func<Task<Stream>>(() => GetStreamAsync(_client, url, from, to));
                    resolvers.Add(resolver);
                }
                var stream = new MultiStream(resolvers);
                return new ChunkStream { Uri = uri, MultiStream = stream };
            }
            else
            {
                var stream = await GetStreamAsync(_client, url, 0);
                return new ChunkStream { Uri = uri, Stream = stream };
            }
        }
        private async Task<Stream> GetStreamAsync(HttpClient client, string requestUri, long? from = null, long? to = null, bool ensureSuccess = true)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Range = new RangeHeaderValue(from, to);
            using (request)
            {
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (ensureSuccess)
                    response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync();
            }
        }

        private async Task<long?> GetContentLengthAsync(string requestUri, bool ensureSuccess = true)
        {
            using (var response = await HeadAsync(_client, requestUri))
            {
                if (ensureSuccess)
                    response.EnsureSuccessStatusCode();
                return response.Content.Headers.ContentLength;
            }
        }

        private async Task<HttpResponseMessage> HeadAsync(HttpClient client, string requestUri)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Head, requestUri))
                return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        }
    }
}
