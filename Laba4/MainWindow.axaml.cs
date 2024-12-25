using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Laba4
{
    public partial class MainWindow : Window
    {
        private Bitmap originalImage;
        private WriteableBitmap? _originalImage; // Убедимся, что originalImage не является локальной переменной метода
        private Bitmap originalImageCopy; // Копия оригинального изображения
        private int angle = 0;
        private int crpX, crpY, rectW, rectH;
        private bool isSelecting = false;
        private bool isDragging = false;
        private bool isFirstPointSelected = false;
        private double offsetX; // Смещение по X
        private double offsetY; // Смещение по Y

        /**/
        private Canvas _drawingCanvas;
        private List<Avalonia.Point> _currentLinePoints;
        private bool _isDrawing;
        private bool allowedToDraw;
        private ImmutablePen _pen;


        public MainWindow()
        {
            InitializeComponent();

            InitializeImageTransform();

            /**/
            _drawingCanvas = this.FindControl<Canvas>("DrawingCanvas");
            _currentLinePoints = new List<Avalonia.Point>();
            _isDrawing = false;
            allowedToDraw = false;

            /**/


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






        private void DrawingCanvas_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (!allowedToDraw) return;
            _isDrawing = true;
            _currentLinePoints.Clear();
            var currentPoint = e.GetCurrentPoint(_drawingCanvas).Position;
            _currentLinePoints.Add(currentPoint);
            e.Handled = true; // Передаем, что событие обработано
        }


        private void DrawingCanvas_PointerMoved(object sender, PointerEventArgs e)
        {
            if (!_isDrawing) return;

            var currentPoint = e.GetCurrentPoint(_drawingCanvas).Position;
            _currentLinePoints.Add(currentPoint);

            if (_currentLinePoints.Count > 1)
            {
                DrawLine(_currentLinePoints[^2], _currentLinePoints[^1]);
            }

            e.Handled = true;
        }

        private void DrawLine(Avalonia.Point p1, Avalonia.Point p2)
        {
            var line = new Line
            {
                StartPoint = p1,
                EndPoint = p2,
                Stroke = _pen.Brush,
                StrokeThickness = _pen.Thickness
            };
            _drawingCanvas.Children.Add(line);
        }

        private void DrawingCanvas_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            _isDrawing = false;
            e.Handled = true;
        }

        private void DrawingCanvas_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            _isDrawing = false;
            e.Handled = true;
        }

        public void SetRedColor(object? sender, RoutedEventArgs args)
        {
            _pen = new ImmutablePen(Brushes.Red, 2);
            allowedToDraw = true;
        }

        public void SetGreenColor(object? sender, RoutedEventArgs args)
        {
            _pen = new ImmutablePen(Brushes.Green, 2);
            allowedToDraw = true;

        }
        public void SetBlueColor(object? sender, RoutedEventArgs args)
        {
            _pen = new ImmutablePen(Brushes.Blue, 2);
            allowedToDraw = true;

        }
        public void stopDrawing(object? sender, RoutedEventArgs args)
         {
            _isDrawing = false;
            allowedToDraw = false;

        }















        // Открытие и выбор файла ( изображения )
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


        // Обрезка изображения
        private void InkCrop_Click(object? sender, RoutedEventArgs e)
        {
            if (originalImage == null || rectW <= 0 || rectH <= 0)
                return;

            int x = (int)crpX;
            int y = (int)crpY;
            int w = (int)rectW;
            int h = (int)rectH;

            // Проверка границ
            if (x < 0) { w += x; x = 0; }
            if (y < 0) { h += y; y = 0; }
            if (x + w > originalImage.PixelSize.Width) w = originalImage.PixelSize.Width - x;
            if (y + h > originalImage.PixelSize.Height) h = originalImage.PixelSize.Height - y;

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
                originalImage.CopyPixels(new PixelRect(x, y, w, h), destData.Address, w * h * 4, w * 4);
            }


            // Сохраним обрезанное изображение как текущее
            originalImage = croppedBitmap;
            // Обновим изображение picBox
            picBox.Source = croppedBitmap;


            // Получаем размеры picBox
            double containerWidth = picBox.Bounds.Width;
            double containerHeight = picBox.Bounds.Height;
            // Получаем размеры обрезанного изображения
            double imageWidth = croppedBitmap.PixelSize.Width;
            double imageHeight = croppedBitmap.PixelSize.Height;

            // Рассчитываем смещения для центрирования
            offsetX = (containerWidth - imageWidth) / 2;
            offsetY = (containerHeight - imageHeight) / 2;

            // Создаем или получаем TransformGroup
            TransformGroup group;

            if (picBox.RenderTransform is TransformGroup existingGroup)
            {
                group = existingGroup;
            }
            else
            {
                group = new TransformGroup();
                picBox.RenderTransform = group;
            }


            // Создаем или получаем TranslateTransform
            TranslateTransform translateTransform;
            if (group.Children.FirstOrDefault(t => t is TranslateTransform) is TranslateTransform existingTranslateTransform)
            {
                translateTransform = existingTranslateTransform;
            }
            else
            {
                translateTransform = new TranslateTransform();
                group.Children.Add(translateTransform);
            }



            // Устанавливаем смещение для центрирования
            translateTransform.X = offsetX;
            translateTransform.Y = offsetY;

            //Очищаем выделение
            isSelecting = false;
            rectW = 0;
            rectH = 0;
            SelectionBorder.Width = 0;
            SelectionBorder.Height = 0;
            SelectionBorder.Margin = new Thickness(0, 0, 0, 0);
            picBox.InvalidateVisual();
        }


        // Сохранения изображения
        private async void InkSaveImage_Click(object? sender, RoutedEventArgs e)
        {
            if (picBox.Source == null) return;

            var dlg = new SaveFileDialog()
            {
                DefaultExtension = "png",  // Рекомендую PNG по умолчанию
                Filters = new List<FileDialogFilter>()
                {
                new FileDialogFilter(){ Name = "PNG image", Extensions = { "png" } },
                new FileDialogFilter() { Name = "JPeg Image", Extensions = { "jpg" } },
                new FileDialogFilter() { Name = "Bitmap Image", Extensions = { "bmp" } },
                new FileDialogFilter() { Name = "Gif Image", Extensions = { "gif" } }
                }
            };

            var savePath = await dlg.ShowAsync(this);
            if (!string.IsNullOrEmpty(savePath))
            {
                await SaveCanvas(DrawingCanvas, savePath); // Вызываем метод сохранения
            }
        }
   
        // Метод для сохранения Canvas
        public async Task SaveCanvas(Canvas canvas, string filePath)
        {
            var width = (int)canvas.Bounds.Width;
            var height = (int)canvas.Bounds.Height;

            if (width <= 0 || height <= 0)
            {
                return; // Защита, если размеры Canvas некорректные
            }

            using (var rtb = new RenderTargetBitmap(new PixelSize(width, height)))
            {
                canvas.Measure(new Avalonia.Size(width, height));
                canvas.Arrange(new Rect(0, 0, width, height));
                rtb.Render(canvas);

                using (var stream = File.Create(filePath))
                {
                    // Определяем формат по расширению файла:
                    var extension = System.IO.Path.GetExtension(filePath).ToLower();
                    rtb.Save(stream);

                }
            }
        }













        // Поворот изображения против часовой стрелки
        private void InkRotateLeft_Click(object? sender, RoutedEventArgs e)
        {
            if (originalImage == null) return;
            picBox.Source = RotateByAngle(originalImage, -90);
        }


        // Поворот изображения по часовой стрелке
        private void InkRotateRight_Click(object? sender, RoutedEventArgs e)
        {
            if (originalImage == null) return;
            picBox.Source = RotateByAngle(originalImage, 90);
        }


        // Реализация поворота изображения
        private Bitmap RotateByAngle(Bitmap img, int angle)
        {

            int w = img.PixelSize.Width;
            int h = img.PixelSize.Height;

            // Длина и ширина меняются местами
            int newW = h; 
            int newH = w;

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
                Avalonia.Platform.PixelFormat.Bgra8888,
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


        // Составление композиции
        private void InkComposition_Click(object? sender, RoutedEventArgs e)
        {
            // Аналогично, составление композиции — нужна доп. логика рендеринга.
            if (originalImage == null) return;

            // 1. Получаем фоновое изображение (текущее picBox.Source)
            var backgroundBitmap = originalImage;

            if (backgroundBitmap == null) return;


            // 2. Создаем композиционное изображение
            int compWidth = backgroundBitmap.PixelSize.Width;
            int compHeight = backgroundBitmap.PixelSize.Height;

            var compositeBitmap = new WriteableBitmap(
                new PixelSize(compWidth, compHeight),
                new Avalonia.Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888,
                 AlphaFormat.Unpremul);


            // 3. Рисуем фоновое изображение на композиционном
            using (var destData = compositeBitmap.Lock())
            {
                backgroundBitmap.CopyPixels(
                new PixelRect(0, 0, compWidth, compHeight),
                destData.Address,
                compWidth * compHeight * 4,
                compWidth * 4);

                // Если есть выделение, получаем обрезанную область
                if (rectW > 0 && rectH > 0)
                {
                    var croppedWidth = (int)Math.Abs(rectW);
                    var croppedHeight = (int)Math.Abs(rectH);

                    // Создаем обрезанное изображение
                    var croppedBitmap = new WriteableBitmap(
                      new PixelSize(croppedWidth, croppedHeight),
                      new Avalonia.Vector(96, 96),
                      Avalonia.Platform.PixelFormat.Bgra8888,
                      AlphaFormat.Unpremul);

                    using (var croppedData = croppedBitmap.Lock())
                    {
                        // Получаем координаты начала выделения
                        int x = (int)crpX;
                        int y = (int)crpY;
                        // Проверка границ
                        if (x < 0) { croppedWidth += x; x = 0; }
                        if (y < 0) { croppedHeight += y; y = 0; }
                        if (x + croppedWidth > backgroundBitmap.PixelSize.Width) croppedWidth = backgroundBitmap.PixelSize.Width - x;
                        if (y + croppedHeight > backgroundBitmap.PixelSize.Height) croppedHeight = backgroundBitmap.PixelSize.Height - y;
                        // Рисуем выделенную область на созданный bitmap
                        backgroundBitmap.CopyPixels(
                            new PixelRect(x, y, croppedWidth, croppedHeight),
                            croppedData.Address,
                            croppedWidth * croppedHeight * 4,
                             croppedWidth * 4);
                    }

                    // 4. Рисуем накладываемое изображение

                    // Получаем offset для накладываемого изображения
                    int offsetX = (int)crpX;
                    int offsetY = (int)crpY;

                    using (var compData = compositeBitmap.Lock())
                    {
                        croppedBitmap.CopyPixels(
                       new PixelRect(0, 0, croppedBitmap.PixelSize.Width, croppedBitmap.PixelSize.Height),
                        compData.Address + (offsetY * compWidth + offsetX) * 4,
                        croppedBitmap.PixelSize.Width * croppedBitmap.PixelSize.Height * 4,
                        compWidth * 4);
                    }
                }
            }

            // 5. Обновляем отображение
            originalImage = compositeBitmap;
            picBox.Source = compositeBitmap;

            // Очищаем выделение
            isSelecting = false;
            rectW = 0;
            rectH = 0;
            SelectionBorder.Width = 0;
            SelectionBorder.Height = 0;
            SelectionBorder.Margin = new Thickness(0, 0, 0, 0);
            picBox.InvalidateVisual();
        }

        // Применение фильтров
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

        // Наложение фильтров на изображение
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

        
        // Изменение яркости
        private void BrightnessSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            ApplyFiltersToImage(BrightnessSlider.Value, ContrastSlider.Value);
        }


        // Изменение контрастности
        private void ContrastSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            ApplyFiltersToImage(BrightnessSlider.Value, ContrastSlider.Value);
        }


        // Приближение изображения
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


        // Отдаление изображения
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

        // Ограничивает рамки изображения при масштабировании
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

        // Инициализирует TransformGroup и RenderTransformOrigin
        private void InitializeImageTransform()
        {
            if (picBox.RenderTransform is not TransformGroup group)
            {
                group = new TransformGroup();

                // Добавляем ScaleTransform
                var scaleTransform = new ScaleTransform { ScaleX = 1, ScaleY = 1 };
                group.Children.Add(scaleTransform);

                // Добавляем TranslateTransform
                var translateTransform = new TranslateTransform();
                group.Children.Add(translateTransform);

                picBox.RenderTransform = group;
            }

            // Устанавливаем RenderTransformOrigin для масштабирования из центра
            picBox.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        }


        // Определяем нажатый пискель ( для выделения обрезаемой области )
        private void PicBox_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (isSelecting)
            {
                var p = e.GetCurrentPoint(picBox).Position;

                if (!isFirstPointSelected)
                {
                    // Первая точка, теперь сохраняем координаты с учетом смещения
                    crpX = (int)(p.X - offsetX);
                    crpY = (int)(p.Y - offsetY);
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
           
        }


        // Определяем саму рамку( для выделения обрезаемой области )
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
            
        }


        private void PicBox_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            isSelecting = false;
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
            if (isSelecting) return;
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
