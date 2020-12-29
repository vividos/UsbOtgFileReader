using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Com.Github.Mjdev.Libaums;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Distribute;
using System;

namespace UsbOtgFileReader
{
    /// <summary>
    /// Activity that shows a list of connected USB devices and lets the user select a single
    /// device.
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    ////[IntentFilter(new[] { UsbManager.ActionUsbDeviceAttached })]
    ////[MetaData(UsbManager.ActionUsbDeviceAttached, Resource = "@xml/device_filter")]
    public partial class UsbDeviceListActivity : Activity
    {
        /// <summary>
        /// AppCenter key for the Android version
        /// </summary>
        private const string AppCenterAndroidKey = "8b3d8c7b-6168-4e26-b16c-e4e7eddfe6f2";

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
            this.ActionBar.SetIcon(Resource.Drawable.usb);
            this.ActionBar.SetHomeButtonEnabled(true);

            Button startUsbScan = this.FindViewById<Button>(Resource.Id.startUsbScan);
            startUsbScan.Click += this.OnClick_ScanButton;

            this.usbDeviceList = this.FindViewById<ListView>(Resource.Id.usbDeviceList);

            this.usbDeviceList.ItemClick += this.OnItemClick_UsbDeviceList;
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
            var storageDevice = this.usbDeviceList.Adapter.GetItem(args.Position) as UsbMassStorageDevice;
            if (storageDevice != null)
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
                Toast.MakeText(this, "Error while scanning: " + ex.Message, ToastLength.Long).Show();
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

            Toast.MakeText(this, $"Found {devicesList.Length} USB devices", ToastLength.Long).Show();
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
        /// Called when permission request has results
        /// </summary>
        /// <param name="requestCode">request code</param>
        /// <param name="permissions">list of requested permissions</param>
        /// <param name="grantResults">list of grant results</param>
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
