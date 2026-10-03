using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        private OcrEngine? _ocrEngineEn;
        private OcrEngine? _ocrEngineJa;

        public OcrEngineService()
        {
            InitializeOcrEngine();
        }

        private void InitializeOcrEngine()
        {
            try
            {
                var langEn = new Windows.Globalization.Language("en-US");
                if (OcrEngine.IsLanguageSupported(langEn))
                {
                    _ocrEngineEn = OcrEngine.TryCreateFromLanguage(langEn);
                }

                var langJa = new Windows.Globalization.Language("ja-JP");
                if (OcrEngine.IsLanguageSupported(langJa))
                {
                    _ocrEngineJa = OcrEngine.TryCreateFromLanguage(langJa);
                }

                if (_ocrEngineEn == null && _ocrEngineJa == null)
                {
                    _ocrEngineEn = OcrEngine.TryCreateFromUserProfileLanguages();
                }
            }
            catch { }
        }

        /// <summary>
        /// Captures screen region and recognizes text.
        /// </summary>
        public async Task<(string rawText, string romajiText)> RecognizeScreenRegionAsync(int x, int y, int width, int height, bool extractRomajiOnly = true)
        {
            OcrEngine? engine = _ocrEngineEn ?? _ocrEngineJa;
            if (engine == null || width <= 5 || height <= 5) return (string.Empty, string.Empty);

            try
            {
                using var bmp = CaptureScreenRegion(x, y, width, height);
                if (bmp == null) return (string.Empty, string.Empty);

                using var scaledBmp = ScaleBitmap(bmp, 2);
                using var softwareBmp = await ConvertBitmapToSoftwareBitmapAsync(scaledBmp);

                if (softwareBmp == null) return (string.Empty, string.Empty);

                var ocrResult = await engine.RecognizeAsync(softwareBmp);
                string rawText = ocrResult.Text ?? string.Empty;

                string cleaned = CleanText(rawText);
                string romajiOnly = ExtractRomajiPrompt(cleaned);

                return (cleaned, extractRomajiOnly ? romajiOnly : cleaned);
            }
            catch
            {
                return (string.Empty, string.Empty);
            }
        }

        public Bitmap? CaptureScreenRegion(int x, int y, int width, int height)
        {
            if (width <= 0 || height <= 0) return null;
            try
            {
                Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                }
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        private Bitmap ScaleBitmap(Bitmap original, int factor)
        {
            int newW = original.Width * factor;
            int newH = original.Height * factor;
            Bitmap scaled = new Bitmap(newW, newH, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(scaled))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(original, 0, 0, newW, newH);
            }
            return scaled;
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

        private string CleanText(string rawText)
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
                else if (c == 0x3000)
                {
                    sb.Append(' ');
                }
                else if (c != '\r' && c != '\n')
                {
                    sb.Append(c);
                }
            }

            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }

        private string ExtractRomajiPrompt(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // Extract lower/upper ASCII characters and common symbols used in Romaji typing
            var matches = Regex.Matches(text, @"[a-zA-Z0-9\-\,\.\?\!\'\ ]+");
            StringBuilder sb = new StringBuilder();
            foreach (Match match in matches)
            {
                sb.Append(match.Value);
            }

            return sb.ToString().Trim();
        }
    }
}
