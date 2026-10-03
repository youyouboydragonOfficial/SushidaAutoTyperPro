using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace SushidaAutoTyper.Services
{
    public class OcrEngineService
    {
        private OcrEngine? _ocrEngine;

        public OcrEngineService()
        {
            InitializeOcrEngine();
        }

        private void InitializeOcrEngine()
        {
            try
            {
                // Try English first for Romaji / typing prompts
                var lang = new Windows.Globalization.Language("en-US");
                if (OcrEngine.IsLanguageSupported(lang))
                {
                    _ocrEngine = OcrEngine.TryCreateFromLanguage(lang);
                }

                if (_ocrEngine == null)
                {
                    _ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
                }
            }
            catch
            {
                _ocrEngine = null;
            }
        }

        /// <summary>
        /// Captures a screen region and performs OCR with image preprocessing for maximum accuracy.
        /// </summary>
        public async Task<string> RecognizeScreenRegionAsync(int x, int y, int width, int height)
        {
            if (_ocrEngine == null || width <= 0 || height <= 0) return string.Empty;

            try
            {
                using var bmp = CaptureScreenRegion(x, y, width, height);
                if (bmp == null) return string.Empty;

                using var processedBmp = PreprocessBitmap(bmp);
                using var softwareBmp = await ConvertBitmapToSoftwareBitmapAsync(processedBmp);

                if (softwareBmp == null) return string.Empty;

                var ocrResult = await _ocrEngine.RecognizeAsync(softwareBmp);
                string text = ocrResult.Text;

                return CleanAndSanitizeText(text);
            }
            catch
            {
                return string.Empty;
            }
        }

        private Bitmap CaptureScreenRegion(int x, int y, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            }
            return bmp;
        }

        /// <summary>
        /// Preprocesses image: 2x Scale, Grayscale, High Contrast to optimize OCR accuracy and prevent character misrecognition.
        /// </summary>
        private Bitmap PreprocessBitmap(Bitmap original)
        {
            int newWidth = original.Width * 2;
            int newHeight = original.Height * 2;
            Bitmap scaled = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(scaled))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(original, 0, 0, newWidth, newHeight);
            }

            // Grayscale & Thresholding
            Bitmap processed = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);
            for (int py = 0; py < newHeight; py++)
            {
                for (int px = 0; px < newWidth; px++)
                {
                    Color col = scaled.GetPixel(px, py);
                    int gray = (int)(col.R * 0.3 + col.G * 0.59 + col.B * 0.11);
                    // Binarize (High Contrast)
                    Color newCol = gray > 140 ? Color.White : Color.Black;
                    processed.SetPixel(px, py, newCol);
                }
            }
            scaled.Dispose();
            return processed;
        }

        private async Task<SoftwareBitmap?> ConvertBitmapToSoftwareBitmapAsync(Bitmap bitmap)
        {
            using MemoryStream stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            stream.Position = 0;

            IRandomAccessStream randomAccessStream = stream.AsRandomAccessStream();
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
            return await decoder.GetSoftwareBitmapAsync();
        }

        /// <summary>
        /// Cleans OCR text, eliminates Mojibake, normalizes fullwidth characters to standard halfwidth Romaji/ASCII.
        /// </summary>
        private string CleanAndSanitizeText(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;

            StringBuilder sb = new StringBuilder();
            foreach (char c in rawText)
            {
                // Fullwidth ASCII to Halfwidth conversion
                if (c >= 0xFF01 && c <= 0xFF5E)
                {
                    sb.Append((char)(c - 0xEE00));
                }
                else if (c == 0x3000) // Fullwidth space
                {
                    sb.Append(' ');
                }
                else if (c >= 32 && c <= 126) // Standard printable ASCII
                {
                    sb.Append(c);
                }
                else if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9')
                {
                    sb.Append(c);
                }
            }

            string result = sb.ToString();
            // Clean consecutive spaces
            result = Regex.Replace(result, @"\s+", " ").Trim();
            return result;
        }
    }
}
