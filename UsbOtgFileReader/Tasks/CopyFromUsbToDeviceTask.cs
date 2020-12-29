﻿using Android.App;
using Android.OS;
using Android.Webkit;
using Android.Widget;
using Com.Github.Mjdev.Libaums.FS;
using Java.IO;
using System;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CS0618 // Type or member is obsolete

namespace UsbOtgFileReader
{
    /// <summary>
    /// Asynchronous task to copy a file from USB to the Android device
    /// </summary>
    internal class CopyFromUsbToDeviceTask : AsyncTask<CopyFromUsbToDeviceParams, int, object>
    {
        /// <summary>
        /// Parameters for copying file from USB device to the Android device
        /// </summary>
        private CopyFromUsbToDeviceParams copyParams;

        /// <summary>
        /// Progress dialog to show progress while downloading
        /// </summary>
        private ProgressDialog dialog;

        /// <summary>
        /// Called before executing the task; shows a progress dialog
        /// </summary>
        [Obsolete]
        protected override void OnPreExecute()
        {
            Activity context = Xamarin.Essentials.Platform.CurrentActivity;

            this.dialog = new ProgressDialog(context);
            this.dialog.SetTitle("Copying file...");
            this.dialog.SetMessage("Copying file from USB device to internal storage; this can take a while...");
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
                var inputStream = new UsbFileInputStream(this.copyParams.From);
                var outputStream = new FileOutputStream(this.copyParams.To);

                byte[] bytes = new byte[this.copyParams.ChunkSize];
                int count = 0;
                long total = 0;

                System.Diagnostics.Debug.WriteLine($"Copy file with length: {this.copyParams.From.Length}");

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
            catch (IOException ex)
            {
                Activity context = Xamarin.Essentials.Platform.CurrentActivity;
                Toast.MakeText(context, $"Error while transferring: {ex.Message}", ToastLength.Long).Show();
            }

            stopwatch.Stop();
            System.Diagnostics.Debug.WriteLine($"copying file took {(int)stopwatch.ElapsedMilliseconds} ms");

            return null;
        }

        /// <summary>
        /// Updates progress by updating the progress dialog
        /// </summary>
        /// <param name="values">progress values</param>
        protected override void OnProgressUpdate(params int[] values)
        {
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
            this.dialog.Dismiss();

            var file = new File(this.copyParams.To.AbsolutePath);
            RegisterFileInDownloadManager(file);

            base.OnPostExecute(result);
        }

        /// <summary>
        /// Registers a file in the download manager
        /// </summary>
        /// <param name="file">file to register</param>
        private static void RegisterFileInDownloadManager(File file)
        {
            string extension = MimeTypeMap.GetFileExtensionFromUrl(file.AbsolutePath);

            string mimeType = MimeTypeMap.Singleton.GetMimeTypeFromExtension(extension) ?? "application/octet-stream";

            var downloadManager = DownloadManager.FromContext(Application.Context);
            downloadManager.AddCompletedDownload(
                file.Name,
                "Downloaded file " + file.Name,
                true,
                mimeType,
                file.AbsolutePath,
                file.Length(),
                true);
        }
    }
}