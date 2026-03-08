using Image2ZPL.Core.Conversion;
using SkiaSharp;

namespace Image2ZPL.SkiaSharp.Conversion
{
    /// <summary>
    /// ZPLII image conversion using SkiaSharp.
    /// </summary>
    internal class ZPLII
    {
        /// <summary>
        /// Builds the string for the converted ZB64 to be used in the ZPLII command string.
        /// </summary>
        /// <param name="srcBitmap">Provided Image</param>
        /// <param name="posX">Horizontal position</param>
        /// <param name="posY">Vertical position</param>
        /// <returns></returns>
        internal static string GetZPLIIImage(SKBitmap srcBitmap, int posX, int posY)
        {
            int width = srcBitmap.Width;
            int height = srcBitmap.Height;
            int stride = ((width + 7) / 8);

            byte[][] imageData = ConvertImageBinary(srcBitmap, width, height, stride);
            return ZPLIIEncoder.BuildZPLIIImage(width, height, imageData, posX, posY);
        }

        /// <summary>
        /// Converts the image to monochrome 1bpp data by thresholding pixel luminance.
        /// Black pixels (luminance &lt; 128) become 1, white pixels become 0.
        /// This matches the System.Drawing Format1bppIndexed conversion behavior.
        /// </summary>
        private static byte[][] ConvertImageBinary(SKBitmap srcBitmap, int width, int height, int stride)
        {
            // Ensure we work with a known pixel format
            SKBitmap bitmap;
            if (srcBitmap.ColorType != SKColorType.Rgba8888 && srcBitmap.ColorType != SKColorType.Bgra8888)
            {
                bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.DrawBitmap(srcBitmap, 0, 0);
                }
            }
            else
            {
                bitmap = srcBitmap;
            }

            try
            {
                byte mask = (byte)(0xff << (stride * 8 - width));
                byte[][] imagebytes = new byte[height][];

                for (int y = 0; y < height; y++)
                {
                    imagebytes[y] = new byte[stride];

                    for (int x = 0; x < width; x++)
                    {
                        SKColor pixel = bitmap.GetPixel(x, y);

                        // Calculate luminance using the same weights as System.Drawing's 1bpp conversion
                        // (ITU-R BT.601): 0.299*R + 0.587*G + 0.114*B
                        double luminance = 0.299 * pixel.Red + 0.587 * pixel.Green + 0.114 * pixel.Blue;

                        // Handle transparency: transparent pixels are treated as white
                        if (pixel.Alpha < 128)
                        {
                            luminance = 255;
                        }

                        // Threshold at 128: dark pixels = black (bit set = 1 after inversion)
                        int byteIndex = x / 8;
                        int bitIndex = 7 - (x % 8);

                        if (luminance < 128)
                        {
                            // Dark pixel -> set bit (inverted: 1 = black in ZPL)
                            imagebytes[y][byteIndex] |= (byte)(1 << bitIndex);
                        }
                    }

                    // Apply mask to last byte to clear unused trailing bits
                    imagebytes[y][stride - 1] &= mask;
                }

                return imagebytes;
            }
            finally
            {
                if (bitmap != srcBitmap)
                {
                    bitmap.Dispose();
                }
            }
        }
    }
}
