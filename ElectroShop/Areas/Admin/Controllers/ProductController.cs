using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ElectroShop.Models;

namespace ElectroShop.Areas.Admin.Controllers
{
    public class ProductController : BaseController
    {
        private ElectroShopDbContext db = new ElectroShopDbContext();

        // GET: Admin/Product
        public ActionResult Index()
        {
            ViewBag.countTrash = db.Products.Where(m => m.Status == 0).Count();
            var list = from p in db.Products
                       join c in db.Categorys
                       on p.CateID equals c.Id
                       where p.Status != 0
                       where p.CateID == c.Id
                       orderby p.Created_at descending
                       select new ProductCategory()
                       {
                           ProductId = p.ID,
                           ProductImg = p.Image,
                           ProductName = p.Name,
                           ProductStatus = p.Status,
                           ProductDiscount = p.Discount,
                           ProductPrice = p.Price,
                           ProductPriceSale = p.ProPrice ?? 0,
                           ProductCreated_At = p.Created_at,
                           CategoryName = c.Name
                       };
            return View(list.ToList());
        }
        public ActionResult Trash()
        {
            var list = from p in db.Products
                       join c in db.Categorys
                       on p.CateID equals c.Id
                       where p.Status == 0
                       where p.CateID == c.Id
                       orderby p.Created_at descending
                       select new ProductCategory()
                       {
                           ProductId = p.ID,
                           ProductImg = p.Image,
                           ProductName = p.Name,
                           ProductStatus = p.Status,
                           ProductDiscount = p.Discount,
                           ProductPrice = p.Price,
                           ProductPriceSale = p.ProPrice ?? 0,
                           ProductCreated_At = p.Created_at,
                           CategoryName = c.Name
                       };

            ViewBag.TrashCount = list.Count(); // Đếm số lượng sản phẩm trong thùng rác
            return View(list.ToList());
        }
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                Notification.set_flash("Không tồn tại!", "warning");
                return RedirectToAction("Index");
            }
            MProduct mProduct = db.Products.Find(id);
            if (mProduct == null)
            {
                Notification.set_flash("Không tồn tại!", "warning");
                return RedirectToAction("Index");
            }
            return View(mProduct);
        }

        public ActionResult Create()
        {
            ViewBag.countTrash = db.Products.Where(m => m.Status == 0).Count();
            MCategory mCategory = new MCategory();
            ViewBag.ListCat = new SelectList(db.Categorys.Where(m => m.Status != 0), "ID", "Name", 0);
            //ViewBag.ListCat = new SelectList(db.Category.ToList(), "ID", "Name", 0);
            return View();
        }

        [HttpPost, ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult Create(MProduct mProduct)
        {
            ViewBag.ListCat = new SelectList(db.Categorys.Where(m => m.Status != 0), "ID", "Name", 0);
            if (ModelState.IsValid)
            {
                if (mProduct.Discount == 1) // Nếu chọn "Có"
                {
                    if (mProduct.ProPrice <= 0) // Kiểm tra giá giảm có được nhập hay không
                    {
                        ModelState.AddModelError("ProPrice", "Vui lòng nhập giá khuyến mại.");
                        return View(mProduct);
                    }
                }
                mProduct.Price = mProduct.Price + 0;
                mProduct.ProPrice = mProduct.ProPrice + 0;

                String strSlug = MyString.ToAscii(mProduct.Name);
                mProduct.Slug = strSlug;
                mProduct.Created_at = DateTime.Now;
                mProduct.Created_by = int.Parse(Session["Admin_ID"].ToString());
                mProduct.Updated_at = DateTime.Now;
                mProduct.Updated_by = int.Parse(Session["Admin_ID"].ToString());

                // Upload file
                var file = Request.Files["Image"];
                if (file != null && file.ContentLength > 0)
                {
                    String filename = strSlug + file.FileName.Substring(file.FileName.LastIndexOf("."));
                    mProduct.Image = filename;
                    String Strpath = Path.Combine(Server.MapPath("~/Public/library/product/"), filename);
                    file.SaveAs(Strpath);
                }

                db.Products.Add(mProduct);
                db.SaveChanges();
				// Tạo đối tượng ProductData từ sản phẩm mới tạo
				ProductData productData = new ProductData
				{
					STT = mProduct.ID.ToString(),
					Name = mProduct.Name,
					Price = mProduct.Price.ToString(),
					PriceSale = mProduct.ProPrice.ToString(),
					Quantity = mProduct.Quantity.ToString()
				};

				// Lưu giá trị vào đối tượng ProductData
				db.ProductData.Add(productData);
				db.SaveChanges();
				Notification.set_flash("Thêm mới sản phẩm thành công!", "success");
                return RedirectToAction("Index");
            }
            return View(mProduct);
        }

        public ActionResult Edit(int? id)
        {
            ViewBag.countTrash = db.Products.Where(m => m.Status == 0).Count();
            ViewBag.ListCat = new SelectList(db.Categorys.ToList(), "ID", "Name", 0);
            MProduct mProduct = db.Products.Find(id);
            if (mProduct == null)
            {
                Notification.set_flash("404!", "warning");
                return RedirectToAction("Index", "Product");
            }
            return View(mProduct);
        }

        [HttpPost, ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(MProduct mProduct)
        {
            ViewBag.ListCat = new SelectList(db.Categorys.ToList(), "ID", "Name", 0);
            if (ModelState.IsValid)
            {
                if (mProduct.Discount == 1) // Nếu chọn "Có"
                {
                    if (mProduct.ProPrice <= 0) // Kiểm tra giá giảm có được nhập hay không
                    {
                        ModelState.AddModelError("ProPrice", "Vui lòng nhập giá khuyến mại.");
                        return View(mProduct);
                    }
                }
                String strSlug = MyString.ToAscii(mProduct.Name);
                mProduct.Slug = strSlug;

                mProduct.Updated_at = DateTime.Now;
                mProduct.Updated_by = int.Parse(Session["Admin_ID"].ToString());

                // Upload file
                var file = Request.Files["Image"];
                if (file != null && file.ContentLength > 0)
                {
                    String filename = strSlug + file.FileName.Substring(file.FileName.LastIndexOf("."));
                    mProduct.Image = filename;
                    String Strpath = Path.Combine(Server.MapPath("~/Public/library/product/"), filename);
                    file.SaveAs(Strpath);
                }

                db.Entry(mProduct).State = EntityState.Modified;
                db.SaveChanges();
                Notification.set_flash("Đã cập nhật lại thông tin sản phẩm!", "success");
                return RedirectToAction("Index");
            }
            return View(mProduct);
        }

        public ActionResult DelTrash(int? id)
        {
            MProduct mProduct = db.Products.Find(id);
            mProduct.Status = 0;

            mProduct.Updated_at = DateTime.Now;
            mProduct.Updated_by = int.Parse(Session["Admin_ID"].ToString());
            db.Entry(mProduct).State = EntityState.Modified;
            db.SaveChanges();
            Notification.set_flash("Ném thành công vào thùng rác!" + " ID = " + id, "success");
            return RedirectToAction("Index");
        }
        public ActionResult Undo(int? id)
        {
            MProduct mProduct = db.Products.Find(id);
            mProduct.Status = 2;

            mProduct.Updated_at = DateTime.Now;
            mProduct.Updated_by = int.Parse(Session["Admin_ID"].ToString()); ;
            db.Entry(mProduct).State = EntityState.Modified;
            db.SaveChanges();
            Notification.set_flash("Khôi phục thành công!" + " ID = " + id, "success");
            return RedirectToAction("Trash");
        }
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                Notification.set_flash("Không tồn tại !", "warning");
                return RedirectToAction("Trash");
            }
            MProduct mProduct = db.Products.Find(id);
            if (mProduct == null)
            {
                Notification.set_flash("Không tồn tại !", "warning");
                return RedirectToAction("Trash");
            }
            return View(mProduct);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAllTrash()
        {
            var trashedProducts = db.Products.Where(p => p.Status == 0).ToList();
            foreach (var product in trashedProducts)
            {
                db.Products.Remove(product);
            }
            db.SaveChanges();
            Notification.set_flash("Đã xóa tất cả sản phẩm trong thùng rác!", "success");
            return RedirectToAction("Trash");
        }

        // POST: Admin/Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            MProduct mProduct = db.Products.Find(id);
            db.Products.Remove(mProduct);
            db.SaveChanges();
            Notification.set_flash("Đã xóa vĩnh viễn sản phẩm!", "danger");
            return RedirectToAction("Trash");
        }

        [HttpPost]
        public JsonResult changeStatus(int id)
        {
            MProduct mProduct = db.Products.Find(id);
            mProduct.Status = (mProduct.Status == 1) ? 2 : 1;

            mProduct.Updated_at = DateTime.Now;
            mProduct.Updated_by = int.Parse(Session["Admin_ID"].ToString());
            db.Entry(mProduct).State = EntityState.Modified;
            db.SaveChanges();
            return Json(new { Status = mProduct.Status });
        }
        [HttpPost]
        public JsonResult changeDiscount(int id)
        {
            MProduct mProduct = db.Products.Find(id);
            mProduct.Discount = (mProduct.Discount == 1) ? 2 : 1;

            mProduct.Updated_at = DateTime.Now;
            mProduct.Updated_by = int.Parse(Session["Admin_ID"].ToString());
            db.Entry(mProduct).State = EntityState.Modified;
            db.SaveChanges();

            return Json(new { Discount = mProduct.Discount });
        }
    }
}
