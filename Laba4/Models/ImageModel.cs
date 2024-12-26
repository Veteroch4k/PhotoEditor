using Avalonia.Media;
using Avalonia;
using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Globalization;
using System.IO;
using Avalonia.Platform;
using System.Linq;

namespace Laba4.Models
{
    public class ImageModel
    {

        /// <summary>
        /// Текущее изображение в формате Avalonia Bitmap.
        /// </summary>
        public Bitmap? CurrentBitmap { get; private set; }
        public Bitmap? CurrentBitmapCopy { get; private set; }

        /// <summary>
        /// Загружает изображение из файла и сохраняет в CurrentBitmap.
        /// </summary>
        public void LoadFromFile(string path)
        {
            if (!File.Exists(path))
                return;

            CurrentBitmap = new Bitmap(path);
            CurrentBitmapCopy = new Bitmap(path);
        }

        /// <summary>
        /// Сохраняет текущее изображение в указанный файл (в том формате, который соответствует расширению).
        /// </summary>
        public void SaveToFile(string path)
        {
            if (CurrentBitmap == null) return;

            using var fs = File.Create(path);
            CurrentBitmap.Save(fs);
        }

        /// <summary>
        /// Поворот изображения (пример: на 90 градусов по часовой стрелке).
        /// </summary>
        public void RotateRight90()
        {
            if (CurrentBitmap == null) return;

            using var stream = new MemoryStream();
            CurrentBitmap.Save(stream);
            stream.Position = 0;

            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
            image.Mutate(x => x.Rotate(90));

            using var outputStream = new MemoryStream();
            image.SaveAsPng(outputStream);
            outputStream.Position = 0;

            CurrentBitmap = new Bitmap(outputStream);
        }

        /// <summary>
        /// Аналогично можно сделать RotateLeft90, Crop, ApplyFilters и т. п.
        /// </summary>
        public void RotateLeft90()
        {
            if (CurrentBitmap == null) return;

            using var stream = new MemoryStream();
            CurrentBitmap.Save(stream);
            stream.Position = 0;

            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
            image.Mutate(x => x.Rotate(-90));

            using var outputStream = new MemoryStream();
            image.SaveAsPng(outputStream);
            outputStream.Position = 0;

            CurrentBitmap = new Bitmap(outputStream);
        }

        // Пример применения контрастности/яркости
        public void ApplyFilters(float brightness, float contrast)
        {
            if (CurrentBitmapCopy == null) return;

            // Сохраняем текущее изображение в MemoryStream
            using var stream = new MemoryStream();
            CurrentBitmapCopy.Save(stream);
            stream.Position = 0;

            // Загружаем в ImageSharp
            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
            image.Mutate(x =>
            {
                // Яркость: 1.0f - исходная, >1 - ярче, <1 - тусклее
                x.Brightness(brightness);
                x.Contrast(contrast);
            });

            // Обратно в Bitmap
            using var outputStream = new MemoryStream();
            image.SaveAsPng(outputStream);
            outputStream.Position = 0;
            CurrentBitmap = new Bitmap(outputStream);
        }


        public RenderTargetBitmap AddTextToImage(string currentText, Avalonia.Point currentTextPosition)
        {
            if (CurrentBitmap == null)
                return null;

            // Получаем размеры изображения
            var width = CurrentBitmap.PixelSize.Width;
            var height = CurrentBitmap.PixelSize.Height;

            PixelSize pixelSize = new PixelSize(width, height);

            // Создаем RenderTargetBitmap для комбинированного изображения
            var renderBitmap = new RenderTargetBitmap(pixelSize);
            using (var context = renderBitmap.CreateDrawingContext(true))
            {
                // Рисуем исходное изображение
                context.DrawImage(CurrentBitmap, new Rect(0, 0, width, height));

                // Устанавливаем параметры текста
                var formattedText = new FormattedText(currentText, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Verdana"), 32, Brushes.Red);
                var textBrush = Brushes.White;

                // Рисуем текст на изображении
                context.DrawText(formattedText, new Avalonia.Point(currentTextPosition.X, currentTextPosition.Y));
            }

            // Отображаем новое изображение в UI
            //picBox.Source = renderBitmap;
            return renderBitmap;
        }


        public void CropImage(double width, double height, int crpX, int crpY, int rectW, int rectH) // ширина и высота выделенной области
        {
            if (CurrentBitmap == null || rectW <= 0 || rectH <= 0)
                return;

            int x = (int)crpX;
            int y = (int)crpY;
            int w = (int)rectW;
            int h = (int)rectH;

            // Проверка границ
            if (x < 0) { w += x; x = 0; }
            if (y < 0) { h += y; y = 0; }
            if (x + w > CurrentBitmap.PixelSize.Width) w = CurrentBitmap.PixelSize.Width - x;
            if (y + h > CurrentBitmap.PixelSize.Height) h = CurrentBitmap.PixelSize.Height - y;

            if (w <= 0 || h <= 0) return;

            // Создадим новый WriteableBitmap для обрезанного фрагмента
            var croppedBitmap = new WriteableBitmap(
               new PixelSize(w, h),
               new Avalonia.Vector(96, 96),
               Avalonia.Platform.PixelFormat.Bgra8888,
               AlphaFormat.Unpremul);

            using (var destData = croppedBitmap.Lock())
            {
                // Скопируем пиксели из оригинального изображения в новую область
                CurrentBitmap.CopyPixels(new PixelRect(x, y, w, h), destData.Address, w * h * 4, w * 4);
            }


            // Сохраним обрезанное изображение как текущее
            CurrentBitmap = croppedBitmap;
            // Обновим изображение picBox
            //picBox.Source = croppedBitmap;

        }


    }
}
