using Avalonia.Media.Imaging;
using Avalonia.Media;
using Avalonia;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Laba4.Operations
{
    public class AddingTextToImage
    {

        public static RenderTargetBitmap AddTextToImage(Bitmap bitmap,string currentText, Avalonia.Point currentTextPosition)
        {
            if (bitmap == null)
                return null;

            // Получаем размеры изображения
            var width = bitmap.PixelSize.Width;
            var height = bitmap.PixelSize.Height;

            PixelSize pixelSize = new PixelSize(width, height);

            // Создаем RenderTargetBitmap для комбинированного изображения
            var renderBitmap = new RenderTargetBitmap(pixelSize);
            using (var context = renderBitmap.CreateDrawingContext(true))
            {
                // Рисуем исходное изображение
                context.DrawImage(bitmap, new Rect(0, 0, width, height));

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


    }
}
