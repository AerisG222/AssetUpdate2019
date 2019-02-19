using System;
using System.Linq;
using System.Threading.Tasks;
using NMagickWand;
using AssetUpdate2019.Data;


namespace AssetUpdate2019
{
    class Program
    {
        readonly Repository _repo;
        readonly Storage _storage;
        readonly ThumbnailProcess _thumbnailProcess;


        Program(string connString, string photoRoot, string videoRoot)
        {
            _repo = new Repository(connString);
            _storage = new Storage(photoRoot, videoRoot);
            _thumbnailProcess = new ThumbnailProcess(_storage);
        }


        static async Task Main(string[] args)
        {
            if(args.Length == 3)
            {
                var p = new Program(args[0], args[1], args[2]);

                await p.ExecuteAsync();
            }
            else
            {
                Console.WriteLine("Please correct commandline arguments");
            }
        }


        async Task ExecuteAsync()
        {
            MagickWandEnvironment.Genesis();

            // Console.WriteLine("Generating new thumbnails for photos...");
            // _thumbnailProcess.CreateNewPhotoThumbnails();

            Console.WriteLine("Generating new thumbnails for videos...");
            _thumbnailProcess.CreateNewVideoThumbnails();

            // var photos = await _repo.GetPhotosAsync();
            var videos = await _repo.GetVideosAsync();

            // Console.WriteLine($"Found {photos.Count()} photos in db");
            Console.WriteLine($"Found {videos.Count()} videos in db");

            // var photoFiles = _storage.GetPhotos();
            // var videoFiles = _storage.GetVideos();

            MagickWandEnvironment.Terminus();
        }
    }
}
