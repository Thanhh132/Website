using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ElectroShop
{
    public class Notification
    {
        public static bool has_flash()
        {
            // Kiểm tra nếu Session["Notification"] là null
            if (System.Web.HttpContext.Current.Session["Notification"] == null ||
                System.Web.HttpContext.Current.Session["Notification"].Equals(""))
            {
                return false;
            }

            // Nếu không null và không rỗng, trả về true hoặc xử lý thêm
            return true;
        }

        public static void set_flash(String mgs, String mgs_type)
        {
            ModelNotification tb = new ModelNotification();
            tb.mgs = mgs;
            tb.mgs_type = mgs_type;

            System.Web.HttpContext.Current.Session["Notification"] = tb;
        }
        public static ModelNotification get_flash()
        {

            ModelNotification Notifi = (ModelNotification)System.Web.HttpContext.Current.Session["Notification"];
            System.Web.HttpContext.Current.Session["Notification"] = "";
            return Notifi;
        }
    }
}