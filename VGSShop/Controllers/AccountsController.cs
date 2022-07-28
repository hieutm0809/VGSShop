using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using VGSShop.Enums;
using VGSShop.Extension;
using VGSShop.Helpers;
using VGSShop.Models;
using VGSShop.ModelsView;

namespace VGSShop.Controllers
{
    [Authorize]
    public class AccountsController : Controller
    {
        private readonly VGSShopContext _context;
        public INotyfService _notyfService { get; }

        public AccountsController(VGSShopContext context, INotyfService notyfService)
        {
            _context = context;
            _notyfService = notyfService;
        }

        [Route("tai-khoan-cua-toi.html", Name = "dashBoard")]
        public IActionResult dashBoard()
        {
            var accountId = HttpContext.Session.GetString("CustomerId");
            if (accountId != null)
            {
                var customer = _context.Customers.AsNoTracking()
                    .SingleOrDefault(x => x.CustomerId == Convert.ToInt32(accountId));
                if (customer != null)
                {
                    var lsOrder = _context.Orders
                        .Include(x => x.TransactStatus)
                        .AsNoTracking()
                        .Where(x => x.CustomerId == customer.CustomerId)
                        .OrderByDescending(x => x.OrderDate).ToList();
                    ViewBag.Order = lsOrder;
                    return View(customer);
                }
            }
            return RedirectToAction("login");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult validatePhone(string Phone)
        {
            try
            {
                var customer = _context.Customers.AsNoTracking().SingleOrDefault(x => x.Phone.ToLower() == Phone.ToLower());
                if (customer != null)
                    return Json(data: "Số điện thoại : " + Phone + "Đã được sử dụng");
                return Json(data: true);
            }
            catch
            {
                return Json(data: true);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult validateEmail(string Email)
        {
            try
            {
                var customer = _context.Customers.AsNoTracking().SingleOrDefault(x => x.Email.ToLower() == Email.ToLower());
                if (customer != null)
                    return Json(data: "Email : " + Email + "Đã được sử dụng");
                return Json(data: true);
            }
            catch
            {
                return Json(data: true);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("dang-ky.html", Name = "registerAccount")]
        public IActionResult registerAccount()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("dang-ky.html", Name = "registerAccount")]
        public async Task<IActionResult> registerAccount(RegisterVM account, string email)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string salt = Utilities.GetRandomKey();
                    Customer customer = new Customer
                    {
                        FullName = account.FullName,
                        Phone = account.Phone.Trim().ToLower(),
                        Email = account.Email.Trim().ToLower(),
                        Adress = account.Address,
                        Password = (account.Password + salt.Trim()).ToMD5(),
                        Active = true,
                        Salt = salt,
                        CreateDate = DateTime.Now
                    };
                    var kh = _context.Customers.AsNoTracking().SingleOrDefault(x => x.Email.ToLower() == email.ToLower());
                    if (kh != null)
                    {
                        ViewBag.Alert = Utilities.ShowAlert(CacheKeys.Danger, "Email không đã tồn tại.");
                        return View(account);
                    }
                    else
                    {
                        try
                        {
                            //Đăng nhập thành công -- lưu thời gian đăng nhập lại
                            customer.LastLogin = DateTime.Now;
                            _context.Add(customer);
                            await _context.SaveChangesAsync();
                            //Lưu Session Mã Khách hàng
                            HttpContext.Session.SetString("CustomerId", customer.CustomerId.ToString());
                            var accountId = HttpContext.Session.GetString("CustomerId");
                            var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, customer.FullName),
                            new Claim("CustomerId", customer.CustomerId.ToString())
                        };
                            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "login");
                            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                            await HttpContext.SignInAsync(claimsPrincipal);
                            _notyfService.Success("Đăng ký tài khoản thành công");
                            return RedirectToAction("dashBoard", "Accounts");
                        }
                        catch
                        {
                            return RedirectToAction("DangKyTaiKhoan", "Accounts");
                        }
                    }
                }
                else
                {
                    return View(account);
                }
            }
            catch
            {
                _notyfService.Error("Đăng ký tài khoản thất bại");
                return View(account);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("dang-nhap.html", Name = "DangNhap")]
        public IActionResult login(string returnUrl = null)
        {
            var accountId = HttpContext.Session.GetString("CustomerId");

            if (accountId != null)
            {
                return RedirectToAction("DangKyTaiKhoan", "Accounts");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("dang-nhap.html", Name = "DangNhap")]
        public async Task<IActionResult> login(LoginViewModel customer, string returnUrl = null)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool isEmail = Utilities.IsValidEmail(customer.Email);
                    if (!isEmail) return View(customer);
                    var khachhang = _context.Customers
                            .AsNoTracking()
                            .SingleOrDefault(x => x.Email.Trim() == customer.Email.Trim());
                    if (khachhang == null)
                    {
                        _notyfService.Error("Rất tiếc chúng tôi không tìm thấy Email của bạn");
                        return View(customer);
                    }
                    string pass = (customer.Password + khachhang.Salt.Trim()).ToMD5();
                    if (khachhang.Password != pass)
                    {
                        _notyfService.Error("Thông tin đăng nhập không chính xác");
                        return View(customer);
                    }
                    //Kiểm tra tài khoản có được Active hay là không
                    if (khachhang.Active == false) return RedirectToAction("Thongbao", "Accounts");

                    //Đăng nhập thành công -- lưu thời gian đăng nhập lại
                    khachhang.LastLogin = DateTime.Now;
                    _context.Update(khachhang);
                    await _context.SaveChangesAsync();

                    //Lưu Session MaKh
                    HttpContext.Session.SetString("CustomerId", khachhang.CustomerId.ToString());
                    var accountId = HttpContext.Session.GetString("CustomerId");
                    //Identity
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, khachhang.FullName),
                        new Claim("CustomerId", khachhang.CustomerId.ToString()),
                    };
                    ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "login");
                    ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                    await HttpContext.SignInAsync(claimsPrincipal);
                    _notyfService.Success("Đăng nhập thành công");
                    if (string.IsNullOrEmpty(returnUrl))
                    {
                        return RedirectToAction("dashBoard", "Accounts");
                    }
                    else
                    {
                        return Redirect(returnUrl);
                    }
                }
            }
            catch
            {
                return RedirectToAction("DangKyTaiKhoan", "Accounts");
            }
            return View(customer);
        }

        [HttpGet]
        [Route("dang-xuat.html", Name = "logout")]
        public IActionResult logout()
        {
            HttpContext.SignOutAsync();
            HttpContext.Session.Remove("CustomerId");
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult changePassword(ChangePasswordViewModel model)
        {
            try
            {
                var accountId = HttpContext.Session.GetString("CustomerId");
                if (accountId == null)
                {
                    return RedirectToAction("login", "Accounts");
                }
                if (ModelState.IsValid)
                {
                    var account = _context.Customers.Find(Convert.ToInt32(accountId));
                    if (account == null) return RedirectToAction("Login", "Accounts");

                    var pass = (model.PasswordNow.Trim() + account.Salt.Trim()).ToMD5();
                    if (pass == account.Password)
                    {
                        string newpass = (model.Password.Trim() + account.Salt.Trim()).ToMD5();
                        account.Password = newpass;
                        _context.Update(account);
                        _context.SaveChanges();
                        _notyfService.Success("Đổi mật khẩu thành công");
                        return RedirectToAction("dashBoard", "Accounts");
                    }
                }
            }
            catch
            {
                _notyfService.Error("Đổi mật khẩu không thành công");
                return RedirectToAction("dashBoard", "Accounts");
            }
            _notyfService.Error("Đổi mật khẩu không thành công");
            return RedirectToAction("dashBoard", "Accounts");
        }

        public async Task<IActionResult> details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.TransactStatus)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            var orderDetail = _context.OrderDetails
                .Include(x => x.Product)
                .AsNoTracking()
                .Where(x => x.OrderId == order.OrderId)
                .OrderBy(x => x.OrderDetailId)
                .ToList();
            ViewBag.ChiTiet = orderDetail;
            return View(order);
        }
    }
}