using ElectroShop.Library;
using ElectroShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web.Mvc;
using System.Text;
using System.Data.Entity;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using QRCoder;
using System.Configuration;
using System.Net;
namespace ElectroShop.Controllers
{
    public class CartController : Controller
    {
        private ElectroShopDbContext db = new ElectroShopDbContext();



        // GET: Cart
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Add(int pid, int qty)
        {
            var p = db.Products.FirstOrDefault(m => m.Status == 1 && m.ID == pid);
            if (p == null)
            {
                return Json(new { result = 0, message = "Sản phẩm không tồn tại hoặc không có sẵn." });
            }

            double productPrice = (p.ProPrice.HasValue && p.ProPrice.Value > 0) ? p.ProPrice.Value : p.Price;

            var cart = Session["Cart"] as List<ModelCart>;

            if (cart == null)
            {
                cart = new List<ModelCart>();
                Session["Cart"] = cart;
            }

            var existingItem = cart.FirstOrDefault(m => m.ProductID == pid);

            if (existingItem != null)
            {
                existingItem.Quantity += qty;
                return Json(new { result = 2, message = "Đã cập nhật số lượng sản phẩm trong giỏ hàng." });
            }
            else
            {
                cart.Add(new ModelCart
                {
                    ProductID = p.ID,
                    Name = p.Name,
                    Slug = p.Slug,
                    Image = p.Image,
                    Quantity = qty,
                    Price = productPrice,
                    IsSelected = true // Set to true by default
                });
                return Json(new { result = 1, message = "Đã thêm sản phẩm vào giỏ hàng." });
            }
        }
        [HttpPost]
        public JsonResult UpdateSelection(int productId, bool isSelected)
        {
            var cart = Session["Cart"] as List<ModelCart>;
            if (cart != null)
            {
                var item = cart.FirstOrDefault(c => c.ProductID == productId);
                if (item != null)
                {
                    item.IsSelected = isSelected;
                    Session["Cart"] = cart;

                    double totalPrice = cart.Where(c => c.IsSelected)
                                            .Sum(c => c.Price * c.Quantity);
                    return Json(new { success = true, totalPrice = totalPrice.ToString("N0") });
                }
            }
            return Json(new { success = false, message = "Không thể cập nhật trạng thái chọn." });
        }
        [HttpPost]
        public JsonResult UpdateQuantity(int productId, int quantity)
        {
            using (var db = new ElectroShopDbContext())
            {
                var product = db.Products.FirstOrDefault(p => p.ID == productId);

                if (product != null)
                {
                    if (quantity > product.Quantity)
                    {
                        return Json(new { success = false, message = "Số lượng vượt quá số lượng hiện có trong kho!", currentQuantity = product.Quantity });
                    }

                    var cart = (List<ModelCart>)Session["Cart"];
                    var cartItem = cart.FirstOrDefault(c => c.ProductID == productId);

                    if (cartItem != null)
                    {
                        cartItem.Quantity = quantity;
                        Session["Cart"] = cart;

                        double totalPrice = cart.Where(c => c.IsSelected).Sum(c => c.Price * c.Quantity);
                        return Json(new { success = true, newTotalPrice = totalPrice.ToString("N0"), itemTotal = (cartItem.Price * cartItem.Quantity).ToString("N0") });
                    }
                }

                return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng!" });
            }
        }

        [HttpPost]
        public JsonResult GetTotalPrice()
        {
            var cart = Session["Cart"] as List<ModelCart>;
            if (cart != null)
            {
                double totalPrice = cart.Where(c => c.IsSelected)
                                        .Sum(c => c.Price * c.Quantity);
                return Json(new { success = true, totalPrice = totalPrice.ToString("N0") });
            }
            return Json(new { success = false, message = "Không thể tính tổng giá." });
        }

        public ActionResult RemoveAll()
        {
            var cart = Session["Cart"] as List<ModelCart>;
            if (cart != null)
            {
                cart.RemoveAll(item => item.IsSelected); // Chỉ xóa những sản phẩm đã được chọn
                if (cart.Count == 0)
                {
                    Session.Remove("Cart");
                }
                else
                {
                    Session["Cart"] = cart;
                }
            }
            Notification.set_flash("Đã xóa toàn bộ sản phẩm đã chọn trong giỏ hàng!", "success");
            return Redirect("~/gio-hang");
        }


