using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NMagickWand;
using AssetUpdate2019.Data;


namespace AssetUpdate2019
{
    class ThumbnailProcess
    {
        const string FfmpegPath = "ffmpeg";

        readonly Storage _storage;
        readonly ParallelOptions _parallelOpts;


        public ThumbnailProcess(Storage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));

            int vpus = Math.Max(Environment.ProcessorCount - 1, 1);

            _parallelOpts = new ParallelOptions { MaxDegreeOfParallelism = vpus };
        }


        public void CreateNewPhotoThumbnails()
        {
            var files = _storage.GetPhotoFiles()
                .Where(p => p.IndexOf("/lg/", StringComparison.OrdinalIgnoreCase) > -1);

            Parallel.ForEach(files, _parallelOpts, GeneratePhotoThumbnail);
        }


        public void CreateNewVideoThumbnails()
        {
            var files = _storage.GetVideoFiles()
                .Where(p => p.IndexOf("/raw/", StringComparison.OrdinalIgnoreCase) > -1);

            Parallel.ForEach(files, _parallelOpts, GenerateVideoThumbnail);
        }


        void GeneratePhotoThumbnail(string sourceFile)
        {
            var dest = sourceFile.Replace("/lg/", "/xs_sq/", StringComparison.OrdinalIgnoreCase);
            var gen = new SquareThumbnailGenerator(sourceFile, dest);

            Directory.CreateDirectory(Path.GetDirectoryName(dest));

            gen.Generate();
        }


        void GenerateVideoThumbnail(string sourceFile)
        {
            var destSq = sourceFile.Replace("/raw/", "/thumb_sq/", StringComparison.OrdinalIgnoreCase);
            var destThumb = sourceFile.Replace("/raw/", "/thumbnails/", StringComparison.OrdinalIgnoreCase);

            Directory.CreateDirectory(Path.GetDirectoryName(destSq));

            destSq = Path.Combine(Path.GetDirectoryName(destSq), Path.GetFileNameWithoutExtension(destSq) + ".jpg");
            destThumb = Path.Combine(Path.GetDirectoryName(destThumb), Path.GetFileNameWithoutExtension(destThumb) + ".jpg");

            RegenerateVideoThumbnail(sourceFile, destThumb);
            DumpImageFromVideo(sourceFile, destSq);

            var gen = new SquareThumbnailGenerator(destSq, destSq);

            gen.Generate();

            DeleteOriginalPngThumbnail(destThumb);
        }


        void RegenerateVideoThumbnail(string sourceFile, string destThumb)
        {
            DumpImageFromVideo(sourceFile, destThumb);

            var width = 240;
			var height = 160;

            using(var wand = new MagickWand(destThumb))
            {
                float idealAspect = (float)width / (float)height;
                float actualAspect = (float)wand.ImageWidth / (float)wand.ImageHeight;

                if(idealAspect >= actualAspect)
                {
                    width = (int)(actualAspect * (float)height);
                }
                else
                {
                    height = (int)((float)width / actualAspect);
                }

                wand.ScaleImage((uint)width, (uint)height);

                // sharpen after potentially resizing
                // http://www.imagemagick.org/Usage/resize/#resize_unsharp
                wand.UnsharpMaskImage(0, 0.7, 0.7, 0.008);

                wand.WriteImage(destThumb, true);
            }
        }


        void DeleteOriginalPngThumbnail(string destThumb)
        {
            var oldThumb = destThumb.Replace(".jpg", ".png");

            if(File.Exists(oldThumb))
            {
                File.Delete(oldThumb);
            }
        }


        void DumpImageFromVideo(string videoFile, string imageFile)
        {
            var args = string.Concat("-y -i \"", videoFile, "\" -ss 00:00:02 -vframes 1 \"", imageFile, "\"");

            ExecuteFfmpeg(args);
        }


        void ExecuteFfmpeg(string arguments)
        {
            Process ffmpeg = null;

            try
            {
                // capture the output, otherwise it won't make sense w/ many processes writing to stdout
                ffmpeg = new Process();

                ffmpeg.StartInfo.FileName = FfmpegPath;
                ffmpeg.StartInfo.Arguments = arguments;
                ffmpeg.StartInfo.UseShellExecute = false;
                ffmpeg.StartInfo.RedirectStandardOutput = true;
                ffmpeg.StartInfo.RedirectStandardError = true;
                ffmpeg.Start();

                ffmpeg.StandardOutput.ReadToEnd();

                ffmpeg.WaitForExit();
            }
            finally
            {
                ffmpeg.Dispose();
            }
        }
    }
}
