using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace Laba4.Operations
{
    public class ApplyingFiltersToImage
    {

        public static Bitmap ApplyFilters(Bitmap bitmapCopy, double brightness, double contrast)
        {

            if (bitmapCopy == null) return null;

            using var memoryStream = new MemoryStream();
            bitmapCopy.Save(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            // Загружаем изображение с помощью ImageSharp
            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(memoryStream);

            // Применяем фильтры с помощью Mutate
            image.Mutate(x =>
            {
                if (brightness != 0)
                {
                    x.Brightness((float)(1 + brightness));
                }

                if (contrast != 0)
                {
                    x.Contrast((float)(1 + contrast));
                }
            });

            // Сохраняем измененное изображение
            using var outputStream = new MemoryStream();
            image.SaveAsPng(outputStream);
            outputStream.Seek(0, SeekOrigin.Begin);
            return new Bitmap(outputStream);

        }

    }
}
