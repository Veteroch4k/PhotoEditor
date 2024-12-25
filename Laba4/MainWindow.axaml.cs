using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Linq;

namespace Laba4
{
    public partial class MainWindow : Window
    {
        private Bitmap originalImage;
        private Bitmap originalImageCopy; // Копия оригинального изображения
        private int angle = 0;
        private int crpX, crpY, rectW, rectH;
        private bool isSelecting = false;
        private bool isDragging = false;
        private bool isFirstPointSelected = false;
        private Avalonia.Point dragStartPoint;

        public MainWindow()
        {
            InitializeComponent();

            // Найти SelectionBorder
            SelectionBorder = this.FindControl<Border>("SelectionBorder");

            picBox.PointerPressed += PicBox_PointerPressed;
            picBox.PointerMoved += PicBox_PointerMoved;
            picBox.PointerReleased += PicBox_PointerReleased;
            picBox.PointerEntered += PicBox_PointerEnter;
            picBox.PointerWheelChanged += PicBox_PointerWheelChanged;

            InkOpenFile.Click += InkOpenFile_Click;
            InkSaveImage.Click += InkSaveImage_Click;
            InkRotateLeft.Click += InkRotateLeft_Click;
            InkRotateRight.Click += InkRotateRight_Click;
            InkSelectArea.Click += InkSelectArea_Click;
            InkCrop.Click += InkCrop_Click;
            InkAddText.Click += InkAddText_Click;
            InkPaint.Click += InkPaint_Click;
            InkComposition.Click += InkComposition_Click;
            InkZoomIn.Click += InkZoomIn_Click;
            InkZoomOut.Click += InkZoomOut_Click;

            // Добавляем обработчики для ползунков
            BrightnessSlider.ValueChanged += BrightnessSlider_ValueChanged;
            ContrastSlider.ValueChanged += ContrastSlider_ValueChanged;
        }

        private async void InkOpenFile_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filters.Add(new FileDialogFilter { Name = "Image files", Extensions = { "gif", "jpg", "jpeg", "bmp", "wmf", "png" } });

            var result = await dlg.ShowAsync(this);
            if (result != null && result.Length > 0 && File.Exists(result[0]))
            {
                originalImage = new Bitmap(result[0]);
                originalImageCopy = new Bitmap(result[0]); // Сохраняем копию
                picBox.Source = originalImage;
                angle = 0;
            }
        }

        private void InkSelectArea_Click(object? sender, RoutedEventArgs e)
        {
            isSelecting = true;
            SelectionBorder.IsVisible = true;
        }

        private void InkCrop_Click(object? sender, RoutedEventArgs e)
        {
            if (originalImage == null || rectW <= 0 || rectH <= 0)
                return;

            int x = crpX;
            int y = crpY;
            int w = rectW;
            int h = rectH;

            // Проверка границ
            if (x < 0) { w += x; x = 0; }
            if (y < 0) { h += y; y = 0; }
            if (x + w > originalImage.PixelSize.Width) w = originalImage.PixelSize.Width - x;
            if (y + h > originalImage.PixelSize.Height) h = originalImage.PixelSize.Height - y;

            if (w <= 0 || h <= 0) return;

            // Создадим новый WriteableBitmap для обрезанного фрагмента
            var croppedBitmap = new WriteableBitmap(
                new PixelSize(w, h),
                new Avalonia.Vector(96, 96), // Указываем полное имя Avalonia.Vector
                PixelFormat.Bgra8888,
                AlphaFormat.Unpremul);

            using (var destData = croppedBitmap.Lock())
            {
                // Скопируем пиксели из оригинального изображения в новую область
                originalImage.CopyPixels(new PixelRect(x, y, w, h), destData.Address, w * h * 4, w * 4);
            }

            originalImage = croppedBitmap;
            picBox.Source = croppedBitmap;

            isSelecting = false;
            crpX = 0;
            crpY = 0;
            rectW = 0;
            rectH = 0;
            SelectionBorder.IsVisible = false;
        }

        private async void InkSaveImage_Click(object? sender, RoutedEventArgs e)
        {
            if (picBox.Source == null) return;
            var dlg = new SaveFileDialog();
            dlg.Filters.Add(new FileDialogFilter() { Name = "JPeg Image", Extensions = { "jpg" } });
            dlg.Filters.Add(new FileDialogFilter() { Name = "Bitmap Image", Extensions = { "bmp" } });
            dlg.Filters.Add(new FileDialogFilter() { Name = "Gif Image", Extensions = { "gif" } });

            var savePath = await dlg.ShowAsync(this);
            if (!string.IsNullOrEmpty(savePath))
            {
                using (var fs = new FileStream(savePath, FileMode.Create))
                {
                    // Сохраняем как PNG, так как нет встроенного переключения формата:
                    (picBox.Source as Bitmap)?.Save(fs);
                }
            }
        }

