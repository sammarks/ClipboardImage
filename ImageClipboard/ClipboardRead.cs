using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;

namespace ImageClipboard
{
    class ClipboardRead
    {
        public static Bitmap GetClipboardImage(DataObject retrievedData)
        {
            Bitmap clipboardImage = null;
            if (retrievedData.GetDataPresent("PNG"))
            {
                MemoryStream pngStream = retrievedData.GetData("PNG") as MemoryStream;
                if (pngStream != null)
                    using (Bitmap bm = new Bitmap(pngStream))
                        clipboardImage = ImageUtils.CloneImage(bm);
            }
            if (clipboardImage == null && retrievedData.GetDataPresent(DataFormats.Dib))
            {
                MemoryStream dib = retrievedData.GetData(DataFormats.Dib) as MemoryStream;
                if (dib != null)
                    clipboardImage = ImageFromClipboardDib(dib.ToArray());
            }
            if (clipboardImage == null && retrievedData.GetDataPresent(DataFormats.Bitmap))
                clipboardImage = new Bitmap(retrievedData.GetData(DataFormats.Bitmap) as Image);
            if (clipboardImage == null && retrievedData.GetDataPresent(typeof(Image)))
                clipboardImage = new Bitmap(retrievedData.GetData(typeof(Image)) as Image);
            return clipboardImage;
        }

        public static Bitmap ImageFromClipboardDib(Byte[] dibBytes)
        {
            if (dibBytes == null || dibBytes.Length < 4)
                return null;
            try
            {
                Int32 headerSize = (Int32)ArrayUtils.ReadIntFromByteArray(dibBytes, 0, 4, true);
                if (headerSize != 40) return null;
                Byte[] header = new Byte[40];
                Array.Copy(dibBytes, header, 40);
                Int32 imageIndex = headerSize;
                Int32 width = (Int32)ArrayUtils.ReadIntFromByteArray(header, 0x04, 4, true);
                Int32 height = (Int32)ArrayUtils.ReadIntFromByteArray(header, 0x08, 4, true);
                Int16 planes = (Int16)ArrayUtils.ReadIntFromByteArray(header, 0x0C, 2, true);
                Int16 bitCount = (Int16)ArrayUtils.ReadIntFromByteArray(header, 0x0E, 2, true);
                Int32 compression = (Int32)ArrayUtils.ReadIntFromByteArray(header, 0x10, 4, true);

                // Not dealing with non-standard formats.
                if (planes != 1 || (compression != 0 && compression != 3))
                    return null;

                PixelFormat fmt;
                switch (bitCount)
                {
                    case 32:
                        fmt = PixelFormat.Format32bppRgb;
                        break;
                    case 24:
                        fmt = PixelFormat.Format24bppRgb;
                        break;
                    case 16:
                        fmt = PixelFormat.Format16bppRgb555;
                        break;
                    default:
                        return null;
                }

                if (compression == 3) imageIndex += 12;
                if (dibBytes.Length < imageIndex) return null;

                Byte[] image = new Byte[dibBytes.Length - imageIndex];
                Array.Copy(dibBytes, imageIndex, image, 0, image.Length);
                Int32 stride = (((((bitCount * width) + 7) / 8) + 3) / 4) * 4;
                if (compression == 3)
                {
                    UInt32 redMask = ArrayUtils.ReadIntFromByteArray(dibBytes, headerSize + 0, 4, true);
                    UInt32 greenMask = ArrayUtils.ReadIntFromByteArray(dibBytes, headerSize + 4, 4, true);
                    UInt32 blueMask = ArrayUtils.ReadIntFromByteArray(dibBytes, headerSize + 8, 4, true);

                    if (bitCount == 32 && redMask == 0xFF0000 && greenMask == 0x00FF00 && blueMask == 0x0000FF)
                    {
                        for (Int32 pix = 3; pix < image.Length; pix += 4)
                        {
                            if (image[pix] == 0) continue;
                            fmt = PixelFormat.Format32bppArgb;
                            break;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }

                Bitmap bitmap = ImageUtils.BuildImage(image, width, height, stride, fmt, null, null);
                bitmap.RotateFlip(RotateFlipType.Rotate180FlipX);
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
