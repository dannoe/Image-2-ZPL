using System.Drawing;
using System.Drawing.Imaging;
using Image2ZPL.Core.Conversion;

namespace Image2ZPL.Conversion
{
    /// <summary>
    /// ZPLII image conversion using System.Drawing.
    /// </summary>
    internal class ZPLII
    {
        /// <summary>
        /// Builds the string for the converted ZB64 to be used in tha ZPLII command string.
        /// </summary>
        /// <param name="srcbitmap">Provided Image</param>
        /// <param name="posx">Horizontal posistion</param>
        /// <param name="posy">Vertical posistion</param>
        /// <returns></returns>
        internal static string GetZPLIIImage(Bitmap srcbitmap, int posx, int posy)
        {

            Rectangle dim = new Rectangle(Point.Empty, srcbitmap.Size);
            int stride = ((dim.Width + 7) / 8);

            using (Bitmap bmpCompressed = srcbitmap.Clone(dim, PixelFormat.Format1bppIndexed))
            {
                byte[][] imageData = ConvertImageBinary(dim, stride, bmpCompressed);
                return ZPLIIEncoder.BuildZPLIIImage(dim.Width, dim.Height, imageData, posx, posy);
            }
        }

        /// <summary>
        /// Converts the image into a byte array (pointer) and converts the image byte-by-byte while inverting the color of the image color for printing.
        /// </summary>
        /// <param name="dim"></param>
        /// <param name="stride"></param>
        /// <param name="bmpimage"></param>
        /// <returns></returns>
        private static byte[][] ConvertImageBinary(Rectangle dim, int stride, Bitmap bmpimage)
        {
            byte[][] imagebytes;
            var data = bmpimage.LockBits(dim, ImageLockMode.ReadOnly, PixelFormat.Format1bppIndexed);
            try
            {
                // This is required to perform operations with pointers. This is only working with a locked bitmap in memory so it is "safe".
                unsafe
                {
                    byte* pixelData = (byte*)data.Scan0.ToPointer();
                    byte mask = (byte)(0xff << (data.Stride * 8 - dim.Width));
                    imagebytes = new byte[dim.Height][];

                    for (int x = 0; x < dim.Height; x++)
                    {
                        byte* rowStart = pixelData + x * data.Stride;
                        imagebytes[x] = new byte[stride];

                        for (int y = 0; y < stride; y++)
                        {
                            byte invert = (byte)(0xff ^ rowStart[y]);
                            invert = (y == stride - 1) ? (byte)(invert & mask) : invert;
                            imagebytes[x][y] = invert;
                        }
                    }
                }
            }
            finally
            {
                bmpimage.UnlockBits(data);
            }
            return imagebytes;
        }
    }
}
