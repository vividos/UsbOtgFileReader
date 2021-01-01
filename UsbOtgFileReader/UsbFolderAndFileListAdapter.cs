using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Github.Mjdev.Libaums.FS;
using System.Collections.Generic;
using System.Linq;

namespace UsbOtgFileReader
{
    /// <summary>
    /// List view adapter for folders and files list
    /// </summary>
    internal class UsbFolderAndFileListAdapter : BaseAdapter
    {
        /// <summary>
        /// View holder for folders and files list items
        /// </summary>
        internal class UsbFileAndFolderListAdapterViewHolder : Java.Lang.Object
        {
            /// <summary>
            /// Folder or file image view
            /// </summary>
            public ImageView Image { get; set; }

            /// <summary>
            /// Folder or file name text view
            /// </summary>
            public TextView Name { get; set; }
        }

        /// <summary>
        /// Activity context
        /// </summary>
        private readonly Context context;

        /// <summary>
        /// List of folder and file items
        /// </summary>
        private readonly List<IUsbFile> folderAndFileList;

        /// <summary>
        /// Indicates if the first item is the ".." parent folder
        /// </summary>
        private readonly bool firstItemIsParent;

        /// <summary>
        /// Creates a new list view item adapter for folders and files
        /// </summary>
        /// <param name="context">activity context</param>
        /// <param name="directory">directory to enumerate folders and files</param>
        public UsbFolderAndFileListAdapter(Context context, IUsbFile directory)
        {
            System.Diagnostics.Debug.Assert(
                directory.IsDirectory,
                "must be a directory");

            this.context = context;
            this.folderAndFileList = directory.ListFiles().ToList();

            // add parent for back navigation
            if (!directory.IsRoot)
            {
                this.folderAndFileList.Insert(0, directory.Parent);
                this.firstItemIsParent = true;
            }
        }

        /// <summary>
        /// Returns folders and files list item count
        /// </summary>
        public override int Count => this.folderAndFileList.Count;

        /// <summary>
        /// Returns the list item object for given position index
        /// </summary>
        /// <param name="position">position index</param>
        /// <returns>list item object</returns>
        public override Java.Lang.Object GetItem(int position)
        {
            return (Java.Lang.Object)this.folderAndFileList[position];
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
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;
            UsbFileAndFolderListAdapterViewHolder holder = null;

            if (view != null)
            {
                holder = view.Tag as UsbFileAndFolderListAdapterViewHolder;
            }

            if (holder == null)
            {
                LayoutInflater inflater = this.context.GetSystemService(Context.LayoutInflaterService).JavaCast<LayoutInflater>();

                view = inflater.Inflate(Resource.Layout.UsbFolderOrFileItem, parent, false);

                holder = new UsbFileAndFolderListAdapterViewHolder();
                view.Tag = holder;

                holder.Name = view.FindViewById<TextView>(Resource.Id.folderOrFileName);
                holder.Image = view.FindViewById<ImageView>(Resource.Id.folderOrFileImage);
            }

            // bind new variables
            IUsbFile usbFile = this.folderAndFileList[position];

            string filename = usbFile.Name;
            if (position == 0 &&
                this.firstItemIsParent)
            {
                filename = "..";
            }

            holder.Name.Text = filename;

            holder.Image.SetImageResource(usbFile.IsDirectory
                ? Resource.Drawable.folder_outline
                : Resource.Drawable.file_outline);

            return view;
        }
    }
}
