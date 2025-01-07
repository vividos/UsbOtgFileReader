using Android.App;
using Android.OS;
using Android.Webkit;
using Android.Widget;
using Com.Github.Mjdev.Libaums.FS;
using Java.IO;
using System;
using System.Diagnostics.CodeAnalysis;
using Xamarin.Essentials;

#pragma warning disable CS0618 // Type or member is obsolete

namespace UsbOtgFileReader.Tasks
{
    /// <summary>
    /// Asynchronous task to copy a file from USB to the Android device
    /// </summary>
    internal class CopyFromUsbToDeviceTask : AsyncTask<CopyFromUsbToDeviceParams, int, object>
    {
        /// <summary>
        /// Parameters for copying file from USB device to the Android device
        /// </summary>
        private CopyFromUsbToDeviceParams? copyParams;

        /// <summary>
        /// Progress dialog to show progress while downloading
        /// </summary>
        private ProgressDialog? dialog;

        /// <summary>
        /// Called before executing the task; shows a progress dialog
        /// </summary>
        protected override void OnPreExecute()
        {
            Activity context = Platform.CurrentActivity;

            string? message = context.Resources?.GetString(Resource.String.progress_copy_file_message);

            this.dialog = new ProgressDialog(context);
            this.dialog.SetTitle(Resource.String.progress_copy_file_title);
            this.dialog.SetMessage(message);
            this.dialog.Indeterminate = false;
            this.dialog.SetProgressStyle(ProgressDialogStyle.Horizontal);
            this.dialog.SetCancelable(false);

            this.dialog.Show();

            base.OnPreExecute();
        }

        /// <summary>
        /// Runs task in background
        /// </summary>
        /// <param name="values">params values</param>
        /// <returns>result object</returns>
        protected override object RunInBackground(params CopyFromUsbToDeviceParams[] values)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            this.copyParams = values[0];

            try
            {
                if (this.copyParams?.From == null ||
                    this.copyParams?.To == null)
                {
                    throw new ArgumentNullException("From or To copyParams is null");
                }

                var inputStream = new UsbFileInputStream(this.copyParams.From);
                var outputStream = new FileOutputStream(this.copyParams.To);

                byte[] bytes = new byte[this.copyParams.ChunkSize];
                int count = 0;
                long total = 0;

                System.Diagnostics.Debug.WriteLine(
                    $"Copy file with length: {this.copyParams.From.Length}");

                while ((count = inputStream.Read(bytes)) != -1)
                {
                    outputStream.Write(bytes, 0, count);
                    total += count;

                    int progress = (int)total;
                    if (this.copyParams.From.Length > int.MaxValue)
                    {
                        progress = (int)(total / 1024);
                    }

                    this.PublishProgress(progress);
                }

                outputStream.Close();
                inputStream.Close();
            }
            catch (Java.Lang.Exception ex)
            {
                Activity context = Platform.CurrentActivity;

                string? message = context.Resources?.GetString(
                    Resource.String.usb_device_error_transferring_s,
                    ex.Message ?? "???");

                MainThread.BeginInvokeOnMainThread(
                    () =>
                    {
                        Toast.MakeText(context, message, ToastLength.Short)?.Show();
                    });
            }

            stopwatch.Stop();
            System.Diagnostics.Debug.WriteLine(
                $"copying file took {(int)stopwatch.ElapsedMilliseconds} ms");

            return null;
        }

        /// <summary>
        /// Updates progress by updating the progress dialog
        /// </summary>
        /// <param name="values">progress values</param>
        protected override void OnProgressUpdate(params int[] values)
        {
            if (this.copyParams?.From == null ||
                this.dialog == null)
            {
                return;
            }

            long max = this.copyParams.From.Length;
            if (this.copyParams.From.Length > int.MaxValue)
            {
                max = this.copyParams.From.Length / 1024;
            }

            this.dialog.Max = (int)max;
            this.dialog.Progress = values[0];

            base.OnProgressUpdate(values);
        }

        /// <summary>
        /// Called after executing the task
        /// </summary>
        /// <param name="result">task result; unused</param>
        protected override void OnPostExecute([AllowNull] object result)
        {
            this.dialog?.Dismiss();

            if (this.copyParams?.To != null)
            {
                var file = new File(this.copyParams.To.AbsolutePath);
                RegisterFileInDownloadManager(file);
            }

            base.OnPostExecute(result);
        }

        /// <summary>
        /// Registers a file in the download manager
        /// </summary>
        /// <param name="file">file to register</param>
        private static void RegisterFileInDownloadManager(File file)
        {
            string? extension = MimeTypeMap.GetFileExtensionFromUrl(file.AbsolutePath);

            string mimeType = MimeTypeMap.Singleton?.GetMimeTypeFromExtension(extension) ?? "application/octet-stream";

            var context = Application.Context;

            string? message = context.Resources?.GetString(
                Resource.String.transfer_download_file_s,
                file.Name);

            var downloadManager = DownloadManager.FromContext(context);
            downloadManager?.AddCompletedDownload(
                file.Name,
                message,
                true,
                mimeType,
                file.AbsolutePath,
                file.Length(),
                true);
        }
    }
}
