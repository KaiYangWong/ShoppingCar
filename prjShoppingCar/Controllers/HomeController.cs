using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using prjShoppingCar.Models;

namespace prjShoppingCar.Controllers
{
    public class HomeController : Controller
    {
        dbShoppingCarEntities db = new dbShoppingCarEntities();
        public ActionResult Index()
        {
            var products = db.tProduct.ToList();

            //若Session["Member"]為空，表示會員未登入
            if(Session["Member"] == null)
            {
                return View("Index","_Layout",products);
            }

            //會員登入狀態
            return View("Index","_LayoutMember",products);
        }

        //登入
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string fUserId,string fPwd)
        {
            //依據帳號密碼取得會員
            var member = db.tMember
                           .Where(m => m.fUserId == fUserId && m.fPwd == fPwd)
                           .FirstOrDefault();

            //若member為null，表示會員未註冊
            if(member == null)
            {
                ViewBag.Message = "帳密錯誤，登入失敗";
                return View();
            }

            Session["Welcome"] = member.fName + "歡迎光臨";
            Session["Member"] = member;
            return RedirectToAction("Index");
        }

        //註冊
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(tMember pMember)
        {
            //未通過驗證則顯示目前的頁面
            if(ModelState.IsValid == false)
            {
                return View();
            }

            var member = db.tMember
                           .Where(m => m.fUserId == pMember.fUserId)
                           .FirstOrDefault();

            if(member == null)
            {
                db.tMember.Add(pMember);
                db.SaveChanges();
                return RedirectToAction("Login");
            }

            ViewBag.Message = "此帳號已有人使用，註冊失敗";
            return View();
        }

        //登出
        //清除Session變數，返回到產品列表
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index");
        }

        //顯示會員的購物車清單
        public ActionResult ShoppingCar()
        {
            string fUserId = (Session["Member"] as tMember).fUserId;

            var orderDetails = db.tOrderDetail
                                 .Where(m => m.fUserId == fUserId && m.fIsApproved == "否")
                                 .ToList();

            return View("ShoppingCar","_LayoutMember",orderDetails);
        }

        [HttpPost]
        public ActionResult ShoppingCar(string fReceiver,string fEmail, string fAddress)
        {
            string fUserId = (Session["Member"] as tMember).fUserId;
            
            //建立識別值當作訂單編號
            string guid = Guid.NewGuid().ToString();

            //建立訂單主檔資料
            tOrder order = new tOrder();
            order.fOrderGuid = guid;
            order.fUserId = fUserId;
            order.fReceiver = fReceiver;
            order.fEmail = fEmail;
            order.fAddress = fAddress;
            order.fDate = DateTime.Now;

            //找出目前會員在訂單明細中是購物車狀態的產品
            var carList = db.tOrderDetail
                            .Where(m => m.fIsApproved == "否" && m.fUserId == fUserId)
                            .ToList();

            //將購物車狀態產品的fIsApproved設為"是"，表示確認訂購產品
            foreach(var item in carList)
            {
                item.fOrderGuid = guid;
                item.fIsApproved = "是";
            }

            //完成訂單主檔和訂單明細的更新
            db.SaveChanges();
            return RedirectToAction("OrderList");
        }

        //加入購物車
        public ActionResult AddCar(string fPId)
        {
            //取得會員帳號
            string fUserId = (Session["Member"] as tMember).fUserId;

            var currentCar = db.tOrderDetail
                               .Where(m => m.fPId == fPId && m.fIsApproved == "否" && m.fUserId == fUserId)
                               .FirstOrDefault();

            //表示會員選購的產品不是購物車狀態
            if(currentCar == null)
            {
                var product = db.tProduct
                                .Where(m => m.fPId == fPId)
                                .FirstOrDefault();
                
                tOrderDetail orderDetail = new tOrderDetail();
                orderDetail.fUserId = fUserId;
                orderDetail.fPId = product.fPId;
                orderDetail.fName = product.fName;
                orderDetail.fPrice = product.fPrice;
                orderDetail.fQty = 1;
                orderDetail.fIsApproved = "否";
                db.tOrderDetail.Add(orderDetail);
            }
            else
            {
                //若產品為購物車狀態，即將該產品數量加1
                currentCar.fQty += 1;
            }

            db.SaveChanges();
            return RedirectToAction("ShoppingCar");
        }

        //刪除
        public ActionResult DeleteCar(int fId)
        {
            var orderDetail = db.tOrderDetail
                                .Where(m => m.fId == fId)
                                .FirstOrDefault();
            db.tOrderDetail.Remove(orderDetail);
            db.SaveChanges();
            return RedirectToAction("ShoppingCar");
        }

        //顯示登入會員的訂單主檔
        public ActionResult OrderList()
        {
            string fUserId = (Session["Member"] as tMember).fUserId;

            var orders = db.tOrder
                           .Where(m => m.fUserId == fUserId)
                           .OrderByDescending(m => m.fDate)
                           .ToList();

            //回傳到目前的會員主檔
            return View("OrderList","_LayoutMember",orders);
        }

        public ActionResult OrderDetail(string fOrderGuid)
        {
            var orderDetails = db.tOrderDetail
                                 .Where(m => m.fOrderGuid == fOrderGuid)
                                 .ToList();
            return View("OrderDetail", "_LayoutMember",orderDetails);
        }
    }
}