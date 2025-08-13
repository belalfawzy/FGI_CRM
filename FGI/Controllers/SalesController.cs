using FGI.Enums;
using FGI.Interfaces;
using FGI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace FGI.Controllers
{
    [Authorize(Roles = "Sales")]
    public class SalesController : Controller
    {
        private readonly ILeadService _leadService;
        private readonly ILeadFeedbackService _feedbackService;
        private readonly IUnitService _unitService;
        private readonly IProjectService _projectService;
        private readonly ILogger<SalesController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppDbContext _context;

        public SalesController(
            ILeadService leadService,
            ILeadFeedbackService feedbackService,
            IUnitService unitService,
            IProjectService projectService,
            ILogger<SalesController> logger,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context)
        {
            _leadService = leadService;
            _feedbackService = feedbackService;
            _unitService = unitService;
            _projectService = projectService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        [Authorize(Roles = "Sales")]
        public async Task<IActionResult> MyTasks()
        {

            var userId = GetCurrentUserId();
            if (userId == null) return Forbid();

            var tasks = await _context.Leads
    .Where(l => l.AssignedToId == userId.Value
        && l.CurrentStatus == LeadStatusType.New).Include(l=>l.Unit)
    .ToListAsync();
            return View(tasks);
        }

        [HttpGet]
        public async Task<IActionResult> AddUnit()
        {
            await LoadProjects();
            return View(new Unit());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUnit(Unit unit)
        {
            if (!ModelState.IsValid)
            {
                await LoadProjects();
                return View(unit);
            }

            try
            {
                var userId = GetCurrentUserId();
                if (userId == null) return Forbid();

                // تحقق من وجود OwnerNumber مسبقًا
                if (!string.IsNullOrEmpty(unit.OwnerPhone))
                {
                    bool exists = await _context.Units
                        .AnyAsync(u => u.OwnerPhone == unit.OwnerPhone);

                    if (exists)
                    {
                        ModelState.AddModelError(nameof(unit.OwnerPhone), "This owner phone number is already registered.");
                        await LoadProjects();
                        return View(unit);
                    }
                }

                unit.CreatedById = userId.Value;
                unit.CreatedAt = DateTime.Now;
                unit.IsAvailable = true; // Default to available when creating

                await _unitService.AddUnitAsync(unit);

                TempData["SuccessMessage"] = "Unit added successfully";
                return RedirectToAction(nameof(MyUnits));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding unit");
                TempData["ErrorMessage"] = "Error adding unit. Please try again.";
                await LoadProjects();
                return View(unit);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditUnit(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Forbid();

            var unit = await _unitService.GetUnitByIdAsync(id);
            if (unit == null || unit.CreatedById != userId)
            {
                return NotFound();
            }

            await LoadProjects();
            return View(unit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUnit(int id, Unit unit)
        {
            if (id != unit.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await LoadProjects();
                return View(unit);
            }

            try
            {
                var userId = GetCurrentUserId();
                if (userId == null) return Forbid();

                var existingUnit = await _unitService.GetUnitByIdAsync(id);
                if (existingUnit == null || existingUnit.CreatedById != userId)
                {
                    return NotFound();
                }

                // Preserve some original values
                unit.CreatedById = existingUnit.CreatedById;
                unit.CreatedAt = existingUnit.CreatedAt;

                await _unitService.UpdateUnitAsync(unit);

                TempData["SuccessMessage"] = "Unit updated successfully";
                return RedirectToAction(nameof(MyUnits));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating unit");
                TempData["ErrorMessage"] = "Error updating unit. Please try again.";
                await LoadProjects();
                return View(unit);
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyUnits()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Forbid();

            var units = await _unitService.GetUnitsByCreatorAsync(userId.Value);
            return View(units);
        }

        [Authorize(Roles = "Sales")]
        public async Task<IActionResult> MyLeads()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Forbid();

            var leads = await _context.Leads
                .Include(l => l.Project)
                .Include(l => l.Unit)
                .Include(l => l.AssignedTo)
                .Where(l => l.AssignedToId == userId &&
                            l.CurrentStatus != LeadStatusType.DoneDeal &&
                            l.CurrentStatus != LeadStatusType.Canceled)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            // لو الـ Project أو Unit مش موجودين، نعمل قيم افتراضية
            foreach (var lead in leads)
            {
                if (lead.Project == null)
                {
                    lead.Project = new Project
                    {
                        Name = "N/A"
                    };
                }

                if (lead.Unit == null)
                {
                    lead.Unit = new Unit
                    {
                        UnitCode = "N/A",
                        Type = UnitType.Apartment,
                        Price = 0,
                        Area = 0
                    };
                }
            }

            return View(leads);
        }

        [HttpGet]
        public async Task<IActionResult> LeadDetails(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Forbid();

            var lead = await _leadService.GetLeadByIdAsync(id);
            if (lead == null || lead.AssignedToId != userId)
            {
                return NotFound();
            }

            ViewBag.Feedbacks = await _feedbackService.GetFeedbacksByLeadAsync(id);
            ViewBag.AvailableUnits = await _unitService.GetAvailableUnitsAsync();

            return View(lead);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignUnitToLead(int leadId, int unitId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Forbid();

            try
            {
                var lead = await _leadService.GetLeadByIdAsync(leadId);
                if (lead == null || lead.AssignedToId != userId)
                {
                    return NotFound();
                }

                var unit = await _unitService.GetUnitByIdAsync(unitId);
                if (unit == null || !unit.IsAvailable)
                {
                    TempData["ErrorMessage"] = "Selected unit is not available";
                    return RedirectToAction(nameof(LeadDetails), new { id = leadId });
                }

                lead.UnitId = unitId;
                unit.IsAvailable = false;

                await _leadService.UpdateLeadAsync(lead);
                await _unitService.UpdateUnitAsync(unit);

                TempData["SuccessMessage"] = "Unit assigned successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning unit to lead");
                TempData["ErrorMessage"] = "Error assigning unit to lead";
            }

            return RedirectToAction(nameof(LeadDetails), new { id = leadId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int leadId, LeadStatusType status, string comment)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "User not found" });

                return RedirectToAction(nameof(LeadDetails), new { id = leadId });
            }

            try
            {
                var lead = await _context.Leads
                    .Include(l => l.AssignedTo)
                    .FirstOrDefaultAsync(l => l.Id == leadId && l.AssignedToId == userId);

                if (lead == null)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Lead not found" });

                    return RedirectToAction(nameof(LeadDetails), new { id = leadId });
                }

                if (status == LeadStatusType.New || status == lead.CurrentStatus)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Invalid status change" });

                    return RedirectToAction(nameof(LeadDetails), new { id = leadId });
                }

                // تحديث حالة الـ Lead
                lead.CurrentStatus = status;
                lead.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // إضافة تعليق للـ Feedback
                await _feedbackService.AddFeedbackAsync(
                    leadId,
                    userId.Value,
                    status,
                    comment
                );

                // إذا كان الطلب AJAX، رجع JSON
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = true,
                        message = "Status updated successfully",
                        redirectUrl = Url.Action("MyAssignedLeads", "Sales")
                    });
                }

                // في حالة الطلب العادي
                TempData["SuccessMessage"] = "Lead status updated successfully";
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Error updating lead status" });

                TempData["ErrorMessage"] = "Error updating lead status";
            }

            return RedirectToAction(nameof(LeadDetails), new { id = leadId });
        }


        private async Task LoadProjects()
        {
            var projects = await _projectService.GetAllProjectsAsync();
            ViewBag.Projects = projects.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Name
            }).ToList();
        }

        private int? GetCurrentUserId()
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userId, out int id) ? id : null;
        }
        [HttpGet]
        public async Task<IActionResult> GetUnitDetails(int id)
        {
            var unit = await _unitService.GetUnitByIdAsync(id);
            if (unit == null)
            {
                return NotFound();
            }

            return PartialView("_UnitDetailsPartial", unit);
        }
        [HttpGet]
        public async Task<IActionResult> AvailableUnits(
    UnitType? type = null,
    int? projectId = null,
    decimal? minPrice = null,
    decimal? maxPrice = null,
    int? bedrooms = null,
    decimal? minArea = null,
    int? bathrooms = null)
        {
            try
            {
                // Get base query for available units only
                var query = _context.Units
                    .Include(u => u.Project)
                    .Where(u => u.IsAvailable) // Only available units
                    .AsQueryable();

                // Apply filters
                if (type.HasValue)
                    query = query.Where(u => u.Type == type.Value);

                if (projectId.HasValue)
                    query = query.Where(u => u.ProjectId == projectId.Value);

                if (minPrice.HasValue)
                    query = query.Where(u => u.Price >= minPrice.Value);

                if (maxPrice.HasValue)
                    query = query.Where(u => u.Price <= maxPrice.Value);

                if (bedrooms.HasValue)
                    query = bedrooms.Value == 4 ?
                        query.Where(u => u.Bedrooms >= 4) :
                        query.Where(u => u.Bedrooms == bedrooms.Value);

                if (minArea.HasValue)
                    query = query.Where(u => u.Area >= minArea.Value);

                if (bathrooms.HasValue)
                    query = bathrooms.Value == 3 ?
                        query.Where(u => u.Bathrooms >= 3) :
                        query.Where(u => u.Bathrooms == bathrooms.Value);

                // Get projects for dropdown
                var projects = await _projectService.GetAllProjectsAsync();
                ViewBag.Projects = new SelectList(projects, "Id", "Name");

                // Execute query
                var units = await query
                    .OrderBy(u => u.Project.Name)
                    .ThenBy(u => u.UnitCode)
                    .ToListAsync();

                return View(units);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available units");
                TempData["ErrorMessage"] = "Error loading units data";
                return View(new List<Unit>());
            }
        }

    }
}
