using System.Text;

namespace Image2ZPL.Core.Conversion
{
    /// <summary>
    /// Platform-agnostic ZPLII encoding logic.
    /// Converts pre-processed monochrome image data into ZPLII format strings.
    /// </summary>
    public static class ZPLIIEncoder
    {
        /// <summary>
        /// Builds a ZPLII image command string from pre-processed monochrome image data.
        /// </summary>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="imageData">Row-by-row monochrome image data (1 bit per pixel, inverted: 1=black, 0=white).
        /// Each row is a byte array of length ceil(width/8).</param>
        /// <param name="posX">The horizontal position to be printed.</param>
        /// <param name="posY">The vertical position to be printed.</param>
        /// <returns>A ZPLII image command string.</returns>
        public static string BuildZPLIIImage(int width, int height, byte[][] imageData, int posX, int posY)
        {
            int rowdata = ((width + 7) / 8);
            int bytes = rowdata * height;

            StringBuilder result = new StringBuilder();
            result.AppendFormat("^FO{0},{1}^GFA,{2},{2},{3},", posX, posY, bytes, rowdata);

            byte[]? previousRow = null;
            foreach (byte[] row in imageData)
            {
                AppendLine(row, previousRow, result);
                previousRow = row;
            }
            result.Append(@"^FS");

            return result.ToString();
        }

        /// <summary>
        /// Converts byte to ZB64 and appends to current string.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="previousRow"></param>
        /// <param name="zb64Stream"></param>
        internal static void AppendLine(byte[] row, byte[]? previousRow, StringBuilder zb64Stream)
        {
            if (row.All(r => r == 0))
            {
                zb64Stream.Append(',');
                return;
            }

            if (row.All(r => r == 0xff))
            {
                zb64Stream.Append('!');
                return;
            }

            if (previousRow != null && MatchByteArray(row, previousRow))
            {
                zb64Stream.Append(':');
                return;
            }

            byte[] nibbles = new byte[row.Length * 2];
            for (int i = 0; i < row.Length; i++)
            {
                nibbles[i * 2] = (byte)(row[i] >> 4);
                nibbles[i * 2 + 1] = (byte)(row[i] & 0x0f);
            }

            for (int i = 0; i < nibbles.Length; i++)
            {
                byte pixel = nibbles[i];

                int repcount = 0;
                for (int j = i; j < nibbles.Length && repcount <= 400; j++)
                {
                    if (pixel == nibbles[j])
                    {
                        repcount++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (repcount > 2)
                {
                    if (repcount == nibbles.Length - i
                        && (pixel == 0 || pixel == 0xf))
                    {
                        if (pixel == 0)
                        {
                            if (i % 2 == 1)
                            {
                                zb64Stream.Append('0');
                            }
                            zb64Stream.Append(',');
                            return;
                        }
                        else if (pixel == 0xf)
                        {
                            if (i % 2 == 1)
                            {
                                zb64Stream.Append('F');
                            }
                            zb64Stream.Append('!');
                            return;
                        }
                    }
                    else
                    {
                        zb64Stream.Append(ConvertZB64(repcount));
                        i += repcount - 1;
                    }
                }
                zb64Stream.Append(pixel.ToString("X"));
            }
        }

        /// <summary>
        /// Converts to ZB64 format specified in the ZPLII Programming document.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        internal static string ConvertZB64(int count)
        {
            if (count > 419)
                throw new ArgumentOutOfRangeException();

            int high = count / 20;
            int low = count % 20;

            const string lowString = " GHIJKLMNOPQRSTUVWXY";
            const string highString = " ghijklmnopqrstuvwxyz";

            string repeatSequence = "";
            if (high > 0)
            {
                repeatSequence += highString[high];
            }
            if (low > 0)
            {
                repeatSequence += lowString[low];
            }

            return repeatSequence;
        }

        internal static bool MatchByteArray(byte[] row, byte[] lastrow)
        {
            for (int i = 0; i < row.Length; i++)
            {
                if (row[i] != lastrow[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
