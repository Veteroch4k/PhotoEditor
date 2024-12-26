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
using Laba4.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Laba4
{
    public partial class MainWindow : Window
    {

        private ImageModel imageModel;

        /**/
        private Bitmap originalImage; // Битовая матрица изображения
        private Bitmap originalImageCopy; // Битовая матрица изображения


        private int crpX, crpY, rectW, rectH;
        private bool isSelecting = false; // Выбираем ли заданную область
        private bool isFirstPointSelected = false;
        private double offsetX; // Смещение по X
        private double offsetY; // Смещение по Y

        /*Для рисования*/
        private Canvas _drawingCanvas; // Полотно для рисования
        private List<Avalonia.Point> _currentLinePoints;
        private bool _isDrawing;
        private bool allowedToDraw;
        private ImmutablePen _pen;

        /*Для текста*/

        private bool isAddingText = false;
        private TextBox currentTextBox;
        private Avalonia.Point clickPosition;
        private string currentText;
        private Avalonia.Point currentTextPosition;


        public MainWindow()
        {

            imageModel = new ImageModel();

            InitializeComponent();

            InitializeImageTransform();

            /*Рисование*/
            _drawingCanvas = this.FindControl<Canvas>("DrawingCanvas");
            _currentLinePoints = new List<Avalonia.Point>();
            _isDrawing = false;
            allowedToDraw = false;

            /**/


            // Найти SelectionBorder
            SelectionBorder = this.FindControl<Border>("SelectionBorder");

            // Добавление слушаетелей на события
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
            InkZoomIn.Click += InkZoomIn_Click;
            InkZoomOut.Click += InkZoomOut_Click;

            // Добавляем обработчики для ползунков
            BrightnessSlider.ValueChanged += BrightnessSlider_ValueChanged;
            ContrastSlider.ValueChanged += ContrastSlider_ValueChanged;
        }


        /// <summary>
        /// Все то, что я уже добавил
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>



        // Открытие и выбор файла ( изображения )
        private async void InkOpenFile_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filters.Add(new FileDialogFilter { Name = "Image files", Extensions = { "gif", "jpg", "jpeg", "bmp", "wmf", "png" } });

            var result = await dlg.ShowAsync(this);
            if (result != null && result.Length > 0 && File.Exists(result[0]))
            {

                imageModel.LoadFromFile(result[0]); // получаем путь к изображению
                originalImage = new Bitmap(result[0]);
                originalImageCopy = new Bitmap(result[0]);
                picBox.Source = originalImage;
            }
        }


        // Поворот изображения против часовой стрелки
        private void InkRotateLeft_Click(object? sender, RoutedEventArgs e)
        {
            imageModel.RotateLeft90();
            
            picBox.Source = imageModel.CurrentBitmap;
        }


        // Поворот изображения по часовой стрелке
        private void InkRotateRight_Click(object? sender, RoutedEventArgs e)
        {
            imageModel.RotateRight90();

            picBox.Source = imageModel.CurrentBitmap;
        }


        // Изменение яркости
        private void BrightnessSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            imageModel.ApplyFilters(BrightnessSlider.Value, ContrastSlider.Value);
            picBox.Source = imageModel.CurrentBitmap;

        }


        // Изменение контрастности
        private void ContrastSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            imageModel.ApplyFilters(BrightnessSlider.Value, ContrastSlider.Value);
            picBox.Source = imageModel.CurrentBitmap;

        }


        // Обработчик клика на кнопку добавления текста
        private void OnAddTextButtonClick(object? sender, RoutedEventArgs e)
        {
            isAddingText = true;
        }


        /*Пока не перенес*/



        // Выбрана ли функция выделения
        private void InkSelectArea_Click(object? sender, RoutedEventArgs e)
        {
            isSelecting = true;
            SelectionBorder.IsVisible = true;
        }


        // Обрезка изображения
        private void InkCrop_Click(object? sender, RoutedEventArgs e)
        {
            
            // Сохраним обрезанное изображение как текущее
            imageModel.CropImage(crpX, crpY, rectW, rectH);            
            // Обновим изображение picBox
            picBox.Source = imageModel.CurrentBitmap;

            /*ВСО*/


        }



        /// <summary>
        /// Все то, что я уже добавил (сверху)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        // Начало рисование после выбора кисти
        private void DrawingCanvas_PointerPressed(object sender, PointerPressedEventArgs e)
        {

            // Проверяем, что режим добавления текста активен и что Canvas и оригинальное изображение доступны
            if (isAddingText && _drawingCanvas != null && originalImage != null)
            {
                // Получаем позицию клика по Canvas
                clickPosition = e.GetPosition(_drawingCanvas);

                // Убедитесь, что позиция клика в пределах Canvas
                if (clickPosition.X < 0 || clickPosition.Y < 0 || clickPosition.X > _drawingCanvas.Width || clickPosition.Y > _drawingCanvas.Height)
                {
                    return; // Если клик вне Canvas, не создаем TextBox
                }

                // Создаем TextBox для ввода текста
                currentTextBox = new TextBox
                {
                    Text = "Введите ваш текст здесь...",  // Предустановленный текст
                    Foreground = Brushes.Black,
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Avalonia.Thickness(0),
                    FontSize = 16,
                    Width = 200,
                    Height = 50,
                };

                // Устанавливаем позицию TextBox на Canvas в место клика
                Canvas.SetLeft(currentTextBox, clickPosition.X);
                Canvas.SetTop(currentTextBox, clickPosition.Y);

                // Добавляем TextBox на Canvas
                _drawingCanvas.Children.Add(currentTextBox);

                // Устанавливаем фокус на TextBox, чтобы можно было сразу вводить текст
                currentTextBox.Focus();

                // Обработчик события завершения редактирования текста
                currentTextBox.LostFocus += (s, args) =>
                {
                    if (currentTextBox != null)
                    {
                        // Сохраняем введенный текст и позицию
                        currentText = currentTextBox.Text;
                        currentTextPosition = clickPosition;

                        // Убираем TextBox с Canvas
                        _drawingCanvas.Children.Remove(currentTextBox);

                        // Добавляем текст на изображение
                        picBox.Source = imageModel.AddTextToImage(currentText, currentTextPosition);

                        // Очищаем переменную
                        currentTextBox = null;
                    }

                    // Выключаем режим добавления текста
                    isAddingText = false;
                };
            }

            if (!allowedToDraw) return;
            _isDrawing = true;
            _currentLinePoints.Clear(); // Очищение массива точке предыдущего рисования
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
                DrawLine(_currentLinePoints[^2], _currentLinePoints[^1]); // Т.к. мы перемещаем курсор, то мы отрисовывем только две  "последние точки" после перемещения
            }

            e.Handled = true;
        }

        // Отрисовка линии
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

        // Прекратили рисование
        private void DrawingCanvas_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            _isDrawing = false;
            e.Handled = true;
        }

        // Применение красного цвета
        public void SetRedColor(object? sender, RoutedEventArgs args)
        {
            _pen = new ImmutablePen(Brushes.Red, 2);
            allowedToDraw = true;
        }

        // Применение зеленого цвета
        public void SetGreenColor(object? sender, RoutedEventArgs args)
        {
            _pen = new ImmutablePen(Brushes.Green, 2);
            allowedToDraw = true;

        }

        // Применение синего цвета
        public void SetBlueColor(object? sender, RoutedEventArgs args)
        {
            _pen = new ImmutablePen(Brushes.Blue, 2);
            allowedToDraw = true;

        }

        // Прекратить рисование
        public void stopDrawing(object? sender, RoutedEventArgs args)
         {
            _isDrawing = false;
            allowedToDraw = false;

        }

        // Сохранения Канваса
        private async void InkSaveImage_Click(object? sender, RoutedEventArgs e)
        {
            if (picBox.Source == null) return;

            var dlg = new SaveFileDialog()
            {
                DefaultExtension = "png",  // PNG по умолчанию
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
            // Размеры полотная
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


        /* Была логика Канваса*/


        /// <summary>
        /// Функции самого View
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

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

                    }
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
            if (!isSelecting) return;
            var p = e.GetCurrentPoint(picBox).Position;
            rectW = (int)(p.X - crpX);
            rectH = (int)(p.Y - crpY);
            if (isSelecting && isFirstPointSelected)
            {
                p = e.GetCurrentPoint(picBox).Position;
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

        // Изменение внешнего вида курсора
        private void PicBox_PointerEnter(object? sender, PointerEventArgs e)
        {
            this.Cursor = isSelecting ? new Cursor(StandardCursorType.Cross) : new Cursor(StandardCursorType.Arrow);
        }

        // Изменение масштаба колесиком мыши
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


        /***/






    }
}
