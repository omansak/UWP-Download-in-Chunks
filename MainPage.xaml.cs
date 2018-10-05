using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Win32.SafeHandles;
using UWP_Testing.ChunkTransferOperation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWP_Testing
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Status.Text = "Starting...";
                Uri uri = (new Uri("https://www.sample-videos.com/video/mp4/720/big_buck_bunny_720p_30mb.mp4"));
                Windows.Storage.Pickers.FolderPicker folderPicker = new Windows.Storage.Pickers.FolderPicker();
                folderPicker.FileTypeFilter.Add("*");
                var folder = await folderPicker.PickSingleFolderAsync();
                var destinationFile = await folder.CreateFileAsync("sample.mp4", CreationCollisionOption.ReplaceExisting);
                await new TransferOperation().CreateDownload(
                    uri,
                    destinationFile,
                    new CancellationToken(),
                    new Progress<TransferOperation.TransferOperationProgress>(DownloadProgress),
                    chunkSize: 10_485_760
                );
                Status.Text = "Complete !";
            }
            catch (Exception ex)
            {
                Status.Text = ex.Message;
            }
        }
        private void DownloadProgress(TransferOperation.TransferOperationProgress progress)
        {
            if (progress.TotalBytes > 0)
            {
                long percent = (progress.ReceiveBytes * 100 / progress.TotalBytes);
                string text = string.Format(" ( % {0} ) {1} / {2} MB ", percent, (progress.ReceiveBytes / (double)(1024 * 1024)).ToString("N"), (progress.TotalBytes / (double)(1024 * 1024)).ToString("N"));
                ProgressText.Text = text;
                ProgressBar.Value = percent;
                Status.Text = "Downloading...";
            }
        }

    }
}
