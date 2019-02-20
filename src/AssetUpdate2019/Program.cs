using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NMagickWand;
using AssetUpdate2019.Data;


namespace AssetUpdate2019
{
    class Program
    {
        readonly string _outputFile;
        readonly Repository _repo;
        readonly Storage _storage;
        readonly ThumbnailProcess _thumbnailProcess;
        readonly VideoMetadataGatherer _videoMetadataGatherer;


        Program(string connString, string photoRoot, string videoRoot, string outputFile)
        {
            _outputFile = outputFile;
            _repo = new Repository(connString);
            _storage = new Storage(photoRoot, videoRoot);
            _thumbnailProcess = new ThumbnailProcess(_storage);
            _videoMetadataGatherer = new VideoMetadataGatherer();
        }


        static async Task Main(string[] args)
        {
            if(args.Length == 4)
            {
                var p = new Program(args[0], args[1], args[2], args[3]);

                await p.ExecuteAsync();
            }
            else
            {
                Console.WriteLine("Please correct commandline arguments");
            }
        }


        Task ExecuteAsync()
        {
            MagickWandEnvironment.Genesis();

            Console.WriteLine("Generating new thumbnails for photos...");
            _thumbnailProcess.CreateNewPhotoThumbnails();

            Console.WriteLine("Generating new thumbnails (and replacing old pngs with jpgs) for videos...");
            _thumbnailProcess.CreateNewVideoThumbnails();

            // var photos = await _repo.GetPhotosAsync();
            // var videos = await _repo.GetVideosAsync();

            // Console.WriteLine($"Found {photos.Count()} photos in db");
            // Console.WriteLine($"Found {videos.Count()} videos in db");

            var photoFiles = _storage.GetPhotos();
            var videoFiles = _storage.GetVideos();

            Console.WriteLine($"Found {photoFiles.Count()} photos on filesystem");
            Console.WriteLine($"Found {videoFiles.Count()} videos on filesystem");

            Console.WriteLine("Trying to gather additional metadata for video files...");
            _videoMetadataGatherer.Gather(videoFiles);
            Console.WriteLine("Finished gathering additional metadata for video files.");

            WriteSqlScript(photoFiles, videoFiles);

            MagickWandEnvironment.Terminus();

            return Task.CompletedTask;
        }


