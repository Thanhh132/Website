    namespace ElectroShop.Models
{
    using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;


    [Table("Products")]
    public class MProduct
    {
        [Key]
        [Required]
        public int ID { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhập tên sản phẩm")]
        public string Name { get; set; }

        public string Slug { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại sản phẩm")]
        public int CateID { get; set; }

        public string Image { get; set; }

        public string NewPromotion { get; set; } // Có thể null

        public decimal? Installment { get; set; } // Nullable

        public int? Discount { get; set; } // Nullable

        [Required(ErrorMessage = "Vui lòng nhập thông tin này.")]
        public string Detail { get; set; }


        [Required(ErrorMessage = "Vui lòng nhập thông tin này.")]
        public string Description { get; set; }

        public string Specification { get; set; } // Có thể null

        [Required(ErrorMessage = "Vui lòng nhập giá.")]
        public double Price { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng.")]
        public int Quantity { get; set; }

        
        public double? ProPrice { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập từ khóa.")]
        public string MetaKey { get; set; }

        public string MetaDesc { get; set; } // Có thể null

        public int Status { get; set; }

        public DateTime Created_at { get; set; }

        public int Created_by { get; set; }

        public DateTime Updated_at { get; set; }

        public int Updated_by { get; set; }
    }
}