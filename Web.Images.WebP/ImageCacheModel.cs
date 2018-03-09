using System;

namespace Web.Images.WebP
{
    internal class ImageCacheModel
    {
        public byte[] WebpContent { get; set; }
        public String FallbackImage { get; set; }
    }
}
