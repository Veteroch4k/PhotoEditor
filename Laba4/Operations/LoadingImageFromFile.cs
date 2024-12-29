using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Laba4.Operations
{
    public class LoadingImageFromFile
    {

        public static Bitmap LoadFromFile(string path)
        {
            if (!File.Exists(path))
                return null;

            return new Bitmap(path);
        }

    }
}
