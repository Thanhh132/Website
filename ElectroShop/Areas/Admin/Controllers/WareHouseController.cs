using ElectroShop.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ElectroShop.Areas.Admin.Controllers
{
    public class WareHouseController : BaseController
    {
        private ElectroShopDbContext db = new ElectroShopDbContext();

        // GET: Admin/WareHouse
        public ActionResult ImportList()
        {
            return View(db.ProductData.ToList());
        }

        private void ReadWriteExcelFiles(string filePath, string[] newContent)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    ViewBag.Message = "Lỗi: File Excel không tồn tại.";
                    return;
                }
                using (var excelPackage = new ExcelPackage(new FileInfo(filePath)))
                {
                    ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[0];

                    int startRow = 5;
                    int currentRow = startRow;
                    while (!string.IsNullOrEmpty(worksheet.Cells[currentRow, 1].Value?.ToString()))
                    {
                        currentRow++;
                        if (currentRow >= startRow + 10)
                        {
                            ViewBag.Message = "Dữ liệu đã đầy, không thể thêm nữa.";
                            return;
                        }
                    }

                    for (int i = 0; i < newContent.Length; i++)
                    {
                        worksheet.Cells[currentRow, i + 1].Value = newContent[i];
                    }

                    excelPackage.Save();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Đã xảy ra lỗi khi đọc và ghi file Excel: " + ex.Message;
            }
        }

        private ProductData LoadProductDataFromDatabase(int id)
        {
            ElectroShopDbContext db = new ElectroShopDbContext();

            ProductData product = db.ProductData.FirstOrDefault(p => p.ID == id);

            return product;
        }


        public ActionResult ImportBill(string[] newContent, int id)
        {
            List<ProductData> products = db.ProductData.ToList();
            ProductData productss = LoadProductDataFromDatabase(id);

            ViewBag.Product = productss;

            ViewBag.Products = products;

            if (newContent != null && newContent.Length == 6)
            {
                try
                {
                    string filePath = Server.MapPath("~/Areas/File/Products1.xlsx"); // Đường dẫn đến tập tin Excel

                    string productName = newContent[2];
                    int quantityExported = int.Parse(newContent[3]);

                    ProductData product = db.ProductData.FirstOrDefault(p => p.Name == productName);

                    if (product != null)
                    {

                        ReadWriteExcelFiles(filePath, newContent);                       
                        ViewBag.Message = "Đã in thành công!";                       
                    }
                    else
                    {
                        ViewBag.Message = "Không tìm thấy sản phẩm trong cơ sở dữ liệu.";
                        return RedirectToAction("ImportList");
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "Đã xảy ra lỗi: " + ex.Message;
                }
            }
            else
            {
                ViewBag.Messages = "Vui lòng nhập đúng nội dung cho tất cả các ô.";
            }

            return View();
        }
    }
}
