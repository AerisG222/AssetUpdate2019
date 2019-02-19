using System;
using System.Linq;
using System.Threading.Tasks;
using AssetUpdate2019.Data;


namespace AssetUpdate2019
{
    class ThumbnailProcess
    {
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

            gen.Generate();
        }


        void GenerateVideoThumbnail(string sourceFile)
        {

        }
    }
}
