﻿using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VGSShop.Models;

namespace VGSShop.Controllers
{
    public class LocationController : Controller
    {
        private readonly VGSShopContext _context;
        public INotyfService _notyfService { get; }
        public LocationController(VGSShopContext context, INotyfService notyfService)
        {
            _context = context;
            _notyfService = notyfService;
        }
        public IActionResult Index()
        {
            return View();
        }
        public ActionResult QuanHuyenList(int LocationId)
        {
            var QuanHuyens = _context.Locations.OrderBy(x => x.LocationId)
                .Where(x => x.ParentCode == LocationId && x.Levels == 2)
                .OrderBy(x => x.Name)
                .ToList();
            return Json(QuanHuyens);
        }
        public ActionResult PhuongXaList(int LocationId)
        {
            var PhuongXas = _context.Locations.OrderBy(x => x.LocationId)
                .Where(x => x.ParentCode == LocationId && x.Levels == 3)
                .OrderBy(x => x.Name)
                .ToList();
            return Json(PhuongXas);
        }
    }
}
