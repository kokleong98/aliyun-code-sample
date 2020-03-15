using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        public IActionResult ViewOSS(string id)
        {
            // Initialize OSS client
            OssClient client = new OssClient(endpoint, accessKeyId, accessKeySecret);
            var listResult = client.ListObjects(id);

            #region Map to our page data model
            List<BucketObjectModel> items = new List<BucketObjectModel>();
            foreach(var item in listResult.ObjectSummaries)
            {
                BucketObjectModel newItem = new BucketObjectModel();
                newItem.name = item.Key;
                newItem.modifiedTime = item.LastModified;
                newItem.size = item.Size;
                items.Add(newItem);
            }
            #endregion

            #region Clean-up
            client = null;
            listResult = null;
            #endregion

            return View(items);
        }

        public IActionResult Upload(string id)
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(string id, IFormFile file)
        {
            // Initialize OSS client
            OssClient client = new OssClient(endpoint, accessKeyId, accessKeySecret);

            PutObjectResult result = client.PutObject(id, file.FileName, file.OpenReadStream());

            #region Clean-up
            client = null;
            result = null;
            #endregion

            return Redirect(string.Format("/{0}/{1}/{2}", "Home", "ViewOSS", id));
        }

        public IActionResult Delete(string id, string bucket)
        {
            // Initialize OSS client
            OssClient client = new OssClient(endpoint, accessKeyId, accessKeySecret);

            client.DeleteObject(bucket, id);

            #region Clean-up
            client = null;
            #endregion

            return Redirect(string.Format("/{0}/{1}/{2}", "Home", "ViewOSS", bucket));
        }

        public IActionResult Download(string id, string bucket)
        {
            // Initialize OSS client
            OssClient client = new OssClient(endpoint, accessKeyId, accessKeySecret);

            OssObject oss = client.GetObject(bucket, id);

            #region Clean-up
            client = null;
            #endregion

            return File(oss.Content, "application/ocet-stream", id);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
