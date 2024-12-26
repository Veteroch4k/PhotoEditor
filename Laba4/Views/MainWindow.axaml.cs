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
using Avalonia.Remote.Protocol.Input;
using Laba4.ViewModels;
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
        private MainViewModel? VM => DataContext as MainViewModel;

        private Avalonia.Point _startSelect;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        // При нажатии кнопки "Select" (InkSelectArea) (например, в XAML Click="InkSelectArea_Click")
        private void InkSelectArea_Click(object? sender, RoutedEventArgs e)
        {
            if (VM == null) return;
            VM.StartSelection();
            SelectionBorder.IsVisible = true;
        }

        // Когда нажимаем на Canvas
        private void DrawingCanvas_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            var pos = e.GetPosition(DrawingCanvas);
            if (VM == null) return;

            // Если добавляем текст
            if (VM.IsAddingText)
            {
                // Добавляем текст в VM
                VM.AddText(pos);
                // Обычно сбрасываем режим IsAddingText = false,
                // или ждем, пока пользователь сам его отключит
                return;
            }

            // Если рисуем
            if (VM.IsDrawing)
            {
                VM.StartLine();
                VM.AddPointToLine(pos);
                return;
            }

            // Иначе, если в режиме выделения
            if (VM.IsSelectingArea)
            {
                _startSelect = pos;
                // Сбросим ширину/высоту
                VM.SelectionWidth = 0;
                VM.SelectionHeight = 0;
            }
        }

        private void DrawingCanvas_PointerMoved(object sender, PointerEventArgs e)
        {
            if (VM == null) return;
            var pos = e.GetPosition(DrawingCanvas);

            // Если рисуем (и нажата левая кнопка)
            if (VM.IsDrawing && e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed)
            {
                VM.AddPointToLine(pos);
                return;
            }

            // Если выделяем (и зажата левая кнопка)
            if (VM.IsSelectingArea && e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed)
            {
                // Обновить Selection
                VM.UpdateSelection(_startSelect, pos);

                // Обновим UI (SelectionBorder)
                var x = VM.SelectionX;
                var y = VM.SelectionY;
                var w = VM.SelectionWidth;
                var h = VM.SelectionHeight;

                Canvas.SetLeft(SelectionBorder, x);
                Canvas.SetTop(SelectionBorder, y);
                SelectionBorder.Width = w;
                SelectionBorder.Height = h;
            }
        }

        private void DrawingCanvas_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (VM == null) return;

            // Заканчиваем выделение
            if (VM.IsSelectingArea)
            {
                VM.EndSelection();
                // Если нужно сразу скрыть рамку:
                // SelectionBorder.IsVisible = false;
            }
        }

        // Пример: при нажатии кнопки "Add text"
        private void OnAddTextButtonClick(object? sender, RoutedEventArgs e)
        {
            if (VM != null)
            {
                VM.IsAddingText = true;
            }
        }
    }
}
