using System;


namespace AssetUpdate2019.Data
{
    public class Photo
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public DateTime? CreateDate { get; set; }
        public double? Latitude { get; set; }
        public string LatitudeRef { get; set; }
        public double? Longitude { get; set; }
        public string LongitudeRef { get; set; }
        public Media MediaXs { get; set; }
        public Media MediaXsSq { get; set; }
        public Media MediaSm { get; set; }
        public Media MediaMd { get; set; }
        public Media MediaLg { get; set; }
        public Media MediaPrt { get; set; }
        public Media MediaSrc { get; set; }
    }
}