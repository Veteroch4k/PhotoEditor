using Avalonia.Media.Imaging;
using System.IO;



namespace Laba4.Operations
{
    public class SavingImageToFile
    {

        public static void SaveImage(Bitmap bitmap,string path)
        {
            if (bitmap == null) return;

            using var fs = File.Create(path);
            bitmap.Save(fs);
        }

    }
}
