using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NExifTool;
using AssetUpdate2019.Data;


namespace AssetUpdate2019
{
    class VideoMetadataGatherer
    {
        readonly ExifTool _exifTool = new ExifTool(new ExifToolOptions());
        readonly ParallelOptions _parallelOpts;


        public VideoMetadataGatherer()
        {
            int vpus = Math.Max(Environment.ProcessorCount - 1, 1);

            _parallelOpts = new ParallelOptions { MaxDegreeOfParallelism = vpus };
        }


        public void Gather(IEnumerable<Video> videos)
        {
            Parallel.ForEach(videos, _parallelOpts, PopulateVideoMetadata);
        }


        void PopulateVideoMetadata(Video video)
        {
            if(video.MediaRaw != null && !string.IsNullOrWhiteSpace(video.MediaRaw.Path))
            {
                var tags = _exifTool.GetTagsAsync(video.MediaRaw.Path).Result;

                video.CreateDate = tags.SingleOrDefaultPrimaryTag("CreateDate")?.TryGetDateTime();
                video.Latitude = tags.SingleOrDefaultPrimaryTag("GPSLatitude")?.TryGetDouble();
                video.LatitudeRef = tags.SingleOrDefaultPrimaryTag("GPSLatitudeRef")?.Value?.Substring(0, 1);
                video.Longitude = tags.SingleOrDefaultPrimaryTag("GPSLongitude")?.TryGetDouble();
                video.LongitudeRef = tags.SingleOrDefaultPrimaryTag("GPSLongitudeRef")?.Value?.Substring(0, 1);
            }
        }
    }
}
