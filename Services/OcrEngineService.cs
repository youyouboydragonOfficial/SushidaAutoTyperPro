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
        /// Captures screen region and recognizes text with Sushida OCR Error Correction & Prolonged Sound Mark ('ー' -> '-') handling.
        /// </summary>
        public async Task<(string rawText, string romajiText)> RecognizeScreenRegionAsync(
            int x, int y, int width, int height, 
            bool extractRomajiOnly = true, 
            bool forceLowercase = true, 
            bool useHighContrastBinarization = true)
        {
            OcrEngine? engine = _ocrEngineEn ?? _ocrEngineJa;
            if (engine == null || width <= 5 || height <= 5) return (string.Empty, string.Empty);

            try
            {
                using var bmp = CaptureScreenRegion(x, y, width, height);
                if (bmp == null) return (string.Empty, string.Empty);

                using var processedBmp = useHighContrastBinarization 
                    ? PreprocessBitmapForSushida(bmp) 
                    : ScaleBitmapSafely(bmp);

                using var softwareBmp = await ConvertBitmapToSoftwareBitmapAsync(processedBmp);

                if (softwareBmp == null) return (string.Empty, string.Empty);

                var ocrResult = await engine.RecognizeAsync(softwareBmp);
                string rawText = ocrResult.Text ?? string.Empty;

                string cleaned = CleanText(rawText);
                
                if (forceLowercase)
                {
                    cleaned = cleaned.ToLowerInvariant();
                }

                // Apply Sushida-specific OCR Error Correction (1/l/| -> i, rn -> m, ー/—/_ -> -)
                string correctedRomaji = CorrectSushidaOcrErrors(cleaned);

                if (extractRomajiOnly)
                {
                    correctedRomaji = FilterSushidaRomajiOnly(correctedRomaji);
                }

                return (cleaned, correctedRomaji);
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

        public Bitmap PreprocessBitmapForSushida(Bitmap original)
        {
            Bitmap scaled = ScaleBitmapSafely(original);
            Bitmap binarized = new Bitmap(scaled.Width, scaled.Height, PixelFormat.Format32bppArgb);

            for (int y = 0; y < scaled.Height; y++)
            {
                for (int x = 0; x < scaled.Width; x++)
                {
                    Color pixel = scaled.GetPixel(x, y);
                    int luminance = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);

                    Color newColor = luminance > 120 ? Color.White : Color.Black;
                    binarized.SetPixel(x, y, newColor);
                }
            }

            scaled.Dispose();
            return binarized;
        }

        private Bitmap ScaleBitmapSafely(Bitmap original)
        {
            const double maxDimension = 2400.0;
            double scale = 2.5;

            if (original.Width * scale > maxDimension)
            {
                scale = maxDimension / original.Width;
            }
            if (original.Height * scale > maxDimension)
            {
                scale = Math.Min(scale, maxDimension / original.Height);
            }
            scale = Math.Max(1.0, scale);

            int newW = (int)(original.Width * scale);
            int newH = (int)(original.Height * scale);

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

            string text = sb.ToString();

            // Convert Katakana prolonged sound mark 'ー', Em Dash '—', En Dash '–', Fullwidth '-' '－', Minus '−', Underscore '_' to ASCII '-'
            text = Regex.Replace(text, @"[\u30FC\u2015\u2013\u2014\u2212\uFF0D_]+", "-");

            return Regex.Replace(text, @"\s+", " ").Trim();
        }

        private string CorrectSushidaOcrErrors(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            string s = text;

            s = s.Replace("rn", "m");
            s = s.Replace("vv", "w");
            s = s.Replace("cl", "d");

            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                if (c == '1' || c == 'l' || c == '|' || c == '!' || c == ']' || c == '[')
                {
                    sb.Append('i');
                }
                else if (c == '0')
                {
                    sb.Append('o');
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        private string FilterSushidaRomajiOnly(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var matches = Regex.Matches(text, @"[a-z\-]+");
            StringBuilder sb = new StringBuilder();
            foreach (Match match in matches)
            {
                sb.Append(match.Value);
            }

            return sb.ToString();
        }
    }
}
