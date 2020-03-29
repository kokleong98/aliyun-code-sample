using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using oss_bucket.Models;
using Aliyun.OSS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace oss_bucket.Controllers
{
    public class HomeController : Controller
    {
        private IConfiguration config;
        private string endpoint;
        private string accessKeyId;
        private string accessKeySecret;

        public HomeController(IConfiguration configuration)
        {
            config = configuration;
            endpoint = config["MySettings:endpoint"];
            accessKeyId = config["MySettings:accessKeyId"];
            accessKeySecret = config["MySettings:accessKeySecret"];
        }

        public IActionResult Index()
        {
            // Initialize OSS client
            OssClient client = new OssClient(endpoint, accessKeyId, accessKeySecret);
            var buckets = client.ListBuckets();

            #region Map to our page data model
            List<BucketModel> items = new List<BucketModel>();
            foreach (var item in buckets)
            {
                BucketModel newItem = new BucketModel();
                newItem.name = item.Name;
                items.Add(newItem);
            }
            #endregion

            #region Clean-up
            client = null;
            buckets = null;
            #endregion

            return View(items);
        }

        public IActionResult ViewOSS(string? id, string ? path)
        {
            // Initialize OSS client
            OssClient client = new OssClient(endpoint, accessKeyId, accessKeySecret);
            ListObjectsRequest request = new ListObjectsRequest(id);
            request.Delimiter = "/";
            request.Prefix = string.IsNullOrEmpty(path) ? "" : path;
            var listResult = client.ListObjects(request);

            #region Map to our page data model
            List<BucketObjectModel> items = new List<BucketObjectModel>();

            foreach (var item in listResult.CommonPrefixes)
            {
                BucketObjectModel newItem = new BucketObjectModel();
                newItem.isFolder = true;
                newItem.key = item;
                newItem.path = item;
                newItem.name = item.Split("/", StringSplitOptions.RemoveEmptyEntries).Last();
                newItem.modifiedTime = DateTime.MinValue;
                newItem.size = 0;
                items.Add(newItem);
            }

            foreach (var item in listResult.ObjectSummaries)
            {
                BucketObjectModel newItem = new BucketObjectModel();
                newItem.isFolder = item.Key.EndsWith("/");
                newItem.key = item.Key;
                newItem.path = Path.GetPathRoot(item.Key);
                newItem.name = Path.GetFileName(item.Key);
                
                newItem.modifiedTime = item.LastModified;
                newItem.size = item.Size;

                if (string.IsNullOrEmpty(newItem.name)) continue;
                items.Add(newItem);
            }
            #endregion

            #region Clean-up
            client = null;
            listResult = null;
            #endregion

            return View(items);
        }

        public IActionResult Upload(string id, string ? path)
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(string id, string? path, IFormFile file)
        {
            // Initialize OSS client
            OssClient client = new OssClient(endpoint, accessKeyId, accessKeySecret);

            PutObjectResult result = client.PutObject(id, Path.Combine(path, file.FileName), file.OpenReadStream());

            #region Clean-up
            client = null;
            result = null;
            #endregion

            return Redirect(string.Format("/{0}/{1}/{2}?path={3}", "Home", "ViewOSS", id, path));
        }

        public IActionResult Delete(string id, string bucket, string? path)
        {
            // Initialize OSS client
            OssClient client = new OssClient(endpoint, accessKeyId, accessKeySecret);

            client.DeleteObject(bucket, WebUtility.UrlDecode(id));

            #region Clean-up
            client = null;
            #endregion

            if(WebUtility.UrlDecode(id).EndsWith("/"))
            {
                return Redirect(string.Format("/{0}/{1}/{2}?path={3}", "Home", "ViewOSS", bucket, ParentPath(WebUtility.UrlDecode(id))));
            }
            return Redirect(string.Format("/{0}/{1}/{2}?path={3}", "Home", "ViewOSS", bucket, path));
        }

        public IActionResult Download(string id, string bucket)
        {
            // Initialize OSS client
            OssClient client = new OssClient(endpoint, accessKeyId, accessKeySecret);

            OssObject oss = client.GetObject(bucket, System.Net.WebUtility.UrlDecode(id));

            #region Clean-up
            client = null;
            #endregion

            return File(oss.Content, "application/ocet-stream", id);
        }

        public IActionResult NewFolder(string id, string? path)
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> NewFolder(string id, string? path, string? foldername)
        {
            // Initialize OSS client
            OssClient client = new OssClient(endpoint, accessKeyId, accessKeySecret);
            PutObjectResult result = client.PutObject(id, string.Format("{0}{1}/", path, foldername), Stream.Null);

            #region Clean-up
            client = null;
            result = null;
            #endregion

            return Redirect(string.Format("/{0}/{1}/{2}?path={3}", "Home", "ViewOSS", id, path));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public static string ParentPath(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            int lastIndex = value.LastIndexOf("/", value.Length - 2);
            if (lastIndex >= 0)
            {
                return value.Substring(0, lastIndex+1);
            }
            return string.Empty;
        }
    }
}
