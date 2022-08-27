using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Android.Widget;
using Com.Github.Mjdev.Libaums;
using System.Linq;

namespace UsbOtgFileReader
{
    /// <summary>
    /// Broadcast receiver for USB related broadcasts
    /// </summary>
    public class UsbBroadcastReceiver : BroadcastReceiver
    {
        /// <summary>
        /// USB permission action text
        /// </summary>
        private static readonly string ActionUsbPermission = Xamarin.Essentials.AppInfo.PackageName + ".USB_PERMISSION";

        /// <summary>
        /// Registers broadcast receiver
        /// </summary>
        /// <param name="context">context to use</param>
        public void Register(Context context)
        {
            var filter = new IntentFilter();

            filter.AddAction(ActionUsbPermission);
            filter.AddAction(UsbManager.ActionUsbDeviceAttached);

            context.RegisterReceiver(this, filter);
        }

        /// <summary>
        /// Requests permission for given USB device
        /// </summary>
        /// <param name="context">context to use</param>
        /// <param name="usbManager">USB manager to use</param>
        /// <param name="usbDevice">USB device to request</param>
        internal void RequestPermission(Context context, UsbManager usbManager, UsbDevice usbDevice)
        {
            var permissionIntent = PendingIntent.GetBroadcast(
                context,
                0,
                new Intent(ActionUsbPermission),
                PendingIntentFlags.Immutable);

            usbManager.RequestPermission(usbDevice, permissionIntent);
        }

        /// <summary>
        /// Unregisters broadcast receiver
        /// </summary>
        /// <param name="context">context to use</param>
        public void Unregister(Context context)
        {
            context.UnregisterReceiver(this);
        }

        /// <summary>
        /// Called when a new broadcast is received
        /// </summary>
        /// <param name="context">content object</param>
        /// <param name="intent">intent object</param>
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context == null || intent == null)
            {
                return;
            }

            if (intent.Action == ActionUsbPermission)
            {
                lock (this)
                {
                    HandleUsbPermissionAction(context, intent);
                }
            }
            else if (intent.Action == UsbManager.ActionUsbDeviceAttached)
            {
                lock (this)
                {
                    HandleUsbDeviceAttachedAction(context, intent);
                }
            }
        }

        /// <summary>
        /// Handles received USB permission action
        /// </summary>
        /// <param name="context">context object</param>
        /// <param name="intent">intent object</param>
        private static void HandleUsbPermissionAction(Context context, Intent intent)
        {
            var usbDevice = (UsbDevice?)intent.GetParcelableExtra(UsbManager.ExtraDevice);

            if (intent.GetBooleanExtra(UsbManager.ExtraPermissionGranted, false))
            {
                if (usbDevice != null)
                {
                    UsbMassStorageDevice? storageDevice =
                        UsbMassStorageDevice.GetMassStorageDevices(usbDevice, context)
                        .FirstOrDefault();

                    if (storageDevice != null)
                    {
                        UsbFileSystemActivity.Start(context, storageDevice);
                    }
                }
            }
            else
            {
                Toast.MakeText(
                    context,
                    Resource.String.usb_permission_not_granted,
                    ToastLength.Short)?.Show();
            }
        }

        /// <summary>
        /// Handles USB device attached action
        /// </summary>
        /// <param name="context">context object</param>
        /// <param name="intent">intent object</param>
        private static void HandleUsbDeviceAttachedAction(Context context, Intent intent)
        {
            var usbDevice = (UsbDevice?)intent.GetParcelableExtra(UsbManager.ExtraDevice);

            if (usbDevice != null)
            {
                UsbMassStorageDevice? storageDevice =
                    UsbMassStorageDevice.GetMassStorageDevices(usbDevice, context)
                    .FirstOrDefault();

                if (storageDevice != null)
                {
                    UsbFileSystemActivity.Start(context, storageDevice);
                }
            }
        }
    }
}
