using System;
using System.Linq;
using System.Threading.Tasks;
using AssetUpdate2019.Data;
using NMagickWand;

namespace AssetUpdate2019
{
    class Program
    {
        Repository _repo;
        Storage _storage;


        Program(string connString, string photoRoot, string videoRoot)
        {
            _repo = new Repository(connString);
            _storage = new Storage(photoRoot, videoRoot);
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

            var photos = await _repo.GetPhotosAsync();
            var videos = await _repo.GetVideosAsync();

            Console.WriteLine($"Found {photos.Count()} photos in db");
            Console.WriteLine($"Found {videos.Count()} videos in db");

            var photoFiles = _storage.GetPhotos();
            var videoFiles = _storage.GetVideos();

            MagickWandEnvironment.Terminus();
        }
    }
}
