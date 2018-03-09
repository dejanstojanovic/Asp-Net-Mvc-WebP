using System;
using System.Web;
using System.IO;
using System.Web.Caching;

namespace Web.Images.WebP
{
    public class RequestHandler : IHttpHandler
    {
        public bool IsReusable => true;

        public void ProcessRequest(HttpContext context)
        {
            var imageCacheItem = GetFromCache(context);

            if (context.Request.Url.AbsoluteUri.EndsWith(".webp", StringComparison.InvariantCultureIgnoreCase))
            {
                var path = context.Server.MapPath(context.Request.Url.AbsolutePath);

                if (context.Request.UserAgent.IndexOf("Chrome/", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    context.Response.ClearHeaders();
                    context.Response.ClearContent();

                    if (imageCacheItem != null && imageCacheItem.WebpContent != null)
                    {
                        ReturnWebpContent(context, imageCacheItem.WebpContent);
                    }
                    else
                    {
                        if (File.Exists(path))
                        {
                            var content = File.ReadAllBytes(path);
                            UpdateCacheContent(context, content, path);
                            ReturnWebpContent(context, content);
                        }
                        else
                        {
                            ImageFallback(context, path);
                        }
                    }
                }
                else
                {
                    ImageFallback(context,path);              
                }
            }
        }

        private void ReturnWebpContent(HttpContext context, byte[] content)
        {
            context.Response.OutputStream.Write(content, 0, content.Length);
            context.Response.OutputStream.Flush();

            context.Response.AppendHeader("Content-type", "image/webp");
        }


        private String GetImagePath(String path, String extension)
        {
            return Path.Combine(Path.GetDirectoryName(path), String.Concat(Path.GetFileNameWithoutExtension(path),".", extension));
        }

        private void ImageFallback(HttpContext context,String path)
        {
            var imageCacheItem = GetFromCache(context);

            if (imageCacheItem != null && !String.IsNullOrWhiteSpace(imageCacheItem.FallbackImage))
            {
                context.Response.Redirect(imageCacheItem.FallbackImage);
            }
            else
            {
                var extensions = new String[] { "png", "jpg", "jpeg" };
                bool found = false;
                foreach (var extension in extensions)
                {
                    var imagePath = GetImagePath(path, extension);

                    if (File.Exists(imagePath))
                    {
                        var staticUrl = context.Request.Url.AbsoluteUri.Substring(0, context.Request.Url.AbsoluteUri.LastIndexOf("/"));
                        staticUrl = String.Concat(staticUrl, "/", Path.GetFileName(imagePath));
                        found = true;

                        UpdateCacheFallback(context, staticUrl, path);

                        context.Response.Redirect(staticUrl);
                    }
                }

                if (!found)
                {
                    context.Response.ClearContent();
                    context.Response.ClearHeaders();
                    context.Response.StatusCode = 404;
                }
            }
        }

        #region Cache handling

        private ImageCacheModel GetFromCache(HttpContext context)
        {
            return context.Cache.Get(context.Request.Url.AbsoluteUri.ToLower()) as ImageCacheModel;
        }

        private void UpdateCacheContent(HttpContext context, byte[] content, String filePath)
        {
            var imageCacheItem = GetFromCache(context);
            if (imageCacheItem == null)
            {
                imageCacheItem = new ImageCacheModel();
            }
            imageCacheItem.WebpContent = content;

            context.Cache.Insert(context.Request.Url.AbsoluteUri.ToLower(), imageCacheItem, new CacheDependency(filePath));
        }

        private void UpdateCacheFallback(HttpContext context, String fallbackUrl, String filePath)
        {
            var imageCacheItem = GetFromCache(context);
            if (imageCacheItem == null)
            {
                imageCacheItem = new ImageCacheModel();
            }
            imageCacheItem.FallbackImage = fallbackUrl;

            context.Cache.Insert(context.Request.Url.AbsoluteUri.ToLower(), imageCacheItem, new CacheDependency(filePath));

        }

        #endregion

    }
}
