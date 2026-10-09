using System.Security.Cryptography;
using SkiaSharp;

namespace DS.Website.Services
{
    public record ProcessedImage(byte[] Content, long SizeBytes, int Width, int Height, string Sha256);

    public static class WebpImageProcessor
    {
        private const int MaxDimension = 2000;
        private const long MaxPixelCount = 40_000_000;
        private const int Quality = 80;

        public static async Task<ProcessedImage> ConvertAsync(Stream source, bool square = false, CancellationToken cancellationToken = default)
        {
            using var memory = new MemoryStream();
            await source.CopyToAsync(memory, cancellationToken);
            memory.Position = 0;

            using var codec = SKCodec.Create(memory);
            if (codec == null) throw new InvalidDataException("Filen er ikke et genkendeligt billede.");

            var info = codec.Info;
            if ((long)info.Width * info.Height > MaxPixelCount) throw new InvalidDataException("Billedet indeholder for mange pixels.");

            memory.Position = 0;
            using var decoded = SKBitmap.Decode(memory);
            if (decoded == null) throw new InvalidDataException("Filen er ikke et genkendeligt billede.");

            using var oriented = ApplyOrigin(decoded, codec.EncodedOrigin);
            using var cropped = square ? CropSquare(oriented) : oriented.Copy();
            using var resized = Resize(cropped);

            using var image = SKImage.FromBitmap(resized);
            using var data = image.Encode(SKEncodedImageFormat.Webp, Quality);
            if (data == null) throw new InvalidOperationException("Kunne ikke kode billedet som WebP.");

            var bytes = data.ToArray();
            var sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

            return new ProcessedImage(bytes, bytes.LongLength, resized.Width, resized.Height, sha256);
        }

        private static SKBitmap CropSquare(SKBitmap bitmap)
        {
            if (bitmap.Width == bitmap.Height) return bitmap.Copy();

            var size = Math.Min(bitmap.Width, bitmap.Height);
            var x = (bitmap.Width - size) / 2;
            var y = (bitmap.Height - size) / 2;

            var cropped = new SKBitmap(size, size);
            using (var canvas = new SKCanvas(cropped))
            {
                var dest = new SKRect(0, 0, size, size);
                var source = new SKRect(x, y, x + size, y + size);
                var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

                canvas.DrawBitmap(bitmap, source, dest, sampling);
            }

            return cropped;
        }

        private static SKBitmap Resize(SKBitmap bitmap)
        {
            if (bitmap.Width <= MaxDimension && bitmap.Height <= MaxDimension) return bitmap.Copy();

            var scale = Math.Min((double)MaxDimension / bitmap.Width, (double)MaxDimension / bitmap.Height);
            var width = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
            var height = Math.Max(1, (int)Math.Round(bitmap.Height * scale));

            var resized = new SKBitmap(width, height);
            using (var canvas = new SKCanvas(resized))
            {
                var dest = new SKRect(0, 0, width, height);
                var source = new SKRect(0, 0, bitmap.Width, bitmap.Height);
                var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

                canvas.DrawBitmap(bitmap, source, dest, sampling);
            }

            return resized;
        }

        private static SKBitmap ApplyOrigin(SKBitmap bitmap, SKEncodedOrigin origin)
        {
            if (origin == SKEncodedOrigin.TopLeft) return bitmap.Copy();

            var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
            var width = swap ? bitmap.Height : bitmap.Width;
            var height = swap ? bitmap.Width : bitmap.Height;
            var result = new SKBitmap(width, height);

            using (var canvas = new SKCanvas(result))
            {
                switch (origin)
                {
                    case SKEncodedOrigin.TopRight:
                        canvas.Translate(bitmap.Width, 0);
                        canvas.Scale(-1, 1);
                        break;
                    case SKEncodedOrigin.BottomRight:
                        canvas.Translate(bitmap.Width, bitmap.Height);
                        canvas.RotateDegrees(180);
                        break;
                    case SKEncodedOrigin.BottomLeft:
                        canvas.Translate(0, bitmap.Height);
                        canvas.Scale(1, -1);
                        break;
                    case SKEncodedOrigin.LeftTop:
                        canvas.RotateDegrees(90);
                        canvas.Scale(1, -1);
                        break;
                    case SKEncodedOrigin.RightTop:
                        canvas.Translate(bitmap.Height, 0);
                        canvas.RotateDegrees(90);
                        break;
                    case SKEncodedOrigin.RightBottom:
                        canvas.Translate(bitmap.Height, bitmap.Width);
                        canvas.RotateDegrees(90);
                        canvas.Scale(-1, 1);
                        break;
                    case SKEncodedOrigin.LeftBottom:
                        canvas.Translate(0, bitmap.Width);
                        canvas.RotateDegrees(270);
                        break;
                }

                canvas.DrawBitmap(bitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None));
            }

            return result;
        }
    }
}
