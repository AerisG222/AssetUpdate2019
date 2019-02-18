using System;


namespace AssetUpdate2019.Data
{
    public class Video
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public DateTime? CreateDate { get; set; }
        public double? Latitude { get; set; }
        public string LatitudeRef { get; set; }
        public double? Longitude { get; set; }
        public string LongitudeRef { get; set; }
        public Media MediaThumbnail { get; set; }
        public Media MediaThumbnailSq { get; set; }
        public Media MediaScaled { get; set; }
        public Media MediaFullsize { get; set; }
        public Media MediaRaw { get; set; }
    }
}