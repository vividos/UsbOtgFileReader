using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Github.Mjdev.Libaums;
using System;

namespace UsbOtgFileReader
{
    /// <summary>
    /// List view adapter for USB mass storage devices list
    /// </summary>
    internal class UsbDeviceListAdapter : BaseAdapter
    {
        /// <summary>
        /// View holder for USB device list items
        /// </summary>
        internal class UsbDeviceItemViewHolder : Java.Lang.Object
        {
            /// <summary>
            /// USB device
            /// </summary>
            public UsbMassStorageDevice? Device { get; internal set; }

            /// <summary>
            /// Device name text view
            /// </summary>
            public TextView? DeviceName { get; set; }
        }

        /// <summary>
        /// Activity context
        /// </summary>
        private readonly Context context;

        /// <summary>
        /// List of USB devices to select from
        /// </summary>
        private readonly UsbMassStorageDevice[] devices;

        /// <summary>
        /// Creates a new list view item adapter for USB mass storage devices
        /// </summary>
        /// <param name="context">activity context</param>
        /// <param name="devices">USB devices list</param>
        public UsbDeviceListAdapter(
            Context context,
            UsbMassStorageDevice[] devices)
        {
            this.context = context;
            this.devices = devices;
        }

        /// <summary>
        /// Returns device list item count
        /// </summary>
        public override int Count => this.devices.Length;

        /// <summary>
        /// Returns the list item object for given position index
        /// </summary>
        /// <param name="position">position index</param>
        /// <returns>list item object</returns>
        public override Java.Lang.Object GetItem(int position)
        {
            return this.devices[position];
        }

        /// <summary>
        /// Converts a position index to an item ID
        /// </summary>
        /// <param name="position">position index</param>
        /// <returns>item ID</returns>
        public override long GetItemId(int position)
        {
            return position;
        }

        /// <summary>
        /// Called to get (or recycle) a view for the list item on given position
        /// </summary>
        /// <param name="position">list item position</param>
        /// <param name="convertView">view to recycle, or null</param>
        /// <param name="parent">parent view group</param>
        /// <returns>view for list item</returns>
        public override View GetView(int position, View? convertView, ViewGroup? parent)
        {
            View? view = convertView;
            UsbDeviceItemViewHolder? holder = null;

            UsbMassStorageDevice device = this.devices[position];

            if (view != null)
            {
                holder = view.Tag as UsbDeviceItemViewHolder;
            }
            else
            {
                LayoutInflater? inflater = this.context
                    .GetSystemService(Context.LayoutInflaterService)
                    .JavaCast<LayoutInflater>();

                view = inflater?.Inflate(Resource.Layout.UsbDeviceItem, parent, false);

                if (view == null)
                {
                    throw new InvalidOperationException("couldn't inflate view");
                }
            }

            if (holder == null)
            {
                holder = new UsbDeviceItemViewHolder
                {
                    DeviceName = view.FindViewById<TextView>(Resource.Id.usbDeviceName)
                };

                view.Tag = holder;
            }

            if (holder.DeviceName != null)
            {
                holder.DeviceName.Text = device.UsbDevice.ProductName;
            }

            holder.Device = device;

            return view;
        }
    }
}
