using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;


namespace Laba4.Operations
{
    public class CroppingImage
    {

        public static Bitmap CropImage(Bitmap bitmap, int crpX, int crpY, int rectW, int rectH) // ширина и высота выделенной области
        {
            // Проверяем входные параметры: если изображение null или ширина/высота <=0, выходим из метода
            if (bitmap == null || rectW <= 0 || rectH <= 0)
                return null;

            // Локальные переменные для хранения координат и размеров области обрезки
            int x = crpX; // Координата X верхнего левого угла
            int y = crpY; // Координата Y верхнего левого угла
            int w = rectW; // Ширина области обрезки
            int h = rectH; // Высота области обрезки

            // Проверка границ области обрезки
            // Если X координата отрицательная (обрезка за левой границей), корректируем ее и ширину
            if (x < 0) { w += x; x = 0; }
            // Если Y координата отрицательная (обрезка за верхней границей), корректируем ее и высоту
            if (y < 0) { h += y; y = 0; }
            // Если X координата + ширина выходит за правую границу изображения, корректируем ширину
            if (x + w > bitmap.PixelSize.Width) w = bitmap.PixelSize.Width - x;
            // Если Y координата + высота выходит за нижнюю границу изображения, корректируем высоту
            if (y + h > bitmap.PixelSize.Height) h = bitmap.PixelSize.Height - y;

            // Если после корректировок ширина или высота стали <= 0, выходим (нечего обрезать)
            if (w <= 0 || h <= 0) return null;

            // для хранения обрезанного фрагмента
            var croppedBitmap = new WriteableBitmap(
               new PixelSize(w, h), 
               new Avalonia.Vector(96, 96), // Разрешение нового изображения
               Avalonia.Platform.PixelFormat.Bgra8888, // Формат пикселей (32-битный RGBA)
               AlphaFormat.Unpremul); 

            // (получаем доступ к памяти пикселей)
            using (var destData = croppedBitmap.Lock())
            {
                // w * h * 4 - общее количество байт для копирования (ширина * высота * 4 байта на пиксель)
                bitmap.CopyPixels(new PixelRect(x, y, w, h), destData.Address, w * h * 4, w * 4);
            }

            return croppedBitmap;

        }
    }
}
