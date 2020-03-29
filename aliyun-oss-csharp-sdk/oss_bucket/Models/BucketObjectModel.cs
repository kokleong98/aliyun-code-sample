using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace oss_bucket.Models
{
    public class BucketObjectModel
    {
        public string name;
        public string key;
        public string path;
        public bool isFolder;
        public long size;
        public DateTime modifiedTime;
    }
}
