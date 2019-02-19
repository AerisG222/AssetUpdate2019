using System;
using System.IO;
using NMagickWand;


namespace AssetUpdate2019
{
    class SquareThumbnailGenerator
    {
        const uint FinalWidth = 160;
        const uint FinalHeight = 120;
        const float Aspect = 160 / 120;

        readonly string _sourcePath;
        readonly string _destPath;


        public SquareThumbnailGenerator(string sourcePath, string destPath)
        {
            if(string.IsNullOrWhiteSpace(sourcePath))
            {
                throw new ArgumentNullException(nameof(sourcePath));
            }

            if(string.IsNullOrWhiteSpace(destPath))
            {
                throw new ArgumentNullException(nameof(destPath));
            }

            _sourcePath = sourcePath;
            _destPath = destPath;
        }


        void Generate()
        {
            using(var wand = new MagickWand(_sourcePath))
            {
                var width = wand.ImageWidth;
                var height = wand.ImageHeight;
                var aspect = (double)width / (double)height;

                if(aspect >= Aspect)
                {
                    uint newWidth = (width / height) * FinalHeight;

                    // scale image to final height
                    wand.ScaleImage(newWidth, FinalHeight);

                    // crop sides as needed
                    wand.CropImage(FinalWidth, FinalHeight, (int) (newWidth - FinalWidth) / 2, 0);
                }
                else
                {
                    uint newHeight = FinalWidth / (width / height);

                    // scale image to final width
                    wand.ScaleImage(FinalWidth, newHeight);

                    // crop top and bottom as needed
                    wand.CropImage(FinalWidth, FinalHeight, 0, (int) (newHeight - FinalHeight) / 2);
                }

                // sharpen after potentially resizing
                // http://www.imagemagick.org/Usage/resize/#resize_unsharp
                wand.UnsharpMaskImage(0, 0.7, 0.7, 0.008);

                Directory.CreateDirectory(Path.GetDirectoryName(_destPath));

                wand.WriteImage(_destPath, true);
            }
        }
    }
}