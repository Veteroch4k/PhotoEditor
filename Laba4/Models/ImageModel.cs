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
using System.Drawing.Imaging;

namespace Laba4.Models
{
    public class ImageModel
    {

        // Текущее изображение в формате Avalonia Bitmap.
        public Bitmap? CurrentBitmap { get;  set; }
        public Bitmap? CurrentBitmapCopy { get;  set; }


    }
}
