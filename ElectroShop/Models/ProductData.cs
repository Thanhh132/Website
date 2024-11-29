using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ElectroShop.Models
{
	public class ProductData
	{
		public int ID { get; set; }
		public string STT { get; set; }
		public string Name { get; set; }
		public string Price { get; set; }
		public string PriceSale { get; set; }
		public string Quantity { get; set; }
	}
}