        private void InkRotateLeft_Click(object? sender, RoutedEventArgs e)
        {
            if (originalImage == null) return;
            picBox.Source = RotateByAngle(originalImage, -90);
        }

        private void InkRotateRight_Click(object? sender, RoutedEventArgs e)
        {
            if (originalImage == null) return;
            picBox.Source = RotateByAngle(originalImage, 90);
        }

        private Bitmap RotateByAngle(Bitmap img, int angle)
        {
            if (angle == 0) return img;

            int w = img.PixelSize.Width;
            int h = img.PixelSize.Height;

            int newW = (angle == 180) ? w : h;
            int newH = (angle == 180) ? h : w;

            // Читаем пиксели в буфер
            byte[] buffer = new byte[w * h * 4];
            unsafe
            {
                fixed (byte* ptr = buffer)
                {
                    img.CopyPixels(new PixelRect(0, 0, w, h), (IntPtr)ptr, buffer.Length, w * 4);
                }
            }

            var rotated = new WriteableBitmap(
                new PixelSize(newW, newH),
                new Avalonia.Vector(96, 96), // Указываем полное имя Avalonia.Vector
                PixelFormat.Bgra8888,
                AlphaFormat.Unpremul);

            using (var dstData = rotated.Lock())
            {
                unsafe
                {
                    fixed (byte* srcPtr = buffer)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            for (int x = 0; x < w; x++)
                            {
                                byte* pixel = srcPtr + (y * w * 4) + (x * 4);
                                int dx, dy;
                                switch (angle)
                                {
                                    case 90:
                                        dx = h - 1 - y;
                                        dy = x;
                                        break;
                                    case -90:
                                        dx = y;
                                        dy = w - 1 - x;
                                        break;
                                    default:
                                        dx = x; dy = y;
                                        break;
                                }

                                byte* dstPtr = (byte*)dstData.Address + (dy * dstData.RowBytes) + (dx * 4);
                                dstPtr[0] = pixel[0];
                                dstPtr[1] = pixel[1];
                                dstPtr[2] = pixel[2];
                                dstPtr[3] = pixel[3];
                            }
                        }
                    }
                }
            }

            originalImage = rotated;
            return rotated;
        }

        private void InkAddText_Click(object? sender, RoutedEventArgs e)
        {
            // Требуется использование сторонних инструментов (SkiaSharp) для рендеринга текста на изображении.
        }

        private void InkPaint_Click(object? sender, RoutedEventArgs e)
        {
            // Аналогично. Для рисования нужен SkiaSharp или другой подход.
        }

        private void InkComposition_Click(object? sender, RoutedEventArgs e)
        {
            // Аналогично, составление композиции — нужна доп. логика рендеринга.
        }

        private void ApplyFilters_Click(object? sender, RoutedEventArgs e)
        {
            var popup = this.FindControl<Popup>("FiltersPopup");

            if (popup?.Child is Border border && border.Child is StackPanel stackPanel)
            {
                var brightnessSlider = stackPanel.FindControl<Slider>("BrightnessSlider");
                var contrastSlider = stackPanel.FindControl<Slider>("ContrastSlider");

                var brightness = brightnessSlider?.Value ?? 0;
                var contrast = contrastSlider?.Value ?? 0;

                ApplyFiltersToImage(brightness, contrast);
            }
        }

        private void ApplyFiltersToImage(double brightness, double contrast)
        {
            if (originalImageCopy == null) return;

            using var memoryStream = new MemoryStream();
            originalImageCopy.Save(memoryStream);
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
            originalImage = new Bitmap(outputStream);

            picBox.Source = originalImage;
        }

        private void BrightnessSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            ApplyFiltersToImage(BrightnessSlider.Value, ContrastSlider.Value);
        }

        private void ContrastSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            ApplyFiltersToImage(BrightnessSlider.Value, ContrastSlider.Value);
        }

        private void InkZoomIn_Click(object? sender, RoutedEventArgs e)
        {
            if (picBox.RenderTransform is TransformGroup group)
            {
                if (group.Children.FirstOrDefault(t => t is ScaleTransform) is ScaleTransform scaleTransform)
                {
                    var currentScale = scaleTransform.ScaleX;
                    if (currentScale < 5) // Ограничиваем максимальный масштаб
                    {
                        scaleTransform.ScaleX += 0.2;
                        scaleTransform.ScaleY += 0.2;

                        // Ограничиваем перемещение, чтобы изображение не выходило за границы
                        ClampTranslateTransform(group, picBox.Bounds, scaleTransform.ScaleX);
                    }
                }
            }
        }

        private void InkZoomOut_Click(object? sender, RoutedEventArgs e)
        {
            if (picBox.RenderTransform is TransformGroup group)
            {
                if (group.Children.FirstOrDefault(t => t is ScaleTransform) is ScaleTransform scaleTransform)
                {
                    var currentScale = scaleTransform.ScaleX;
                    if (currentScale > 0.2) // Ограничиваем минимальный масштаб
                    {
                        scaleTransform.ScaleX -= 0.2;
                        scaleTransform.ScaleY -= 0.2;

                        // Ограничиваем перемещение, чтобы изображение не выходило за границы
                        ClampTranslateTransform(group, picBox.Bounds, scaleTransform.ScaleX);
                    }
                }
            }
        }

        private void ClampTranslateTransform(TransformGroup group, Rect bounds, double scale)
        {
            if (group.Children.FirstOrDefault(t => t is TranslateTransform) is TranslateTransform translateTransform)
            {
                // Получаем размеры изображения с учетом масштаба
                var scaledWidth = bounds.Width * scale;
                var scaledHeight = bounds.Height * scale;

                // Ограничиваем перемещение по X
                if (scaledWidth > bounds.Width)
                {
                    translateTransform.X = Math.Clamp(translateTransform.X, -(scaledWidth - bounds.Width) / 2, (scaledWidth - bounds.Width) / 2);
                }
                else
                {
                    translateTransform.X = 0;
                }

                // Ограничиваем перемещение по Y
                if (scaledHeight > bounds.Height)
                {
                    translateTransform.Y = Math.Clamp(translateTransform.Y, -(scaledHeight - bounds.Height) / 2, (scaledHeight - bounds.Height) / 2);
                }
                else
                {
                    translateTransform.Y = 0;
                }
            }
        }

        private void PicBox_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (isSelecting)
            {
                var p = e.GetCurrentPoint(picBox).Position;

                if (!isFirstPointSelected)
                {
                    // Первая точка
                    crpX = (int)p.X;
                    crpY = (int)p.Y;
                    isFirstPointSelected = true;
                }
                else
                {
                    // Вторая точка
                    rectW = (int)(p.X - crpX);
                    rectH = (int)(p.Y - crpY);
                    SelectionBorder.Width = Math.Abs(rectW);
                    SelectionBorder.Height = Math.Abs(rectH);
                    SelectionBorder.Margin = new Thickness(Math.Min(crpX, p.X), Math.Min(crpY, p.Y), 0, 0);
                    isSelecting = false;
                    isFirstPointSelected = false;
                }
            }
            else
            {
                isDragging = true;
                dragStartPoint = e.GetPosition(picBox);
            }


        }

        private void PicBox_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (isSelecting && isFirstPointSelected)
            {
                var p = e.GetCurrentPoint(picBox).Position;
                rectW = (int)(p.X - crpX);
                rectH = (int)(p.Y - crpY);
                SelectionBorder.Width = Math.Abs(rectW);
                SelectionBorder.Height = Math.Abs(rectH);
                SelectionBorder.Margin = new Thickness(Math.Min(crpX, p.X), Math.Min(crpY, p.Y), 0, 0);
            }
            else if (isDragging)
            {
                var currentPoint = e.GetPosition(picBox);
                var delta = currentPoint - dragStartPoint;

                if (picBox.RenderTransform is TransformGroup group)
                {
                    if (group.Children.FirstOrDefault(t => t is TranslateTransform) is TranslateTransform translateTransform &&
                       group.Children.FirstOrDefault(t => t is ScaleTransform) is ScaleTransform scaleTransform)
                    {
                        // Учитываем текущий масштаб
                        translateTransform.X += delta.X / scaleTransform.ScaleX;
                        translateTransform.Y += delta.Y / scaleTransform.ScaleY;
                    }
                }
                dragStartPoint = currentPoint;

            }
        }

        private void PicBox_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            isSelecting = false;
            isDragging = false;
            isFirstPointSelected = false;
            // Сбрасываем размеры рамки, убираем её
            SelectionBorder.Width = 0;
            SelectionBorder.Height = 0;
            SelectionBorder.Margin = new Thickness(0, 0, 0, 0);
        }

        private void PicBox_PointerEnter(object? sender, PointerEventArgs e)
        {
            this.Cursor = isSelecting ? new Cursor(StandardCursorType.Cross) : new Cursor(StandardCursorType.Arrow);
        }

        private void PicBox_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            var delta = e.Delta.Y;
            if (delta > 0)
            {
                InkZoomIn_Click(sender, e);
            }
            else
            {
                InkZoomOut_Click(sender, e);
            }
        }
    }
}