        [HttpPost]
        public JsonResult RemoveItem(int productId)
        {
            var cart = Session["Cart"] as List<ModelCart>;
            if (cart != null)
            {
                var itemToRemove = cart.FirstOrDefault(c => c.ProductID == productId);
                if (itemToRemove != null)
                {
                    cart.Remove(itemToRemove);
                    Session["Cart"] = cart;

                    if (cart.Count == 0)
                    {
                        Session.Remove("Cart");
                        return Json(new { success = true, isEmpty = true });
                    }

                    double totalPrice = cart.Where(c => c.IsSelected).Sum(c => c.Price * c.Quantity);
                    return Json(new { success = true, isEmpty = false, newTotalPrice = totalPrice.ToString("N0") });
                }
            }
            return Json(new { success = false, message = "Không thể xóa sản phẩm." });
        }
        public ActionResult Checkout()
        {
            if (Session["User_Name"] == null)
            {
                Notification.set_flash("Bạn cần đăng nhập để tiếp tục!", "warning"); // Thông báo yêu cầu đăng nhập
                return RedirectToAction("Index", "Cart");
            }

            if (Session["Cart"] != null)
            {
                int user_id = Convert.ToInt32(Session["User_ID"]);
                ViewBag.Info = db.Users.Where(m => m.ID == user_id).First();
            }
            else
            {
                return RedirectToAction("Index", "Cart");
            }

            return View();
        }

        [HttpPost]
        public JsonResult Payment(String Email, String Address, String FullName, String Phone)
        {
            var order = new MOrder();
            int user_id = Convert.ToInt32(Session["User_ID"]);
            order.Code = DateTime.Now.ToString("yyyyMMddhhMMss");
            order.CustemerId = user_id;
            order.CreateDate = DateTime.Now;
            order.DeliveryAddress = Address;
            order.DeliveryEmail = Email;
            order.DeliveryPhone = Phone;
            order.DeliveryName = FullName;
            order.Status = 1;
            db.Orders.Add(order);
            db.SaveChanges();

            var OrderID = order.Id;
            var cart = Session["Cart"] as List<ModelCart>;

            // Lọc những sản phẩm đã chọn (IsSelected = true)
            var selectedItems = cart.Where(c => c.IsSelected).ToList();

            foreach (var c in selectedItems)
            {
                var orderdetails = new MOrderdetail();
                orderdetails.OrderId = OrderID;
                orderdetails.ProductId = c.ProductID;
                orderdetails.Price = c.Price;
                orderdetails.Quantity = c.Quantity;
                orderdetails.Amount = c.Price * c.Quantity;
                db.Orderdetails.Add(orderdetails);
                db.SaveChanges();
            }

            if (Session["User_Name"] != null)
            {
                int userid = Convert.ToInt32(Session["User_ID"]);
                var user = db.Users.FirstOrDefault(m => m.ID == userid);
                if (user != null)
                {
                    ViewBag.Info = user;
                    string userEmail = user.Email;
                    SendOrderConfirmationEmail(userEmail);
                }
            }

            // Xóa các sản phẩm đã thanh toán khỏi giỏ hàng
            foreach (var item in selectedItems)
            {
                cart.Remove(item);
            }

            Session["Cart"] = cart; // Cập nhật lại giỏ hàng trong session

            Notification.set_flash("Bạn đã đặt hàng thành công!", "success");
            return Json(true);
        }
        private string GetImageUrl(string imagePath)
        {
            // Sử dụng URL tuyệt đối cho cả môi trường development và production
            string baseUrl = "https://mangamart.somee.com"; // Thay thế bằng domain thực của bạn

            if (Request.IsLocal)
            {
                baseUrl = "https://mangamart.somee.com";
            }

            // Đảm bảo imagePath không bắt đầu bằng "/"
            if (imagePath.StartsWith("/"))
            {
                imagePath = imagePath.Substring(1);
            }

            return $"{baseUrl}/Public/library/product/{imagePath}";
        }
        public void SendOrderConfirmationEmail(String Mail)
        {
            string subject = "Xác nhận đơn hàng - MangaMart";
            string body = "<html><body style='font-family: Arial, sans-serif; background-color: #f0f8ff;'>";
            body += "<table style='margin: auto; border-collapse: collapse; width: 100%; background-color: #ffffff; border: 1px solid #ddd;'>";
            body += "<tr><td>";
            // Directly add the logo URL for the logo without using GetImageUrl
            body += "<div style='text-align: center; margin: 10px; background-color: transparent;'><img src='https://mangamart.somee.com/Public/user/img/brand/WebLogo.png' alt='MangaMart' style='width: 150px;'></div>";
            body += "<h2 style='text-align: center; color: #0066cc;'>Xác nhận đơn hàng</h2>";
            int userid = Convert.ToInt32(Session["User_ID"]);
            var user = db.Users.FirstOrDefault(m => m.ID == userid);
            body += "<p style='text-align: center'>Xin chào " + user.FullName + ", cảm ơn bạn đã đặt hàng tại MangaMart. Đơn hàng của bạn đã được xác nhận.</p>";
            body += "<h3 style='color: #0066cc;text-align: center'>THÔNG TIN ĐƠN HÀNG</h3>";
            body += "<table style='border-collapse: collapse; width: 100%;'>";
            body += "<tr style='background-color: #e6f2ff; border: 1px solid #ddd;'><th style='padding: 8px; text-align: left;'>Tên sản phẩm</th><th style='padding: 8px; text-align: left;'>Số lượng</th><th style='padding: 8px; text-align: left;'>Giá</th><th style='padding: 8px; text-align: left;'>Hình ảnh</th></tr>";

            var cart = (List<ModelCart>)Session["Cart"];
            var selectedItems = cart.Where(item => item.IsSelected).ToList(); // Lọc sản phẩm đã chọn

            foreach (var item in selectedItems) // Duyệt qua các sản phẩm đã chọn
            {
                body += "<tr><td style='padding: 8px; border: 1px solid #ddd;'>" + item.Name + "</td>";
                body += "<td style='padding: 8px; border: 1px solid #ddd;'>" + item.Quantity + "</td>";
                body += "<td style='padding: 8px; border: 1px solid #ddd;'>" + item.Price.ToString("N0") + " VNĐ</td>";
                body += "<td style='padding: 8px; border: 1px solid #ddd;'><img src='" + GetImageUrl(item.Image) + "' alt='" + item.Name + "' style='width: 50px; height: auto;'></td></tr>";
            }


            // Hàm hỗ trợ để lấy URL đầy đủ của ảnh

            body += "</table>";
            body += "<p style='margin: 10px; font-style: italic; color: #666;'>Đây là email xác nhận đơn hàng tự động, vui lòng không trả lời email này.</p>";
            body += "<div style='text-align: center; margin-top: 20px;'><a href='[YOUR_ORDER_CHECK_URL]' style='display: inline-block; padding: 10px 20px; background-color: #0066cc; color: #fff; text-decoration: none; border-radius: 5px;'>Kiểm tra đơn hàng</a></div>";
            body += "</td></tr></table>";
            body += "<div style='padding: 20px; text-align: center; background-color: #e6f2ff;'>";
            body += "<p>MangaMart</p>";
            body += "<a href='[YOUR_GOOGLE_PLAY_URL]'><img src='https://play.google.com/intl/en_us/badges/static/images/badges/en_badge_web_generic.png' alt='Google Play' style='height: 40px; margin: 0 5px;'></a>";
            body += "</div>";

            body += "</body></html>";

            // Tạo đối tượng MailMessage
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress("tn2525810@gmail.com", "Mangashop"); // Thay đổi ở đây
            mailMessage.To.Add(Mail);
            mailMessage.Subject = subject;
            mailMessage.Body = body;
            mailMessage.IsBodyHtml = true;

            // Gửi email sử dụng SmtpClient
            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com");
            smtpClient.Port = 587;
            smtpClient.Credentials = new System.Net.NetworkCredential("tn2525810@gmail.com", "hjin babd rhcg pymv");
            smtpClient.EnableSsl = true;

            // Gửi email
            smtpClient.Send(mailMessage);
        }

