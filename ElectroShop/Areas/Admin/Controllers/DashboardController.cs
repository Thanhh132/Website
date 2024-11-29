using ElectroShop.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ElectroShop.Areas.Admin.Controllers
{
    public class DashboardController : BaseController
    {
        private ElectroShopDbContext db = new ElectroShopDbContext();
        // GET: Admin/Dashboard
        public ActionResult Index()
        {
            ViewBag.CountOrderSuccess = db.Orders.Where(m => m.Status == 3).Count();
            ViewBag.CountOrderCancel = db.Orders.Where(m => m.Status == 1).Count();
            ViewBag.CountContactDoneReply = db.Contacts.Where(m => m.Flag == 0).Count();
            ViewBag.CountUser = db.Users.Where(m => m.Status != 0 && m.Access==0).Count();

			decimal tongDoanhThu = ThongKeDoanhThuTrongNgay();
			ViewBag.DoanhThuTrongNgay = tongDoanhThu;
			int namHienTai = DateTime.Now.Year;
			TongTienThuVaoTheoNam(namHienTai);

			return View();
        }
		public decimal TongTienThuVaoTheoNam(int nam)
		{
			var donHangTheoNam = db.Orders
				.Join(db.Orderdetails,
					order => order.Id,
					orderDetail => orderDetail.OrderId,
					(order, orderDetail) => new { Order = order, OrderDetail = orderDetail })
				.Where(joinResult => joinResult.Order.Status == 3 && joinResult.Order.CreateDate.Year == nam)
				.ToList();

			decimal tongTienThu = donHangTheoNam
				.Sum(joinResult => (decimal?)(Convert.ToDecimal(joinResult.OrderDetail.Quantity) * Convert.ToDecimal(joinResult.OrderDetail.Price))) ?? 0;

			ViewBag.TongTienThuVaoTheoNam = tongTienThu;

			return tongTienThu;
		}
		public decimal ThongKeDoanhThuTrongNgay()
		{
			DateTime ngayHienTai = DateTime.Today;

			var orderDetails = db.Orderdetails
				.Where(od => db.Orders.Any(o => o.Id == od.OrderId && o.Status == 3 && DbFunctions.TruncateTime(o.CreateDate) == ngayHienTai))
				.ToList();

			decimal doanhThuNgayHienTai = orderDetails
			.Select(od => (decimal?)od.Quantity * (decimal?)od.Price)
			.DefaultIfEmpty(0)
			.Sum() ?? 0;


			return doanhThuNgayHienTai;
		}
	}
}