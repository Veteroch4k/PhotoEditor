using Avalonia.Media.Imaging;
using Avalonia.Media;
using Avalonia;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Laba4.Models
{
    /// <summary>
    /// Структура для хранения линии (списка точек) и цвета/толщины.
    /// </summary>
    public class DrawLine
    {
        public List<Avalonia.Point> Points { get; set; } = new();
        public Avalonia.Media.Color Color { get; set; } = Colors.Black;
        public double Thickness { get; set; } = 2;
    }

    /// <summary>
    /// Структура для хранения текста, который нужно вывести.
    /// </summary>
    public class TextElement
    {
        public Avalonia.Point Position { get; set; }
        public string Text { get; set; } = string.Empty;
        public Avalonia.Media.Color Color { get; set; } = Colors.Red;
        public double FontSize { get; set; } = 16;
    }

    public class ImageModel
    {
        /// <summary>
        /// Текущее отображаемое (редактируемое) изображение.
        /// </summary>
        public Bitmap? CurrentImage { get; private set; }

        /// <summary>
        /// Оригинальная копия, чтобы можно было "откатываться"
        /// или заново применять фильтры и т.д.
        /// </summary>
        public Bitmap? OriginalImage { get; private set; }

        /// <summary>
        /// Список нарисованных кистью линий.
        /// (Храним до момента "объединения" с битмапой)
        /// </summary>
        private readonly List<DrawLine> _lines = new();

        /// <summary>
        /// Список надписей (текстов), которые нужно отобразить поверх.
        /// </summary>
        private readonly List<TextElement> _texts = new();

        #region Загрузка / Сохранение
        public void LoadImage(string filePath)
        {
            if (!File.Exists(filePath)) return;

            var bmp = new Bitmap(filePath);
            CurrentImage = bmp;
            OriginalImage = new Bitmap(filePath); // копия
            _lines.Clear();
            _texts.Clear();
        }

        public void SaveImage(string filePath)
        {
            if (CurrentImage == null) return;

            // Пример простой записи в PNG:
            using var fs = File.Create(filePath);
            CurrentImage.Save(fs);
        }
        #endregion

        #region Поворот / Масштабирование / Обрезка
        /// <summary>
        /// Повернуть изображение на угол (в градусах).
        /// </summary>
        public void Rotate(int angle)
        {
            if (CurrentImage == null) return;

            using var memoryStream = new MemoryStream();
            CurrentImage.Save(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(memoryStream);
            img.Mutate(x => x.Rotate(angle));

            // Перезаписываем обратно в Avalonia Bitmap
            using var outStream = new MemoryStream();
            img.SaveAsPng(outStream);
            outStream.Seek(0, SeekOrigin.Begin);
            CurrentImage = new Bitmap(outStream);
        }

        /// <summary>
        /// Масштабирование (scaleFactor > 1 => увеличить, 0 < factor < 1 => уменьшить).
        /// </summary>
        public void Scale(double scaleFactor)
        {
            if (CurrentImage == null || scaleFactor <= 0) return;

            using var memoryStream = new MemoryStream();
            CurrentImage.Save(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            using var imageSharp = SixLabors.ImageSharp.Image.Load<Rgba32>(memoryStream);

            var newWidth = (int)(imageSharp.Width * scaleFactor);
            var newHeight = (int)(imageSharp.Height * scaleFactor);
            if (newWidth <= 0 || newHeight <= 0) return;

            imageSharp.Mutate(x => x.Resize(newWidth, newHeight));

            using var outStream = new MemoryStream();
            imageSharp.SaveAsPng(outStream);
            outStream.Seek(0, SeekOrigin.Begin);
            CurrentImage = new Bitmap(outStream);
        }

        /// <summary>
        /// Обрезка по выделенной области.
        /// </summary>
        public void Crop(int x, int y, int width, int height)
        {
            if (CurrentImage == null) return;

            using var mem = new MemoryStream();
            CurrentImage.Save(mem);
            mem.Seek(0, SeekOrigin.Begin);

            using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(mem);
            // Проверяем границы
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (x + width > img.Width) width = img.Width - x;
            if (y + height > img.Height) height = img.Height - y;
            if (width <= 0 || height <= 0) return;

            img.Mutate(i => i.Crop(new SixLabors.ImageSharp.Rectangle(x, y, width, height)));

            using var outStream = new MemoryStream();
            img.SaveAsPng(outStream);
            outStream.Seek(0, SeekOrigin.Begin);
            CurrentImage = new Bitmap(outStream);
        }
        #endregion

        #region Фильтры
        public void ApplyBrightnessContrast(float brightness, float contrast)
        {
            if (OriginalImage == null) return;

            using var mem = new MemoryStream();
            OriginalImage.Save(mem);
            mem.Seek(0, SeekOrigin.Begin);

            using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(mem);
            img.Mutate(x =>
            {
                if (brightness != 0)
                    x.Brightness(1 + brightness);
                if (contrast != 0)
                    x.Contrast(1 + contrast);
            });

            using var outStream = new MemoryStream();
            img.SaveAsPng(outStream);
            outStream.Seek(0, SeekOrigin.Begin);
            CurrentImage = new Bitmap(outStream);
        }
        #endregion

        #region Рисование кистью
        /// <summary>
        /// Начать новую линию (например, при PointerPressed).
        /// </summary>
        public void StartLine(Avalonia.Media.Color color, double thickness)
        {
            _lines.Add(new DrawLine
            {
                Color = color,
                Thickness = thickness,
                Points = new List<Avalonia.Point>()
            });
        }

        /// <summary>
        /// Добавить точку к последней линии (PointerMoved).
        /// </summary>
        public void AddPointToLine(Avalonia.Point pt)
        {
            if (_lines.Count == 0) return;
            _lines[^1].Points.Add(pt);
        }

        /// <summary>
        /// Преобразовать все линии и тексты в итоговую битмапу (CurrentImage).
        /// </summary>
        public void CommitDrawingsToImage()
        {
            if (CurrentImage == null) return;

            // Запоминаем исходный размер
            var w = CurrentImage.PixelSize.Width;
            var h = CurrentImage.PixelSize.Height;

            // Создаем промежуточный RenderTargetBitmap и "поверх" рисуем линии и текст.
            var renderBitmap = new RenderTargetBitmap(new PixelSize(w, h));
            using (var ctx = renderBitmap.CreateDrawingContext())
            {
                // Сначала нарисуем "старое" изображение
                ctx.DrawImage(CurrentImage, new Rect(0, 0, w, h));

                // Рисуем каждую линию
                foreach (var line in _lines)
                {
                    if (line.Points.Count < 2) continue;

                    for (int i = 0; i < line.Points.Count - 1; i++)
                    {
                        var p1 = line.Points[i];
                        var p2 = line.Points[i + 1];
                        var pen = new Avalonia.Media.Immutable.ImmutablePen(
                            Brushes.Red,
                            line.Thickness
                        );
                        ctx.DrawLine(pen, p1, p2);
                    }
                }

                // Рисуем каждый текст
                foreach (var textElem in _texts)
                {
                    var ft = new FormattedText(
                        textElem.Text,
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Verdana"),
                        textElem.FontSize,
                        new SolidColorBrush(textElem.Color)
                    );

                    ctx.DrawText(ft, textElem.Position);
                }
            }

            // Обновляем CurrentImage конечной отрисовкой
            CurrentImage = renderBitmap;

            // После commit можно очистить списки (если нужно), 
            // чтобы линии/тексты считались «зафиксированными».
            _lines.Clear();
            _texts.Clear();
        }
        #endregion

        #region Добавление текста
        /// <summary>
        /// Добавить текст. Сам рендеринг произойдёт в CommitDrawingsToImage().
        /// </summary>
        public void AddText(Avalonia.Point position, string text, Avalonia.Media.Color color, double fontSize = 16)
        {
            _texts.Add(new TextElement
            {
                Position = position,
                Text = text,
                Color = color,
                FontSize = fontSize
            });
        }
        #endregion
    }
}
