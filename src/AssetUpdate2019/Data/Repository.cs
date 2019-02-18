using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Npgsql;


namespace AssetUpdate2019.Data
{
    class Repository
    {
        const string PHOTO_SQL = @"
            SELECT
            id,
            category_id,
            create_date,
            gps_latitude AS latitude,
            gps_latitude_ref_id AS latitude_ref,
            gps_longitude AS longitude,
            gps_longitude_ref_id AS longitude_ref,
            xs_path AS path,
            xs_width AS width,
            xs_height AS height,
            xs_size AS size,
            xs_sq_path AS path,
            xs_sq_width AS width,
            xs_sq_height AS height,
            xs_sq_size AS size,
            sm_path AS path,
            sm_width AS width,
            sm_height AS height,
            sm_size AS size,
            md_path AS path,
            md_width AS width,
            md_height AS height,
            md_size AS size,
            lg_path AS path,
            lg_width AS width,
            lg_height AS height,
            lg_size AS size,
            prt_path AS path,
            prt_width AS width,
            prt_height AS height,
            prt_size AS size,
            src_path AS path,
            src_width AS width,
            src_height AS height,
            src_size AS size
            FROM photo.photo";

        static readonly Type[] PHOTO_PROJECTION_TYPES = new Type[] {
            typeof(Photo),
            typeof(Media),
            typeof(Media),
            typeof(Media),
            typeof(Media),
            typeof(Media),
            typeof(Media),
            typeof(Media)
        };

        const string VIDEO_SQL = @"
            SELECT
            id,
            category_id,
            duration,
            create_date,
            gps_latitude AS latitude,
            gps_latitude_ref_id AS latitude_ref,
            gps_longitude AS longitude,
            gps_longitude_ref_id AS longitude_ref,
            thumb_path AS path,
            thumb_width AS width,
            thumb_height AS height,
            thumb_size AS size,
            thumb_sq_path AS path,
            thumb_sq_width AS width,
            thumb_sq_height AS height,
            thumb_sq_size AS size,
            scaled_path AS path,
            scaled_width AS width,
            scaled_height AS height,
            scaled_size AS size,
            full_path AS path,
            full_width AS width,
            full_height AS height,
            full_size AS size,
            raw_path AS path,
            raw_width AS width,
            raw_height AS height,
            raw_size AS size
            FROM video.video";

        static readonly Type[] VIDEO_PROJECTION_TYPES = new Type[] {
            typeof(Video),
            typeof(Media),
            typeof(Media),
            typeof(Media),
            typeof(Media),
            typeof(Media)
        };

        string _connString;


        static Repository()
        {
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
            Dapper.SqlMapper.AddTypeMap(typeof(string), System.Data.DbType.AnsiString);
        }


        public Repository(string connString)
        {
            if(string.IsNullOrWhiteSpace(connString))
            {
                throw new ArgumentNullException(nameof(connString));
            }

            _connString = connString;
        }


        public Task<IEnumerable<Photo>> GetPhotosAsync()
        {
            return RunAsync(conn => {
                return conn.QueryAsync<Photo>(
                    PHOTO_SQL,
                    PHOTO_PROJECTION_TYPES,
                    (objects) => AssemblePhoto(objects),
                    splitOn: "path"
                );
            });
        }


        public Task<IEnumerable<Video>> GetVideosAsync()
        {
            return RunAsync(conn => {
                return conn.QueryAsync<Video>(
                    VIDEO_SQL,
                    VIDEO_PROJECTION_TYPES,
                    (objects) => AssembleVideo(objects),
                    splitOn: "path"
                );
            });
        }


        async Task<T> RunAsync<T>(Func<IDbConnection, Task<T>> queryData)
        {
            using(var conn = GetConnection())
            {
                await conn.OpenAsync().ConfigureAwait(false);

                return await queryData(conn).ConfigureAwait(false);
            }
        }


        DbConnection GetConnection()
        {
            return new NpgsqlConnection(_connString);
        }


        Photo AssemblePhoto(object[] objects)
        {
            var photo = (Photo) objects[0];

            photo.MediaXs = (Media) objects[1];
            photo.MediaXsSq = (Media) objects[2];
            photo.MediaSm = (Media) objects[3];
            photo.MediaMd = (Media) objects[4];
            photo.MediaLg = (Media) objects[5];
            photo.MediaPrt = (Media) objects[6];
            photo.MediaSrc = (Media) objects[7];

            return photo;
        }


        Video AssembleVideo(object[] objects)
        {
            var video = (Video) objects[0];

            video.MediaThumbnail = (Media) objects[1];
            video.MediaThumbnailSq = (Media) objects[2];
            video.MediaScaled = (Media) objects[3];
            video.MediaFullsize = (Media) objects[4];
            video.MediaRaw = (Media) objects[5];

            return video;
        }
    }
}