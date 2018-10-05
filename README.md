# UWP-Download-in-Chunks

It offers to save large files from bandwidth limited servers such as Google Drive, Yandex Disk,YouTube etc.

## Features

Read Async
Write Async
Download File In Chunks Async
UWP / C#

## Usage

` new TransferOperation().CreateDownload(Uri uri*,StorageFile file*,CancellationToken token*, IProgress<double> progress*,              chunkSize: 10_485_760)`

## Progess
`            if (progress.TotalBytes > 0)
            {
                long percent = (progress.ReceiveBytes * 100 / progress.TotalBytes);
                string text = string.Format(" ( % {0} ) {1} / {2} MB ", percent, (progress.ReceiveBytes / (double)(1024 * 1024)).ToString("N"), (progress.TotalBytes / (double)(1024 * 1024)).ToString("N"));
            }`
