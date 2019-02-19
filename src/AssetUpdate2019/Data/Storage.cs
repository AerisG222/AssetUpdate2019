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

            Console.WriteLine("Assembling photos...");

            var photos = AssemblePhotos();

            Console.WriteLine($"Finished assembling {photos.Count()} photos");

            return photos;
        }


        public IEnumerable<Video> GetVideos()
        {
            Console.WriteLine("Getting videos");

            GetVideoMedia();

            Console.WriteLine();
            Console.WriteLine($"Finished.  Found {_videoList.Count} files");

            Console.WriteLine("Assembling photos...");

            var videos = AssembleVideos();

            Console.WriteLine($"Finished assembling {videos.Count()} videos");

            return videos;
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
            // let's rely on dimensions from db for movies, probably should have done that for images too...
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


        IEnumerable<Photo> AssemblePhotos()
        {
            var xsMedia =   _photoList.Where(x => x.Path.IndexOf("/xs/", StringComparison.OrdinalIgnoreCase) > -1).ToDictionary(x => x.Path);
            var xsSqMedia = _photoList.Where(x => x.Path.IndexOf("/xs_sq/", StringComparison.OrdinalIgnoreCase) > -1).ToDictionary(x => x.Path);
            var smMedia =   _photoList.Where(x => x.Path.IndexOf("/sm/", StringComparison.OrdinalIgnoreCase) > -1).ToDictionary(x => x.Path);
            var mdMedia =   _photoList.Where(x => x.Path.IndexOf("/md/", StringComparison.OrdinalIgnoreCase) > -1).ToDictionary(x => x.Path);
            var lgMedia =   _photoList.Where(x => x.Path.IndexOf("/lg/", StringComparison.OrdinalIgnoreCase) > -1).ToDictionary(x => x.Path);
            var prtMedia = _photoList.Where(x => x.Path.IndexOf("/prt/", StringComparison.OrdinalIgnoreCase) > -1).ToDictionary(x => x.Path);
            var srcMedia = _photoList.Where(x => x.Path.IndexOf("/src/", StringComparison.OrdinalIgnoreCase) > -1).ToDictionary(x => x.Path);

            Console.WriteLine("Verify that the following counts match:");
            Console.WriteLine($"xs: {xsMedia.Count}");
            Console.WriteLine($"xs_sq: {xsSqMedia.Count}");
            Console.WriteLine($"sm: {smMedia.Count}");
            Console.WriteLine($"md: {mdMedia.Count}");
            Console.WriteLine($"lg: {lgMedia.Count}");
            Console.WriteLine($"prt: {prtMedia.Count}");
            Console.WriteLine($"src: {srcMedia.Count}");

            var photos = xsMedia.Keys.Select(key => {
                Media xsSq = null;
                Media sm = null;
                Media md = null;
                Media lg = null;
                Media prt = null;
                Media src = null;

                xsSqMedia.TryGetValue(key.Replace("/xs/", "/xs_sq/"), out xsSq);
                smMedia.TryGetValue(key.Replace("/xs/", "/sm/"), out sm);
                mdMedia.TryGetValue(key.Replace("/xs/", "/md/"), out md);
                lgMedia.TryGetValue(key.Replace("/xs/", "/lg/"), out lg);
                prtMedia.TryGetValue(key.Replace("/xs/", "/prt/"), out prt);
                srcMedia.TryGetValue(key.Replace("/xs/", "/src/"), out src);

                return new Photo {
                    MediaXs = xsMedia[key],
                    MediaXsSq = xsSq,
                    MediaSm = sm,
                    MediaMd = md,
                    MediaLg = lg,
                    MediaPrt = prt,
                    MediaSrc = src,
                };
            });

            return photos;
        }


        IEnumerable<Video> AssembleVideos()
        {
            var thumbMedia =   _videoList.Where(x => x.Path.IndexOf("/thumbnails/", StringComparison.OrdinalIgnoreCase) > -1).ToDictionary(x => x.Path);
            var thumbSqMedia = _videoList.Where(x => x.Path.IndexOf("/thumb_sq/", StringComparison.OrdinalIgnoreCase) > -1).ToDictionary(x => x.Path);
            var scaledMedia =  _videoList.Where(x => x.Path.IndexOf("/scaled/", StringComparison.OrdinalIgnoreCase) > -1).ToDictionary(x => x.Path);
            var fullMedia =    _videoList.Where(x => x.Path.IndexOf("/full/", StringComparison.OrdinalIgnoreCase) > -1).ToDictionary(x => x.Path);
            var rawMedia =     _videoList.Where(x => x.Path.IndexOf("/raw/", StringComparison.OrdinalIgnoreCase) > -1).ToDictionary(x => x.Path);

            Console.WriteLine("Verify that the following counts match:");
            Console.WriteLine($"thumb: {thumbMedia.Count}");
            Console.WriteLine($"thumb_sq: {thumbSqMedia.Count}");
            Console.WriteLine($"scaled: {scaledMedia.Count}");
            Console.WriteLine($"full: {fullMedia.Count}");
            Console.WriteLine($"raw: {rawMedia.Count}");

            var videos = thumbMedia.Keys.Select(key => {
                Media thumbSq = null;
                Media scaled = null;
                Media full = null;
                Media raw = null;

                thumbSqMedia.TryGetValue(key.Replace("/thumbnails/", "/thumb_sq/"), out thumbSq);
                scaledMedia.TryGetValue(key.Replace("/thumbnails/", "/scaled/"), out scaled);
                fullMedia.TryGetValue(key.Replace("/thumbnails/", "/full/"), out full);
                rawMedia.TryGetValue(key.Replace("/thumbnails/", "/raw/"), out raw);

                return new Video {
                    MediaThumbnail = thumbMedia[key],
                    MediaThumbnailSq = thumbSq,
                    MediaScaled = scaled,
                    MediaFullsize = full,
                    MediaRaw = raw
                };
            });

            return videos;
        }
    }
}
