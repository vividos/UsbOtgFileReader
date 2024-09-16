using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware.Usb;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Github.Mjdev.Libaums;
using Com.Github.Mjdev.Libaums.FS;
using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using UsbOtgFileReader.Tasks;
using Xamarin.Essentials;

namespace UsbOtgFileReader
{
    /// <summary>
    /// Activity that shows the file system of a connected USB device
    /// </summary>
    [Activity(Label = "@string/file_system_title", Theme = "@style/AppTheme")]
    public class UsbFileSystemActivity : Activity
    {
        /// <summary>
        /// Extras parameter to pass the USB device to connect to
        /// </summary>
        private const string ExtraUsbDevice = "EXTRA_USB_DEVICE";

        /// <summary>
        /// Storage device to connect to
        /// </summary>
        private UsbMassStorageDevice? storageDevice;

        /// <summary>
        /// Current file system interface
        /// </summary>
        private IFileSystem? currentFileSystem;

        /// <summary>
        /// List view with current list of folders and files
        /// </summary>
        private ListView? usbFolderAndFileList;

        /// <summary>
        /// Current directory to show
        /// </summary>
        private IUsbFile? currentDirectory;

        /// <summary>
        /// Starts file system activity
        /// </summary>
        /// <param name="context">context to use for start</param>
        /// <param name="device">USB mass storage device to open</param>
        public static void Start(Context context, UsbMassStorageDevice device)
        {
            var intent = new Intent(context, typeof(UsbFileSystemActivity));
            intent.PutExtra(ExtraUsbDevice, device.UsbDevice);
            context.StartActivity(intent);
        }

        /// <summary>
        /// Called when the activity is about to be created
        /// </summary>
        /// <param name="savedInstanceState">saved instance state; unused</param>
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Platform.Init(this, savedInstanceState);

            if (this.SetupUsbDevice())
            {
                this.InitLayout();
            }
        }

        /// <summary>
        /// Sets up USB mass storage device to display file system for
        /// </summary>
        /// <returns>true when setting up USB device was successful</returns>
        private bool SetupUsbDevice()
        {
            var item = this.Intent?.GetParcelableExtra(ExtraUsbDevice);

            if (item is not UsbDevice usbDevice)
            {
                Toast.MakeText(this, Resource.String.usb_device_null, ToastLength.Short)?.Show();
                this.Finish();
                return false;
            }

            var usbManager = this.GetSystemService(Context.UsbService) as UsbManager;
            bool hasPermision = usbManager?.HasPermission(usbDevice) ?? false;

            if (!hasPermision)
            {
                Toast.MakeText(
                    this,
                    Resource.String.usb_permission_not_granted,
                    ToastLength.Short)?.Show();

                this.Finish();
                return false;
            }

            this.storageDevice =
                UsbMassStorageDevice.GetMassStorageDevices(usbDevice, this).FirstOrDefault();

            if (this.storageDevice == null)
            {
                Toast.MakeText(
                    this,
                    Resource.String.usb_device_error_no_mass_storage,
                    ToastLength.Short)?.Show();

                this.Finish();
                return false;
            }

            try
            {
                // before interacting with a device you need to call Init()!
                this.storageDevice.Init();
            }
            catch (Java.IO.IOException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Error while UsbMassStorageDevice.Init() call: " + ex.ToString());

                Toast.MakeText(
                    this,
                    Resource.String.usb_device_error_opening,
                    ToastLength.Short)?.Show();
                this.Finish();

                this.storageDevice = null;

                return false;
            }

            // Only uses the first partition on the device
            this.currentFileSystem = this.storageDevice.Partitions?.FirstOrDefault()?.FileSystem;

            if (this.currentFileSystem == null)
            {
                Toast.MakeText(
                    this,
                    Resource.String.usb_device_error_no_partitions_or_filesystems,
                    ToastLength.Short)?.Show();

                this.Finish();

                this.storageDevice = null;

                return false;
            }

            System.Diagnostics.Debug.WriteLine(
                $"Capacity: {this.currentFileSystem.Capacity}, " +
                $"Occupied Space: {this.currentFileSystem.OccupiedSpace}, " +
                $"Free Space: {this.currentFileSystem.FreeSpace}, " +
                $"Chunk size: {this.currentFileSystem.ChunkSize}");

            this.currentDirectory = this.currentFileSystem.RootDirectory;

            return true;
        }

        /// <summary>
        /// Initializes activity layout
        /// </summary>
        private void InitLayout()
        {
            this.SetContentView(Resource.Layout.UsbFileSystemActivity);

            Toolbar? toolbar = this.FindViewById<Toolbar>(Resource.Id.toolbar);
            this.SetActionBar(toolbar);

            if (this.ActionBar != null)
            {
                this.ActionBar.SetTitle(Resource.String.app_name);
                this.ActionBar.SetDisplayHomeAsUpEnabled(true);
                this.ActionBar.SetHomeButtonEnabled(true);
            }

            this.usbFolderAndFileList = this.FindViewById<ListView>(Resource.Id.usbFolderAndFileList);

            if (this.usbFolderAndFileList != null &&
                this.currentDirectory != null)
            {
                this.usbFolderAndFileList.ItemClick += this.OnItemClick_UsbFolderAndFileList;

                this.usbFolderAndFileList.Adapter =
                    new UsbFolderAndFileListAdapter(this, this.currentDirectory);
            }
        }

