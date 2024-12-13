using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.IO;
using System.Linq;

namespace Laba4
 // азербайджанцы тут
{
    public partial class MainWindow : Window
    {
        private Bitmap originalImage;
        private int angle = 0;

        int crpX, crpY, rectW, rectH;
        private bool isSelecting = false;

        public MainWindow()
        {
            InitializeComponent();

            picBox.PointerPressed += PicBox_PointerPressed;
            picBox.PointerMoved += PicBox_PointerMoved;
            picBox.PointerEntered += PicBox_PointerEnter;

            InkOpenFile.Click += InkOpenFile_Click;
            InkSaveImage.Click += InkSaveImage_Click;
            InkRotateLeft.Click += InkRotateLeft_Click;
            InkRotateRight.Click += InkRotateRight_Click;
            InkSelectArea.Click += InkSelectArea_Click;
            InkCrop.Click += InkCrop_Click;
            InkAddText.Click += InkAddText_Click;
            InkPaint.Click += InkPaint_Click;
            InkComposition.Click += InkComposition_Click;
            InkFilters.Click += InkFilters_Click;
            InkZoomIn.Click += InkZoomIn_Click;
            InkZoomOut.Click += InkZoomOut_Click;
        }

        private async void InkOpenFile_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filters.Add(new FileDialogFilter { Name = "Image files", Extensions = { "gif", "jpg", "jpeg", "bmp", "wmf", "png" } });

            var result = await dlg.ShowAsync(this);
            if (result != null && result.Length > 0 && File.Exists(result[0]))
            {
                originalImage = new Bitmap(result[0]);
                picBox.Source = originalImage;
                angle = 0;
            }
        }

        private void InkSelectArea_Click(object? sender, RoutedEventArgs e)
        {
            isSelecting = true;
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
                new Vector(96, 96),
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
            rectW = 0;
            rectH = 0;
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
                new Vector(96, 96),
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

        private void InkFilters_Click(object? sender, RoutedEventArgs e)
        {
            // Применение фильтров — аналогично, доступ к пикселям через CopyPixels/Lock.
        }

        private void InkZoomIn_Click(object? sender, RoutedEventArgs e)
        {
            // Пример: изменить масштаб через RenderTransform (нужно задать TransformOrigin и т.д.)
            // picBox.RenderTransform = new ScaleTransform(2,2);
        }

        private void InkZoomOut_Click(object? sender, RoutedEventArgs e)
        {
            // Аналогично InkZoomIn, но уменьшение
            // picBox.RenderTransform = new ScaleTransform(0.5,0.5);
        }

        private void PicBox_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!isSelecting) return;
            var p = e.GetCurrentPoint(picBox).Position;
            crpX = (int)p.X;
            crpY = (int)p.Y;
        }

        private void PicBox_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (!isSelecting) return;
            var p = e.GetCurrentPoint(picBox).Position;
            rectW = (int)(p.X - crpX);
            rectH = (int)(p.Y - crpY);
            // Для отображения рамки выделения нужно реализовать собственный рендеринг (не входит в данный пример).
        }

        private void PicBox_PointerEnter(object? sender, PointerEventArgs e)
        {
            this.Cursor = isSelecting ? new Cursor(StandardCursorType.Cross) : new Cursor(StandardCursorType.Arrow);
        }
    }
}