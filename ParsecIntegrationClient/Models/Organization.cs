using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsecIntegrationClient.Models
{
    public class Organization
    {
        public string ID { get; set; }
        public string NAME { get; set; }
        public string DESCRIPTION { get; set; }
        public string GUID { get; set; }
        public string PARENT_GUID { get; set; }
    }
}
