using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace ImageClipboard
{
    class ClipboardWrite
    {
        public static void SetClipboardImage(Bitmap image, Bitmap imageNoTr, DataObject data)
        {
            Clipboard.Clear();
            if (data == null)
                data = new DataObject();
            if (imageNoTr == null)
                imageNoTr = image;

            using (MemoryStream pngMemStream = new MemoryStream())
            using (MemoryStream dibMemStream = new MemoryStream())
            {
                data.SetData(DataFormats.Bitmap, true, imageNoTr);
                image.Save(pngMemStream, ImageFormat.Png);
                data.SetData("PNG", false, pngMemStream);

                Byte[] dibData = ConvertToDib(image);
                dibMemStream.Write(dibData, 0, dibData.Length);
                data.SetData(DataFormats.Dib, false, dibMemStream);

                Clipboard.SetDataObject(data, true);
            }
        }

        public static Byte[] ConvertToDib(Image image)
        {
            Byte[] bm32bData;
            Int32 width = image.Width;
            Int32 height = image.Height;

            using (Bitmap bm32b = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics gr = Graphics.FromImage(bm32b))
                    gr.DrawImage(image, new Rectangle(0, 0, bm32b.Width, bm32b.Height));
                bm32b.RotateFlip(RotateFlipType.Rotate180FlipX);
                Int32 stride;
                bm32bData = ImageUtils.GetImageData(bm32b, out stride);
            }

            Int32 hdrSize = 0x28;
            Byte[] fullImage = new Byte[hdrSize + 12 + bm32bData.Length];

            ArrayUtils.WriteIntToByteArray(fullImage, 0x00, 4, true, (UInt32)hdrSize);
            ArrayUtils.WriteIntToByteArray(fullImage, 0x04, 4, true, (UInt32)width);
            ArrayUtils.WriteIntToByteArray(fullImage, 0x08, 4, true, (UInt32)height);
            ArrayUtils.WriteIntToByteArray(fullImage, 0x0C, 2, true, 1);
            ArrayUtils.WriteIntToByteArray(fullImage, 0x0E, 2, true, 32);
            ArrayUtils.WriteIntToByteArray(fullImage, 0x10, 4, true, 3);
            ArrayUtils.WriteIntToByteArray(fullImage, 0x14, 4, true, (UInt32)bm32bData.Length);

            ArrayUtils.WriteIntToByteArray(fullImage, hdrSize + 0, 4, true, 0x00FF0000);
            ArrayUtils.WriteIntToByteArray(fullImage, hdrSize + 4, 4, true, 0x0000FF00);
            ArrayUtils.WriteIntToByteArray(fullImage, hdrSize + 8, 4, true, 0x000000FF);
            Array.Copy(bm32bData, 0, fullImage, hdrSize + 12, bm32bData.Length);
            return fullImage;
        }
    }
}