        public ActionResult PaymentVNPay()
        {
            if (Session["User_Name"] == null)
            {
                Notification.set_flash("Bạn cần đăng nhập để thanh toán!", "warning");
                return RedirectToAction("Index", "Cart");
            }

            if (Session["Cart"] == null)
            {
                Notification.set_flash("Giỏ hàng của bạn trống!", "warning");
                return RedirectToAction("Index", "Cart");
            }

            int user_id = Convert.ToInt32(Session["User_ID"]);
            var user = db.Users.FirstOrDefault(m => m.ID == user_id);
            if (user == null)
            {
                return RedirectToAction("Index", "Cart");
            }

            var order = new MOrder
            {
                Code = DateTime.Now.ToString("yyyyMMddhhMMss"),
                CustemerId = user_id,
                CreateDate = DateTime.Now,
                DeliveryAddress = user.Address,
                DeliveryEmail = user.Email,
                DeliveryPhone = user.Phone.ToString(),
                DeliveryName = user.FullName,
                Status = 1
            };
            db.Orders.Add(order);
            db.SaveChanges();

            var OrderID = order.Id;

            long totalAmount = 0;
            var cart = (List<ModelCart>)Session["Cart"];
            foreach (var item in cart.Where(i => i.IsSelected))
            {
                var orderdetails = new MOrderdetail
                {
                    OrderId = OrderID,
                    ProductId = item.ProductID,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    Amount = item.Price * item.Quantity
                };
                db.Orderdetails.Add(orderdetails);
                totalAmount += (long)(item.Price * item.Quantity);
            }
            db.SaveChanges();

            // Chuẩn bị dữ liệu cho VNPAY
            string url = ConfigurationManager.AppSettings["Url"];
            string returnUrl = ConfigurationManager.AppSettings["ReturnUrl"];
            string tmnCode = ConfigurationManager.AppSettings["TmnCode"];
            string hashSecret = ConfigurationManager.AppSettings["HashSecret"];

            PayLib pay = new PayLib();

            TimeZoneInfo vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

            pay.AddRequestData("vnp_Version", "2.1.0");
            pay.AddRequestData("vnp_Command", "pay");
            pay.AddRequestData("vnp_TmnCode", tmnCode);
            pay.AddRequestData("vnp_Amount", (totalAmount * 100).ToString());
            pay.AddRequestData("vnp_CreateDate", vietnamTime.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", "VND");
            pay.AddRequestData("vnp_IpAddr", Util.GetIpAddress());
            pay.AddRequestData("vnp_Locale", "vn");
            pay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {OrderID}");
            pay.AddRequestData("vnp_OrderType", "other");
            pay.AddRequestData("vnp_ReturnUrl", returnUrl);
            pay.AddRequestData("vnp_TxnRef", vietnamTime.Ticks.ToString()); // Sử dụng thời gian Việt Nam cho mã giao dịch

            string paymentUrl = pay.CreateRequestUrl(url, hashSecret);

            return Redirect(paymentUrl);
        }

        public ActionResult PaymentConfirm()
        {
            if (Request.QueryString.Count > 0)
            {
                string vnp_HashSecret = ConfigurationManager.AppSettings["HashSecret"]; // Chuỗi bí mật
                var vnpayData = Request.QueryString;
                PayLib pay = new PayLib();

                // Lấy toàn bộ dữ liệu được trả về
                foreach (string s in vnpayData)
                {
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        pay.AddResponseData(s, vnpayData[s]);
                    }
                }
                var cart = Session["Cart"] as List<ModelCart>;
                var selectedItems = cart.Where(c => c.IsSelected).ToList();
                long orderId = Convert.ToInt64(pay.GetResponseData("vnp_TxnRef")); // Mã hóa đơn
                long vnpayTranId = Convert.ToInt64(pay.GetResponseData("vnp_TransactionNo")); // Mã giao dịch tại VNPAY
                string vnp_ResponseCode = pay.GetResponseData("vnp_ResponseCode"); // Mã phản hồi
                string vnp_SecureHash = Request.QueryString["vnp_SecureHash"]; // Chuỗi mã hóa để kiểm tra độ toàn vẹn của dữ liệu

                bool checkSignature = pay.ValidateSignature(vnp_SecureHash, vnp_HashSecret); // Kiểm tra chữ ký đúng hay không?

                if (checkSignature)
                {
                    if (vnp_ResponseCode == "00")
                    {
                        // Thanh toán thành công
                        var order = db.Orders.Find(orderId);
                        if (order != null)
                        {
                            order.Status = 2; // Giả sử 2 là trạng thái đã thanh toán
                            db.SaveChanges();
                        }

                        // Gửi email xác nhận đơn hàng sau khi thanh toán thành công
                        int userId = Convert.ToInt32(Session["User_ID"]);
                        var user = db.Users.FirstOrDefault(m => m.ID == userId);
                        if (user != null)
                        {
                            SendOrderConfirmationEmail(user.Email);
                        }

                        foreach (var item in selectedItems)
                        {
                            cart.Remove(item);
                        }
                        Notification.set_flash("Thanh toán thành công!", "success");
                    }
                    else
                    {
                        // Thanh toán không thành công
                        Notification.set_flash("Thanh toán không thành công. Mã lỗi: " + vnp_ResponseCode, "error");
                    }
                }
                else
                {
                    Notification.set_flash("Có lỗi xảy ra trong quá trình xử lý", "error");
                }
            }

            return RedirectToAction("Index", "Cart");
        }

        public JsonResult Tesst()
        {
            if (Session["User_Name"] != null)
                return Json(1);
            return Json(0);
        }
        public JsonResult CheckAuth()
        {
            if (Session["User_Name"] != null)
                return Json(1);
            return Json(0);
        }
    }
}