using System.Drawing;
using SkiaSharp;
using Xunit;

namespace Image2ZPL.Tests
{
    public class SkiaSharpConvertTests
    {
        private static string GetTestImagePath(string filename)
        {
            return Path.Combine(AppContext.BaseDirectory, "TestImages", filename);
        }

        [Theory]
        [InlineData("checkerboard_8x8.bmp")]
        [InlineData("black_16x16.bmp")]
        [InlineData("white_16x16.bmp")]
        [InlineData("diagonal_16x16.png")]
        [InlineData("gradient_32x32.png")]
        [InlineData("stripe_13x7.bmp")]
        public void SkiaSharp_ProducesNonEmptyOutput(string imageFile)
        {
            string path = GetTestImagePath(imageFile);
            using var bitmap = SKBitmap.Decode(path);

            string result = Image2ZPL.SkiaSharp.Convert.SKBitmapToZPLII(bitmap, 10, 20);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.StartsWith("^FO10,20^GFA,", result);
            Assert.EndsWith("^FS", result);
        }

        [Fact]
        public void SkiaSharp_AllBlack_ContainsExpectedPattern()
        {
            string path = GetTestImagePath("black_16x16.bmp");
            using var bitmap = SKBitmap.Decode(path);

            string result = Image2ZPL.SkiaSharp.Convert.SKBitmapToZPLII(bitmap, 0, 0);

            Assert.StartsWith("^FO0,0^GFA,", result);
            Assert.EndsWith("^FS", result);
            // All black should produce '!' (all bits set) rows
            Assert.Contains("!", result);
        }

        [Fact]
        public void SkiaSharp_AllWhite_ContainsExpectedPattern()
        {
            string path = GetTestImagePath("white_16x16.bmp");
            using var bitmap = SKBitmap.Decode(path);

            string result = Image2ZPL.SkiaSharp.Convert.SKBitmapToZPLII(bitmap, 0, 0);

            Assert.StartsWith("^FO0,0^GFA,", result);
            Assert.EndsWith("^FS", result);
        }

        [Fact]
        public void SkiaSharp_DifferentPositions_ChangesHeader()
        {
            string path = GetTestImagePath("black_16x16.bmp");
            using var bitmap = SKBitmap.Decode(path);

            string result1 = Image2ZPL.SkiaSharp.Convert.SKBitmapToZPLII(bitmap, 10, 20);
            string result2 = Image2ZPL.SkiaSharp.Convert.SKBitmapToZPLII(bitmap, 30, 40);

            Assert.StartsWith("^FO10,20^GFA,", result1);
            Assert.StartsWith("^FO30,40^GFA,", result2);
        }
    }

    /// <summary>
    /// Tests that require System.Drawing.Common (Windows-only).
    /// These tests are skipped on non-Windows platforms.
    /// </summary>
    public class SystemDrawingConvertTests
    {
        private static string GetTestImagePath(string filename)
        {
            return Path.Combine(AppContext.BaseDirectory, "TestImages", filename);
        }

        [Theory]
        [InlineData("checkerboard_8x8.bmp")]
        [InlineData("black_16x16.bmp")]
        [InlineData("white_16x16.bmp")]
        [InlineData("diagonal_16x16.png")]
        [InlineData("gradient_32x32.png")]
        [InlineData("stripe_13x7.bmp")]
        public void SystemDrawing_ProducesNonEmptyOutput(string imageFile)
        {
            if (!OperatingSystem.IsWindows()) return;

            string path = GetTestImagePath(imageFile);
            using var bitmap = (Bitmap)Bitmap.FromFile(path);

            string result = Image2ZPL.Convert.BitmapToZPLII(bitmap, 10, 20);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.StartsWith("^FO10,20^GFA,", result);
            Assert.EndsWith("^FS", result);
        }
    }

    /// <summary>
    /// Tests comparing System.Drawing and SkiaSharp output.
    /// These tests are skipped on non-Windows platforms since System.Drawing is required.
    /// </summary>
    public class ComparisonTests
    {
        private static string GetTestImagePath(string filename)
        {
            return Path.Combine(AppContext.BaseDirectory, "TestImages", filename);
        }

