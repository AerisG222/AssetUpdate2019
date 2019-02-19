using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NMagickWand;


namespace AssetUpdate2019.Data
{
    class Storage
    {
        readonly object _lockObj = new object();
        readonly ParallelOptions _parallelOpts;
        readonly string _photoRoot;
        readonly string _videoRoot;
        readonly List<Media> _photoList = new List<Media>();
        readonly List<Media> _videoList = new List<Media>();


        public Storage(string photoRoot, string videoRoot)
        {
            if(string.IsNullOrWhiteSpace(photoRoot))
            {
                throw new ArgumentNullException(nameof(photoRoot));
            }

            if(string.IsNullOrWhiteSpace(videoRoot))
            {
                throw new ArgumentNullException(nameof(videoRoot));
            }

            _photoRoot = photoRoot;
            _videoRoot = videoRoot;

            int vpus = Math.Max(Environment.ProcessorCount - 1, 1);

            _parallelOpts = new ParallelOptions { MaxDegreeOfParallelism = vpus };
        }


        public IEnumerable<Photo> GetPhotos()
        {
            Console.WriteLine("Getting photos");

            GetPhotoMedia();

            Console.WriteLine();
            Console.WriteLine($"Finished.  Found {_photoList.Count} files");

            return null;
        }


        public IEnumerable<Video> GetVideos()
        {
            Console.WriteLine("Getting videos");

            GetVideoMedia();

            Console.WriteLine();
            Console.WriteLine($"Finished.  Found {_videoList.Count} files");

            return null;
        }


        public IEnumerable<string> GetPhotoFiles()
        {
            return GetFiles(_photoRoot);
        }


        public IEnumerable<string> GetVideoFiles()
        {
            return GetFiles(_videoRoot);
        }


        void GetPhotoMedia()
        {
            var files = GetPhotoFiles();

            Parallel.ForEach(files, _parallelOpts, PopulatePhotoMedia);
        }


        void GetVideoMedia()
        {
            var files = GetVideoFiles();

            Parallel.ForEach(files, _parallelOpts, PopulateVideoMedia);
        }


        void PopulatePhotoMedia(string path)
        {
            var media = BuildMedia(path);

            PopulateImageProperties(path, media);

            lock(_lockObj)
            {
                _photoList.Add(media);

                if(_photoList.Count % 2000 == 0)
                {
                    Console.Write('.');
                }
            }
        }


        void PopulateVideoMedia(string path)
        {
            var media = BuildMedia(path);

            if(path.EndsWith("jpg", StringComparison.OrdinalIgnoreCase))
            {
                PopulateImageProperties(path, media);
            }
            else
            {
                PopulateVideoProperties(path, media);
            }

            lock(_lockObj)
            {
                _videoList.Add(media);

                if(_photoList.Count % 100 == 0)
                {
                    Console.Write('.');
                }
            }
        }


        void PopulateImageProperties(string file, Media media)
        {
            using(var wand = new MagickWand(media.Path))
            {
                media.Height = (int) wand.ImageHeight;
                media.Width = (int) wand.ImageWidth;
            }
        }


        void PopulateVideoProperties(string file, Media media)
        {

        }


        Media BuildMedia(string file)
        {
            var fi = new FileInfo(file);

            var media = new Media {
                Path = file,
                Size = fi.Length
            };

            return media;
        }


        IEnumerable<string> GetFiles(string path)
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
        }
    }
}
