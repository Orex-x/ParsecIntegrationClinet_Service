using System;

namespace ParsecIntegrationClient.Models
{
    public class Card
    {
        public string CODE { get; set; }
        public string GUID_PEP { get; set; }
        public string GUID_ACCESS_GROUP{ get; set; }
        public bool IS_PRIMARY { get; set; }
        public DateTime VALID_FROM { get; set; }
        public DateTime VALID_TO { get; set; }
    }
}
