using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFProxy.Models
{
    public class TokenModel
    {
        public string instance_url { get; set; }
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string signature { get; set; }
    }
}
