using Com.Github.Mjdev.Libaums.FS;
using Java.IO;

namespace UsbOtgFileReader
{
    /// <summary>
    /// Parameters to copy from USB device to this device
    /// </summary>
    internal class CopyFromUsbToDeviceParams
    {
        /// <summary>
        /// USB file to copy from
        /// </summary>
        public IUsbFile From { get; set; }

        /// <summary>
        /// Storage file to copy to
        /// </summary>
        public File To { get; set; }

        /// <summary>
        /// Chunk size of current file system
        /// </summary>
        public long ChunkSize { get; set; }
    }
}
