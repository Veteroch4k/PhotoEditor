using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;


namespace Laba4.Operations
{
    public class RotatingImage
    {

        public static Bitmap RotateImageRight90(Bitmap bitmap)
        {
            if (bitmap == null) return null;

            using var stream = new MemoryStream();
            bitmap.Save(stream);
            stream.Position = 0;

            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
            image.Mutate(x => x.Rotate(90));

            using var outputStream = new MemoryStream();
            image.SaveAsPng(outputStream);
            outputStream.Position = 0;

            return new Bitmap(outputStream);
        }

        // Поворот против часовой стрелки на 90 градусов
        public static Bitmap RotateImageLeft90(Bitmap bitmap)
        {
            if (bitmap == null) return null;

            using var stream = new MemoryStream();
            bitmap.Save(stream);
            stream.Position = 0;

            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
            image.Mutate(x => x.Rotate(-90));

            using var outputStream = new MemoryStream();
            image.SaveAsPng(outputStream);
            outputStream.Position = 0;

            return new Bitmap(outputStream);
        }

    }
}
