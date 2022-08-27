using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Github.Mjdev.Libaums;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Distribute;
using System;
using System.Runtime.Versioning;

[assembly: UsesFeature("android.hardware.usb.host", Required = true)]

namespace UsbOtgFileReader
{
    /// <summary>
    /// Activity that shows a list of connected USB devices and lets the user select a single
    /// device.
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    [IntentFilter(new[] { UsbManager.ActionUsbDeviceAttached })]
    [MetaData(UsbManager.ActionUsbDeviceAttached, Resource = "@xml/device_filter")]
    public partial class UsbDeviceListActivity : Activity
    {
        /// <summary>
        /// AppCenter key for the Android version
        /// </summary>
        private const string AppCenterAndroidKey = "8b3d8c7b-6168-4e26-b16c-e4e7eddfe6f2";

        /// <summary>
        /// GitHub project URL
        /// </summary>
        private const string GitHubProjectUrl = "https://github.com/vividos/UsbOtgFileReader/";

        /// <summary>
        /// USB manager instance
        /// </summary>
        private UsbManager usbManager;

        /// <summary>
        /// Broadcast receiver used to
        /// </summary>
        private UsbBroadcastReceiver receiver;

        /// <summary>
        /// USB device list
        /// </summary>
        private ListView usbDeviceList;

        /// <summary>
        /// Called when the activity is about to be created
        /// </summary>
        /// <param name="savedInstanceState">saved instance state; unused</param>
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            AppCenter.Start(
                AppCenterAndroidKey,
                typeof(Distribute),
                typeof(Crashes));

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            this.InitUsbManager();
            this.InitLayout();
        }

        /// <summary>
        /// Initializes USB manager
        /// </summary>
        private void InitUsbManager()
        {
            this.usbManager = this.GetSystemService(Context.UsbService) as UsbManager;

            this.receiver = new UsbBroadcastReceiver();
            this.receiver.Register(this);
        }

        /// <summary>
        /// Initializes activity layout
        /// </summary>
        private void InitLayout()
        {
            this.SetContentView(Resource.Layout.UsbDeviceListActivity);

            Toolbar toolbar = this.FindViewById<Toolbar>(Resource.Id.toolbar);
            this.SetActionBar(toolbar);
            this.ActionBar.SetTitle(Resource.String.app_name);
            this.ActionBar.SetIcon(Resource.Drawable.usb_and_folder_outline);
            this.ActionBar.SetHomeButtonEnabled(true);

            Button startUsbScan = this.FindViewById<Button>(Resource.Id.startUsbScan);
            startUsbScan.Click += this.OnClick_ScanButton;

            this.usbDeviceList = this.FindViewById<ListView>(Resource.Id.usbDeviceList);

            this.usbDeviceList.ItemClick += this.OnItemClick_UsbDeviceList;
        }

        /// <summary>
        /// Called when activity is about to be started; starts a device scan
        /// </summary>
        protected override void OnStart()
        {
            base.OnStart();

            // start initial scan
            this.StartScan();
        }

        /// <summary>
        /// Called when the user clicked on the scan button
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="args">event args</param>
        private void OnClick_ScanButton(object sender, EventArgs args)
        {
            this.StartScan();
        }

        /// <summary>
        /// Called when the user clicked on an USB device list item
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="args">event args</param>
        private void OnItemClick_UsbDeviceList(object sender, AdapterView.ItemClickEventArgs args)
        {
            var item = this.usbDeviceList.Adapter.GetItem(args.Position);
            if (item is UsbMassStorageDevice storageDevice)
            {
                this.ConnectDevice(storageDevice);
            }
        }

        /// <summary>
        /// Starts USB device scan
        /// </summary>
        private void StartScan()
        {
            UsbMassStorageDevice[] devicesList;
            try
            {
                devicesList = UsbMassStorageDevice.GetMassStorageDevices(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Error scanning for devices: " + ex.ToString());

                Toast.MakeText(
                    this,
                    Resource.String.usb_device_error_scanning,
                    ToastLength.Short).Show();
                return;
            }

            if (devicesList == null)
            {
                return;
            }

            this.usbDeviceList = this.FindViewById<ListView>(Resource.Id.usbDeviceList);

            this.usbDeviceList.Adapter = new UsbDeviceListAdapter(
                this,
                devicesList);

            string message = this.Resources.GetString(
                Resource.String.usb_devices_found_n,
                devicesList.Length);

            Toast.MakeText(this, message, ToastLength.Short).Show();
        }

        /// <summary>
        /// Connects to a USB device and opens the USB file system activity.
        /// </summary>
        /// <param name="device">device to open</param>
        private void ConnectDevice(UsbMassStorageDevice device)
        {
            bool hasPermision = this.usbManager.HasPermission(device.UsbDevice);

            if (hasPermision)
            {
                UsbFileSystemActivity.Start(this, device);
            }
            else
            {
                this.receiver.RequestPermission(this, this.usbManager, device.UsbDevice);
            }
        }

        /// <summary>
        /// Creates options menu for this activity
        /// </summary>
        /// <param name="menu">menu to add to</param>
        /// <returns>true when successful</returns>
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            this.MenuInflater.Inflate(Resource.Menu.optionsmenu, menu);
            return true;
        }

        /// <summary>
        /// Called when a menu item entry is selected
        /// </summary>
        /// <param name="item">selected menu item</param>
        /// <returns>true when menu item was handled</returns>
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.showInfo)
            {
                string message = this.Resources.GetString(
                    Resource.String.info_version_s,
                    Xamarin.Essentials.VersionTracking.CurrentVersion);

                var builder = new AlertDialog.Builder(this);
                builder
                    .SetTitle(Resource.String.app_name)
                    .SetIcon(Resource.Mipmap.icon)
                    .SetMessage(message)
                    .SetNeutralButton(
                        Resource.String.action_open_github_page,
                        (sender, args) => this.OpenGitHubProject())
                    .SetPositiveButton(Resource.String.action_close_dialog, listener: null);

                builder.Create().Show();
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        /// <summary>
        /// Opens GitHub project page
        /// </summary>
        private void OpenGitHubProject()
        {
            Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(
                async () =>
                {
                    await Xamarin.Essentials.Launcher.OpenAsync(GitHubProjectUrl);
                });
        }

        /// <summary>
        /// Called when permission request has results
        /// </summary>
        /// <param name="requestCode">request code</param>
        /// <param name="permissions">list of requested permissions</param>
        /// <param name="grantResults">list of grant results</param>
        [SupportedOSPlatform("android23.0")]
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        /// <summary>
        /// Called when the activity is about to be destroyed
        /// </summary>
        protected override void OnDestroy()
        {
            this.receiver?.Unregister(this);

            base.OnDestroy();
        }
    }
}
