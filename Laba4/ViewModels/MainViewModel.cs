using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Laba4.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Laba4.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ImageModel _model = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public MainViewModel()
        {
            // Инициализация команд
            OpenFileCommand = new RelayCommand(_ => OnOpenFile());
            SaveFileCommand = new RelayCommand(_ => OnSaveFile());
            RotateLeftCommand = new RelayCommand(_ => OnRotate(-90));
            RotateRightCommand = new RelayCommand(_ => OnRotate(90));
            ZoomInCommand = new RelayCommand(_ => OnScale(1.2));
            ZoomOutCommand = new RelayCommand(_ => OnScale(0.8));
            CropCommand = new RelayCommand(_ => OnCrop());
            ApplyFiltersCommand = new RelayCommand(_ => OnApplyFilters());
            CommitDrawingsCommand = new RelayCommand(_ => OnCommitDrawings());
        }

        #region Путь к файлам (упрощённо)
        private string? _openPath;
        public string? OpenPath
        {
            get => _openPath;
            set { _openPath = value; OnPropertyChanged(); }
        }

        private string? _savePath;
        public string? SavePath
        {
            get => _savePath;
            set { _savePath = value; OnPropertyChanged(); }
        }
        #endregion

        #region Команды
        public ICommand OpenFileCommand { get; }
        public ICommand SaveFileCommand { get; }
        public ICommand RotateLeftCommand { get; }
        public ICommand RotateRightCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand CropCommand { get; }
        public ICommand ApplyFiltersCommand { get; }
        public ICommand CommitDrawingsCommand { get; }

        private void OnOpenFile()
        {
            if (!string.IsNullOrEmpty(OpenPath))
            {
                _model.LoadImage(OpenPath);
                OnPropertyChanged(nameof(CurrentBitmap));
            }
        }

        private void OnSaveFile()
        {
            if (!string.IsNullOrEmpty(SavePath))
            {
                _model.SaveImage(SavePath);
            }
        }

        private void OnRotate(int angle)
        {
            _model.Rotate(angle);
            OnPropertyChanged(nameof(CurrentBitmap));
        }

        private void OnScale(double factor)
        {
            _model.Scale(factor);
            OnPropertyChanged(nameof(CurrentBitmap));
        }

        private void OnCrop()
        {
            if (SelectionWidth > 0 && SelectionHeight > 0)
            {
                _model.Crop(SelectionX, SelectionY, SelectionWidth, SelectionHeight);
                OnPropertyChanged(nameof(CurrentBitmap));
            }
        }

        private void OnApplyFilters()
        {
            _model.ApplyBrightnessContrast((float)Brightness, (float)Contrast);
            OnPropertyChanged(nameof(CurrentBitmap));
        }

        private void OnCommitDrawings()
        {
            _model.CommitDrawingsToImage();
            OnPropertyChanged(nameof(CurrentBitmap));
        }
        #endregion

        #region Cвойства, связанные с изображением (для привязки во View)
        public Bitmap? CurrentBitmap => _model.CurrentImage;
        #endregion

        #region Фильтры
        private double _brightness;
        public double Brightness
        {
            get => _brightness;
            set { _brightness = value; OnPropertyChanged(); }
        }

        private double _contrast;
        public double Contrast
        {
            get => _contrast;
            set { _contrast = value; OnPropertyChanged(); }
        }
        #endregion

        #region Выделение (Selection)
        private bool _isSelectingArea;
        public bool IsSelectingArea
        {
            get => _isSelectingArea;
            set
            {
                _isSelectingArea = value;
                OnPropertyChanged();
            }
        }

        private int _selectionX;
        public int SelectionX
        {
            get => _selectionX;
            set
            {
                _selectionX = value;
                OnPropertyChanged();
            }
        }

        private int _selectionY;
        public int SelectionY
        {
            get => _selectionY;
            set
            {
                _selectionY = value;
                OnPropertyChanged();
            }
        }

        private int _selectionWidth;
        public int SelectionWidth
        {
            get => _selectionWidth;
            set
            {
                _selectionWidth = value;
                OnPropertyChanged();
            }
        }

        private int _selectionHeight;
        public int SelectionHeight
        {
            get => _selectionHeight;
            set
            {
                _selectionHeight = value;
                OnPropertyChanged();
            }
        }

        // Пример: метод, который может дергать code-behind по событию InkSelectArea_Click
        public void StartSelection()
        {
            IsSelectingArea = true;
            // Можно обнулить старые данные
            SelectionX = 0;
            SelectionY = 0;
            SelectionWidth = 0;
            SelectionHeight = 0;
        }

        public void UpdateSelection(Point startPoint, Point currentPoint)
        {
            // Логика, как считать X,Y,W,H
            var x = Math.Min(startPoint.X, currentPoint.X);
            var y = Math.Min(startPoint.Y, currentPoint.Y);
            var w = Math.Abs(currentPoint.X - startPoint.X);
            var h = Math.Abs(currentPoint.Y - startPoint.Y);

            SelectionX = (int)x;
            SelectionY = (int)y;
            SelectionWidth = (int)w;
            SelectionHeight = (int)h;
        }

        public void EndSelection()
        {
            IsSelectingArea = false;
        }
        #endregion


        #region Пример команд (необязательно)

        // Если хотите, чтобы кнопка "Open Image File" была связана с командой
        //public ICommand OpenFileCommand { get; }
        //public void OpenImage() { ... }

        //public ICommand SaveFileCommand { get; }
        //public void SaveImage() { ... }

        #endregion

        #region Рисование кистью

        private bool _isDrawing;
        public bool IsDrawing
        {
            get => _isDrawing;
            set { _isDrawing = value; OnPropertyChanged(); }
        }

        private Color _currentColor = Colors.Black;
        public Color CurrentColor
        {
            get => _currentColor;
            set { _currentColor = value; OnPropertyChanged(); }
        }

        private double _currentThickness = 2;
        public double CurrentThickness
        {
            get => _currentThickness;
            set { _currentThickness = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Начать линию (обычно при PointerPressed).
        /// </summary>
        public void StartLine()
        {
            if (IsDrawing)
            {
                _model.StartLine(CurrentColor, CurrentThickness);
            }
        }

        /// <summary>
        /// Добавлять точки (PointerMoved).
        /// </summary>
        public void AddPointToLine(Point p)
        {
            if (IsDrawing)
            {
                _model.AddPointToLine(p);
            }
        }
        #endregion

        #region Добавление текста
        private bool _isAddingText;
        public bool IsAddingText
        {
            get => _isAddingText;
            set { _isAddingText = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Текст, который пользователь вводит.
        /// </summary>
        private string _userText = "Введите текст...";
        public string UserText
        {
            get => _userText;
            set { _userText = value; OnPropertyChanged(); }
        }

        private double _textFontSize = 16;
        public double TextFontSize
        {
            get => _textFontSize;
            set { _textFontSize = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// При клике на Canvas добавим текст в модель.
        /// </summary>
        public void AddText(Point position)
        {
            if (IsAddingText && !string.IsNullOrEmpty(UserText))
            {
                _model.AddText(position, UserText, CurrentColor, TextFontSize);
            }
        }
        #endregion


    }

    /// <summary>
    /// Простейшая реализация команды.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
