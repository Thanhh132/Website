using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ElectroShop.Models
{
    public class ProductKeys
    {
        public int KeyID { get; set; }
        public int ProductID { get; set; }
        public string LicenseKey { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? UsedAt { get; set; }
    }
}