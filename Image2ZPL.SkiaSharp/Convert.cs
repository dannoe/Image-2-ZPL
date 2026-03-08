using SkiaSharp;

namespace Image2ZPL.SkiaSharp
{
    public static class Convert
    {
        /// <summary>
        /// Converts BMP, PNG, JPG into a ZB64 string that can be used for a ZPLII printer.
        /// </summary>
        /// <param name="imageToConvert">SKBitmap image to be converted.</param>
        /// <param name="posX">The horizontal position to be printed.</param>
        /// <param name="posY">The vertical position to be printed.</param>
        /// <returns>A ZPLII image command string.</returns>
        public static string SKBitmapToZPLII(SKBitmap imageToConvert, int posX, int posY)
        {
            return Conversion.ZPLII.GetZPLIIImage(imageToConvert, posX, posY);
        }
    }
}
