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


        Program(string connString)
        {
            _repo = new Repository(connString);
        }


        static async Task Main(string[] args)
        {
            if(args.Length > 0)
            {
                var p = new Program(args[0]);

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

            Console.WriteLine($"Found {photos.Count()} photos");
            Console.WriteLine($"Found {videos.Count()} videos");

            MagickWandEnvironment.Terminus();
        }
    }
}
