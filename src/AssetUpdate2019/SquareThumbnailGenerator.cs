using System;
using System.IO;
using System.Threading.Tasks;
using NJpegOptim;
using NJpegTran;
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


        public void Generate()
        {
            using(var wand = new MagickWand(_sourcePath))
            {
                var width = (double)wand.ImageWidth;
                var height = (double)wand.ImageHeight;
                var aspect = width / height;

                if(aspect >= Aspect)
                {
                    var newWidth = (width / height) * FinalHeight;

                    // scale image to final height
                    wand.ScaleImage((uint) newWidth, FinalHeight);

                    // crop sides as needed
                    wand.CropImage(FinalWidth, FinalHeight, (int) (newWidth - FinalWidth) / 2, 0);
                }
                else
                {
                    var newHeight = FinalWidth / (width / height);

                    // scale image to final width
                    wand.ScaleImage(FinalWidth, (uint) newHeight);

                    // crop top and bottom as needed
                    wand.CropImage(FinalWidth, FinalHeight, 0, (int) (newHeight - FinalHeight) / 2);
                }

                // sharpen after potentially resizing
                // http://www.imagemagick.org/Usage/resize/#resize_unsharp
                wand.UnsharpMaskImage(0, 0.7, 0.7, 0.008);

                Directory.CreateDirectory(Path.GetDirectoryName(_destPath));

                wand.WriteImage(_destPath, true);
            }

            ExecuteJpegOptimAsync(_destPath);
            ExecuteJpegTranAsync(_destPath);
        }


        NJpegOptim.Result ExecuteJpegOptimAsync(string srcPath)
        {
            var opts = new NJpegOptim.Options {
                StripProperties = StripProperty.All,
                ProgressiveMode = ProgressiveMode.ForceProgressive,
                MaxQuality = 72,
                OutputToStream = true
            };

            var jo = new JpegOptim(opts);

            // ideally wouldn't block here but am being lazy
            return jo.RunAsync(srcPath).Result;
        }


        NJpegTran.Result ExecuteJpegTranAsync(string dstPath)
        {
            var opts = new NJpegTran.Options {
                Optimize = true,
                Copy = Copy.None
            };

            var jt = new JpegTran(opts);

            // ideally wouldn't block here but am being lazy
            return jt.RunAsync(dstPath, dstPath).Result;
        }
    }
}