        [Theory]
        [InlineData("checkerboard_8x8.bmp")]
        [InlineData("black_16x16.bmp")]
        [InlineData("white_16x16.bmp")]
        [InlineData("stripe_13x7.bmp")]
        public void SystemDrawing_And_SkiaSharp_ProduceIdenticalOutput_ForSimpleImages(string imageFile)
        {
            if (!OperatingSystem.IsWindows()) return;

            string path = GetTestImagePath(imageFile);

            string systemDrawingResult;
            using (var bitmap = (Bitmap)Bitmap.FromFile(path))
            {
                systemDrawingResult = Image2ZPL.Convert.BitmapToZPLII(bitmap, 10, 20);
            }

            string skiaSharpResult;
            using (var bitmap = SKBitmap.Decode(path))
            {
                skiaSharpResult = Image2ZPL.SkiaSharp.Convert.SKBitmapToZPLII(bitmap, 10, 20);
            }

            Assert.Equal(systemDrawingResult, skiaSharpResult);
        }

        [Theory]
        [InlineData("diagonal_16x16.png")]
        [InlineData("gradient_32x32.png")]
        public void SystemDrawing_And_SkiaSharp_ProduceAlmostIdenticalOutput_ForComplexImages(string imageFile)
        {
            if (!OperatingSystem.IsWindows()) return;

            string path = GetTestImagePath(imageFile);

            string systemDrawingResult;
            using (var bitmap = (Bitmap)Bitmap.FromFile(path))
            {
                systemDrawingResult = Image2ZPL.Convert.BitmapToZPLII(bitmap, 10, 20);
            }

            string skiaSharpResult;
            using (var bitmap = SKBitmap.Decode(path))
            {
                skiaSharpResult = Image2ZPL.SkiaSharp.Convert.SKBitmapToZPLII(bitmap, 10, 20);
            }

            // Both should have the same header format (same dimensions means same byte counts)
            Assert.StartsWith("^FO10,20^GFA,", systemDrawingResult);
            Assert.StartsWith("^FO10,20^GFA,", skiaSharpResult);
            Assert.EndsWith("^FS", systemDrawingResult);
            Assert.EndsWith("^FS", skiaSharpResult);

            // Calculate similarity: count matching characters
            int minLength = Math.Min(systemDrawingResult.Length, skiaSharpResult.Length);
            int maxLength = Math.Max(systemDrawingResult.Length, skiaSharpResult.Length);
            int matches = 0;
            for (int i = 0; i < minLength; i++)
            {
                if (systemDrawingResult[i] == skiaSharpResult[i])
                    matches++;
            }

            double similarity = (double)matches / maxLength;
            // Expect at least 80% similarity for complex images where dithering may differ
            Assert.True(similarity > 0.80,
                $"Output similarity is {similarity:P2}, expected > 80%. " +
                $"System.Drawing length: {systemDrawingResult.Length}, SkiaSharp length: {skiaSharpResult.Length}");
        }

        [Fact]
        public void SystemDrawing_And_SkiaSharp_AllBlack_ProduceIdenticalOutput()
        {
            if (!OperatingSystem.IsWindows()) return;

            string path = GetTestImagePath("black_16x16.bmp");

            string systemDrawingResult;
            using (var bitmap = (Bitmap)Bitmap.FromFile(path))
            {
                systemDrawingResult = Image2ZPL.Convert.BitmapToZPLII(bitmap, 0, 0);
            }

            string skiaSharpResult;
            using (var bitmap = SKBitmap.Decode(path))
            {
                skiaSharpResult = Image2ZPL.SkiaSharp.Convert.SKBitmapToZPLII(bitmap, 0, 0);
            }

            Assert.Equal(systemDrawingResult, skiaSharpResult);
        }

        [Fact]
        public void SystemDrawing_And_SkiaSharp_AllWhite_ProduceIdenticalOutput()
        {
            if (!OperatingSystem.IsWindows()) return;

            string path = GetTestImagePath("white_16x16.bmp");

            string systemDrawingResult;
            using (var bitmap = (Bitmap)Bitmap.FromFile(path))
            {
                systemDrawingResult = Image2ZPL.Convert.BitmapToZPLII(bitmap, 0, 0);
            }

            string skiaSharpResult;
            using (var bitmap = SKBitmap.Decode(path))
            {
                skiaSharpResult = Image2ZPL.SkiaSharp.Convert.SKBitmapToZPLII(bitmap, 0, 0);
            }

            Assert.Equal(systemDrawingResult, skiaSharpResult);
        }
    }
}