        void WriteSqlScript(IEnumerable<Photo> photos, IEnumerable<Video> videos)
        {
            var sep = "-- -------------------------------------------";

            using(var sw = new StreamWriter(File.OpenWrite(_outputFile)))
            {
                sw.WriteLine(sep);
                sw.WriteLine("-- create indexes to improve update performance");
                sw.WriteLine(sep);

                sw.WriteLine(@"
                    DO
                    $$
                    BEGIN
                        IF NOT EXISTS (SELECT 1
                                        FROM pg_catalog.pg_indexes
                                        WHERE schemaname = 'photo'
                                        AND tablename = 'photo'
                                        AND indexname = 'ix_photo_photo_xs_path') THEN

                            CREATE INDEX ix_photo_photo_xs_path
                                ON photo.photo(xs_path);

                        END IF;
                    END
                    $$;"
                );

                sw.WriteLine(@"
                    DO
                    $$
                    BEGIN
                        IF NOT EXISTS (SELECT 1
                                        FROM pg_catalog.pg_indexes
                                        WHERE schemaname = 'video'
                                        AND tablename = 'video'
                                        AND indexname = 'ix_video_video_thumb_path') THEN

                            CREATE INDEX ix_video_video_thumb_path
                                ON video.video(thumb_path);

                        END IF;
                    END
                    $$;"
                );

                sw.WriteLine(sep);
                sw.WriteLine("-- add photo sizes and new thumbnail details");
                sw.WriteLine(sep);

                foreach(var photo in photos)
                {
                    sw.WriteLine(
                        $"UPDATE photo.photo " +
                        $"   SET xs_size = { SqlNumber(photo.MediaXs.Size) }, " +
                        $"       sm_size = { SqlNumber(photo.MediaSm?.Size) }, " +
                        $"       md_size = { SqlNumber(photo.MediaMd?.Size) }, " +
                        $"       lg_size = { SqlNumber(photo.MediaLg?.Size) }, " +
                        $"       prt_size = { SqlNumber(photo.MediaPrt?.Size) }, " +
                        $"       src_size = { SqlNumber(photo.MediaSrc?.Size) }, " +
                        $"       xs_sq_height = { SqlNumber(photo.MediaXsSq?.Height) }, " +
                        $"       xs_sq_width = { SqlNumber(photo.MediaXsSq?.Width) }, " +
                        $"       xs_sq_path = { SqlString(photo.MediaXsSq?.Path) }, " +
                        $"       xs_sq_size = { SqlNumber(photo.MediaXsSq?.Size) } " +
                        $" WHERE xs_path = { SqlString(GetPhotoWebPath(photo.MediaXs.Path)) };");
                }

                sw.WriteLine(sep);
                sw.WriteLine("-- add video sizes, gps, create_date, and new thumbnail details");
                sw.WriteLine(sep);

                foreach(var video in videos)
                {
                    sw.WriteLine(
                        $"UPDATE video.video " +
                        $"   SET thumb_size = { SqlNumber(video.MediaThumbnail.Size) }, " +
                        $"       scaled_size = { SqlNumber(video.MediaScaled?.Size) }, " +
                        $"       full_size = { SqlNumber(video.MediaFullsize?.Size) }, " +
                        $"       raw_size = { SqlNumber(video.MediaRaw?.Size) }, " +
                        $"       xs_sq_height = { SqlNumber(video.MediaThumbnailSq?.Height) }, " +
                        $"       xs_sq_width = { SqlNumber(video.MediaThumbnailSq?.Width) }, " +
                        $"       xs_sq_path = { SqlString(video.MediaThumbnailSq?.Path) }, " +
                        $"       xs_sq_size = { SqlNumber(video.MediaThumbnailSq?.Size) }, " +
                        $"       gps_latitude = { SqlNumber(video.Latitude) }, " +
                        $"       gps_latitude_ref_id = { SqlString(video.LatitudeRef) }, " +
                        $"       gps_longitude = { SqlNumber(video.Longitude) }, " +
                        $"       gps_longitude_ref_id = { SqlString(video.LongitudeRef) }, " +
                        $"       create_date = { SqlTimestamp(video.CreateDate) } " +
                        $" WHERE thumb_path = { SqlString(GetVideoWebPath(video.MediaThumbnail.Path)) };");
                }

                sw.WriteLine(sep);
                sw.WriteLine("-- bulk update photo category details");
                sw.WriteLine(sep);

                sw.WriteLine(
                    "UPDATE photo.category c " +
                    "   SET photo_count = (SELECT COUNT(1) FROM photo.photo WHERE category_id = c.id), " +
                    "       create_date = (SELECT create_date FROM photo.photo WHERE id = (SELECT MIN(id) FROM photo.photo where category_id = c.id AND create_date IS NOT NULL)), " +
                    "       gps_latitude = (SELECT gps_latitude FROM photo.photo WHERE id = (SELECT MIN(id) FROM photo.photo WHERE category_id = c.id AND gps_latitude IS NOT NULL)), " +
                    "       gps_latitude_ref_id = (SELECT gps_latitude_ref_id FROM photo.photo WHERE id = (SELECT MIN(id) FROM photo.photo WHERE category_id = c.id AND gps_latitude IS NOT NULL)), " +
                    "       gps_longitude = (SELECT gps_longitude FROM photo.photo WHERE id = (SELECT MIN(id) FROM photo.photo WHERE category_id = c.id AND gps_latitude IS NOT NULL)), " +
                    "       gps_longitude_ref_id = (SELECT gps_longitude_ref_id FROM photo.photo WHERE id = (SELECT MIN(id) FROM photo.photo WHERE category_id = c.id AND gps_latitude IS NOT NULL)), " +
                    "       total_size_xs = (SELECT SUM(xs_size) FROM photo.photo WHERE category_id = c.id), " +
                    "       total_size_xs_sq = (SELECT SUM(xs_sq_size) FROM photo.photo WHERE category_id = c.id), " +
                    "       total_size_sm = (SELECT SUM(sm_size) FROM photo.photo WHERE category_id = c.id), " +
                    "       total_size_md = (SELECT SUM(md_size) FROM photo.photo WHERE category_id = c.id), " +
                    "       total_size_lg = (SELECT SUM(lg_size) FROM photo.photo WHERE category_id = c.id), " +
                    "       total_size_prt = (SELECT SUM(prt_size) FROM photo.photo WHERE category_id = c.id), " +
                    "       total_size_src = (SELECT SUM(src_size) FROM photo.photo WHERE category_id = c.id), " +
                    "       teaser_photo_size = (SELECT xs_size FROM photo.photo WHERE category_id = c.id AND xs_path = c.teaser_photo_path), " +
                    "       teaser_photo_sq_height = (SELECT xs_sq_height FROM photo.photo WHERE category_id = c.id AND xs_path = c.teaser_photo_path), " +
                    "       teaser_photo_sq_width = (SELECT xs_sq_width FROM photo.photo WHERE category_id = c.id AND xs_path = c.teaser_photo_path), " +
                    "       teaser_photo_sq_path = (SELECT xs_sq_path FROM photo.photo WHERE category_id = c.id AND xs_path = c.teaser_photo_path), " +
                    "       teaser_photo_sq_size = (SELECT xs_sq_size FROM photo.photo WHERE category_id = c.id AND xs_path = c.teaser_photo_path);"
                );

                sw.WriteLine(sep);
                sw.WriteLine("-- bulk update video category details");
                sw.WriteLine(sep);

                sw.WriteLine(
                    "UPDATE video.category c " +
                    "   SET video_count = (SELECT COUNT(1) FROM video.video WHERE category_id = c.id), " +
                    "       create_date = (SELECT create_date FROM video.video WHERE id = (SELECT MIN(id) FROM video.video where category_id = c.id AND create_date IS NOT NULL)), " +
                    "       gps_latitude = (SELECT gps_latitude FROM video.video WHERE id = (SELECT MIN(id) FROM video.video WHERE category_id = c.id AND gps_latitude IS NOT NULL)), " +
                    "       gps_latitude_ref_id = (SELECT gps_latitude_ref_id FROM video.video WHERE id = (SELECT MIN(id) FROM video.video WHERE category_id = c.id AND gps_latitude IS NOT NULL)), " +
                    "       gps_longitude = (SELECT gps_longitude FROM video.video WHERE id = (SELECT MIN(id) FROM video.video WHERE category_id = c.id AND gps_latitude IS NOT NULL)), " +
                    "       gps_longitude_ref_id = (SELECT gps_longitude_ref_id FROM video.video WHERE id = (SELECT MIN(id) FROM video.video WHERE category_id = c.id AND gps_latitude IS NOT NULL)), " +
                    "       total_duration = (SELECT SUM(duration) FROM video.video WHERE category_id = c.id), " +
                    "       total_size_thumb = (SELECT SUM(thumb_size) FROM video.video WHERE category_id = c.id), " +
                    "       total_size_thumb_sq = (SELECT SUM(thumb_sq_size) FROM video.video WHERE category_id = c.id), " +
                    "       total_size_scaled = (SELECT SUM(scaled_size) FROM video.video WHERE category_id = c.id), " +
                    "       total_size_full = (SELECT SUM(full_size) FROM video.video WHERE category_id = c.id), " +
                    "       total_size_raw = (SELECT SUM(raw_size) FROM video.video WHERE category_id = c.id), " +
                    "       teaser_image_size = (SELECT thumb_size FROM video.video WHERE category_id = c.id AND thumb_path = c.teaser_image_path), " +
                    "       teaser_image_sq_height = (SELECT thumb_sq_height FROM video.video WHERE category_id = c.id AND thumb_path = c.teaser_image_path), " +
                    "       teaser_image_sq_width = (SELECT thumb_sq_width FROM video.video WHERE category_id = c.id AND thumb_path = c.teaser_image_path), " +
                    "       teaser_image_sq_path = (SELECT thumb_sq_path FROM video.video WHERE category_id = c.id AND thumb_path = c.teaser_image_path), " +
                    "       teaser_image_sq_size = (SELECT thumb_sq_size FROM video.video WHERE category_id = c.id AND thumb_path = c.teaser_image_path);"
                );

                sw.WriteLine(sep);
                sw.WriteLine("-- drop the indexes that were used for these updates");
                sw.WriteLine(sep);

                sw.WriteLine("DROP INDEX photo.ix_photo_photo_xs_path;");
                sw.WriteLine("DROP INDEX video.ix_video_video_thumb_path;");

                sw.Flush();
            }
        }


        string GetPhotoWebPath(string path)
        {
            return path.Substring(path.IndexOf("/images/"));
        }


        string GetVideoWebPath(string path)
        {
            return path.Substring(path.IndexOf("/movies/"));
        }


        public static string SqlNumber(object num)
        {
            if(num == null)
            {
                return "NULL";
            }

            return num.ToString();
        }


        string SqlTimestamp(DateTime? dt)
        {
            if(dt == null)
            {
                return "NULL";
            }

            return SqlString(((DateTime)dt).ToString("yyyy-MM-dd HH:mm:sszzz"));
        }


        string SqlString(string val)
        {
            if(string.IsNullOrWhiteSpace(val))
            {
                return "NULL";
            }
            else
            {
                val = val.Replace("'", "''");

                return $"'{val}'";
            }
        }
    }
}