        /// <summary>
        /// Called when the user clicked on a folder or file list item
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="args">event args</param>
        private void OnItemClick_UsbFolderAndFileList(object? sender, AdapterView.ItemClickEventArgs args)
        {
            if (this.usbFolderAndFileList == null ||
                this.usbFolderAndFileList.Adapter == null)
            {
                return;
            }

            if (this.usbFolderAndFileList.Adapter.GetItem(args.Position)
                is not IUsbFile folderOrFile)
            {
                return;
            }

            if (folderOrFile.IsDirectory)
            {
                // navigate to this folder
                try
                {
                    this.usbFolderAndFileList.Adapter = new UsbFolderAndFileListAdapter(this, folderOrFile);
                    this.currentDirectory = folderOrFile;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Error while changing to subfolder: " + ex.ToString());

                    Toast.MakeText(
                        this,
                        Resource.String.usb_device_error_change_folder,
                        ToastLength.Short)?.Show();
                    this.Finish();
                }
            }
            else
            {
                // show a context menu to download the file
                var menu = new PopupMenu(this, args.View, GravityFlags.Center);
                menu.Inflate(Resource.Menu.contextmenu);
                menu.MenuItemClick += (s, a) => this.OnPopupMenuItemClick(a?.Item, folderOrFile);
                menu.Show();
            }
        }

        /// <summary>
        /// Called when a menu item in the popup menu has been clicked
        /// </summary>
        /// <param name="item">menu item</param>
        /// <param name="file">USB file</param>
        private async void OnPopupMenuItemClick(IMenuItem? item, IUsbFile file)
        {
            if (item != null &&
                item.ItemId == Resource.Id.fileItemDownload)
            {
                await this.StartFileDownload(file);
            }
        }

        /// <summary>
        /// Starts downloading file to device
        /// </summary>
        /// <param name="usbFile">USB file to download</param>
        /// <returns>task to wait on</returns>
        private async Task StartFileDownload(IUsbFile usbFile)
        {
            PermissionStatus status =
                await Permissions.RequestAsync<Permissions.StorageWrite>();

            if (status != PermissionStatus.Granted)
            {
                Toast.MakeText(
                    this,
                    Resource.String.download_missing_permission_write_storage,
                    ToastLength.Short)?.Show();
            }

#pragma warning disable CS0618 // Type or member is obsolete
            Java.IO.File? downloadsFolder =
                Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
#pragma warning restore CS0618 // Type or member is obsolete

            if (downloadsFolder == null ||
                this.currentFileSystem == null)
            {
                return;
            }

            var copyParams = new CopyFromUsbToDeviceParams
            {
                From = usbFile,
                To = new Java.IO.File(downloadsFolder, usbFile.Name),
                ChunkSize = this.currentFileSystem.ChunkSize,
            };

            var task = new CopyFromUsbToDeviceTask();
            task.Execute(copyParams);
        }

        /// <summary>
        /// Called when an item in the options menu has been selected.
        /// </summary>
        /// <param name="item">selected item</param>
        /// <returns>true when item selection was handled, false when not</returns>
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home)
            {
                if (!this.NavigateBack())
                {
                    this.Finish();
                }

                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        /// <summary>
        /// Called when the hardware back button has been pressed
        /// </summary>
        public override void OnBackPressed()
        {
            if (!this.NavigateBack())
            {
                base.OnBackPressed();
            }
        }

        /// <summary>
        /// Navigates back from the current directory to the parent, if not already in root.
        /// </summary>
        /// <returns>true when navigated back to parent, or false when already at root</returns>
        private bool NavigateBack()
        {
            if (this.currentDirectory == null ||
                this.currentDirectory.IsRoot ||
                this.currentDirectory.Parent == null ||
                this.usbFolderAndFileList == null)
            {
                return false;
            }

            this.usbFolderAndFileList.Adapter =
                new UsbFolderAndFileListAdapter(this, this.currentDirectory.Parent);

            this.currentDirectory = this.currentDirectory.Parent;

            return true;
        }

        /// <summary>
        /// Called when permission request has results
        /// </summary>
        /// <param name="requestCode">request code</param>
        /// <param name="permissions">list of requested permissions</param>
        /// <param name="grantResults">list of grant results</param>
        [SupportedOSPlatform("android23.0")]
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        /// <summary>
        /// Called when the activity is about to be destroyed
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();

            this.currentFileSystem = null;
            this.currentDirectory = null;
            if (this.storageDevice != null)
            {
                this.storageDevice.Close();
                this.storageDevice = null;
            }
        }
    }
}
