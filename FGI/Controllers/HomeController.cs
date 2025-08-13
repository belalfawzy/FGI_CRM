using FGI.Enums;
using FGI.Interfaces;
using FGI.Models;
using FGI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FGI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILeadService _leadService;
        private readonly AppDbContext _context;
        private readonly IUnitService _unitService;

        public HomeController(ILeadService leadService, AppDbContext context, IUnitService unitService)
        {
            _leadService = leadService;
            _context = context;
            _unitService = unitService;
        }
        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            ViewBag.UserRole = role;

            // For all users
            var unassignedCount = await _context.Leads
               .CountAsync(l => l.AssignedToId == null);
            ViewBag.UnassignedLeadsCount = unassignedCount;

            // For Sales users
            if (role == "Sales")
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userId, out int salesPersonId))
                {
                    ViewBag.NewLeadsCount = await _context.Leads
                        .CountAsync(l => l.AssignedToId == salesPersonId &&
                                       l.CurrentStatus == LeadStatusType.New);

                    ViewBag.ActiveLeadsCount = await _context.Leads
                        .CountAsync(l => l.AssignedToId == salesPersonId &&
                                       l.CurrentStatus != LeadStatusType.New &&
                                       l.CurrentStatus != LeadStatusType.DoneDeal &&
                                       l.CurrentStatus != LeadStatusType.Canceled);

                    ViewBag.UnitsCount = await _context.Units
                        .CountAsync(u => u.CreatedById == salesPersonId);
                }
            }

            return View();
        }
    }
}