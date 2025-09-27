using FGI.Enums;
using FGI.Interfaces;
using FGI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
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
        private readonly IHelperService _helperService;
        private readonly ILogger<SalesController> _logger;
        private readonly AppDbContext _context;

        public SalesController(
            ILeadService leadService,
            ILeadFeedbackService feedbackService,
            IUnitService unitService,
            IProjectService projectService,
            ILogger<SalesController> logger,
            IHelperService helperService,
            AppDbContext context)
        {
            _leadService = leadService;
            _feedbackService = feedbackService;
            _unitService = unitService;
            _projectService = projectService;
            _logger = logger;
            _helperService = helperService;
            _context = context;
        }

        [Authorize(Roles = "Sales")]
        public async Task<IActionResult> MyTasks()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Forbid();

            var tasks = await _context.Leads
                .Where(l => l.AssignedToId == userId.Value
                    && l.CurrentStatus == LeadStatusType.New)
                .Include(l => l.Unit)
                .Include(l => l.Project)
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
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null) return Json(new { success = false, message = "User not authenticated" });

                // Check if unit code already exists in the same project
                if (!string.IsNullOrWhiteSpace(unit.UnitCode) && unit.ProjectId.HasValue)
                {
                    var existingUnit = await _context.Units
                        .FirstOrDefaultAsync(u => u.UnitCode == unit.UnitCode && u.ProjectId == unit.ProjectId);
                    
                    if (existingUnit != null)
                    {
                        return Json(new { 
                            success = false, 
                            message = $"Unit code '{unit.UnitCode}' already exists in this project. Please choose a different unit code." 
                        });
                    }
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(unit.Location))
                {
                    return Json(new { 
                        success = false, 
                        message = "Location is required. Please enter the unit location." 
                    });
                }

                if (unit.Price <= 0)
                {
                    return Json(new { 
                        success = false, 
                        message = "Price must be greater than 0. Please enter a valid price." 
                    });
                }

                if (unit.Area <= 0)
                {
                    return Json(new { 
                        success = false, 
                        message = "Area must be greater than 0. Please enter a valid area." 
                    });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return Json(new { 
                        success = false, 
                        message = "Validation failed", 
                        errors = errors 
                    });
                }

                // Set creation details
                unit.CreatedById = userId.Value;
                unit.CreatedAt = DateTime.Now;
                unit.IsAvailable = true;

                await _unitService.AddUnitAsync(unit);

                return Json(new { success = true, message = "Unit added successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding unit: {Error}", ex.Message);
                
                // Provide more specific error messages
                if (ex.Message.Contains("duplicate"))
                {
                    return Json(new { 
                        success = false, 
                        message = "This unit already exists. Please check the unit code and project." 
                    });
                }
                
                if (ex.Message.Contains("constraint"))
                {
                    return Json(new { 
                        success = false, 
                        message = "Invalid data provided. Please check all fields and try again." 
                    });
                }

                return Json(new { 
                    success = false, 
                    message = "An unexpected error occurred while adding the unit. Please try again or contact support if the problem persists." 
                });
            }
        }
        [HttpGet]
        public async Task<IActionResult> EditUnit(int id)
        {
            try
            {
                var unit = await _unitService.GetUnitByIdAsync(id);
                if (unit == null)
                {
                    TempData["ErrorMessage"] = "Unit not found";
                    return RedirectToAction(nameof(MyUnits));
                }

                await LoadViewData();
                return View(unit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading unit for editing");
                TempData["ErrorMessage"] = "Error loading unit for editing";
                return RedirectToAction(nameof(MyUnits));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUnit(int id, Unit unit)
        {
            try
            {
                if (id != unit.Id)
                {
                    TempData["ErrorMessage"] = "Unit ID mismatch";
                    return RedirectToAction(nameof(MyUnits));
                }

                // Clean numeric values from formatting
                if (!string.IsNullOrEmpty(Request.Form["Price"]))
                {
                    var priceValue = Request.Form["Price"].ToString().Replace(",", "");
                    if (decimal.TryParse(priceValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cleanPrice))
                    {
                        unit.Price = cleanPrice;
                    }
                    else
                    {
                        ModelState.AddModelError("Price", "Please enter a valid price");
                    }
                }

                if (!ModelState.IsValid)
                {
                    await LoadViewData();
                    return View(unit);
                }
                var existingUnit = _context.Units.AsNoTracking().FirstOrDefault(u => u.Id == unit.Id);
                await _unitService.UpdateUnitAsync(unit);
                TempData["SuccessMessage"] = "Unit updated successfully";
                return RedirectToAction(nameof(MyUnits));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating unit");
                TempData["ErrorMessage"] = "Error updating unit";
                await LoadViewData();
                return View(unit);
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyUnits()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Forbid();

            var units = await _unitService.GetUnitsByCreatorAsync(userId.Value);
            units = units.OrderByDescending(u => u.CreatedAt).ToList();
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
                .Where(l => l.AssignedToId == userId)
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
                        UnitType = UnitSaleType.Sale,
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
                        redirectUrl = Url.Action("MyLeads", "Sales")
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

        [HttpGet]
        public async Task<IActionResult> GetUnitDetails(int id)
        {
            try
            {
                var unit = await _unitService.GetUnitByIdAsync(id);
                if (unit == null)
                {
                    return NotFound();
                }

                // تأكد من تحميل العلاقات إذا لزم الأمر
                unit = await _context.Units
                    .Include(u => u.Project)
                    .Include(u => u.CreatedBy)
                    .Include(u => u.Owner)
                    .FirstOrDefaultAsync(u => u.Id == id);

                return PartialView("_UnitDetailsPartial", unit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unit details");
                return StatusCode(500, "Error loading unit details");
            }
        }

        [HttpGet]
        public async Task<IActionResult> AvailableUnits(
       UnitType? type = null,
       int? projectId = null,
       decimal? minPrice = null,
       decimal? maxPrice = null,
       int? bedrooms = null,
       decimal? minArea = null,
       int? bathrooms = null,
       UnitSaleType? saleType = null,
       string searchTerm = null)
        {
            try
            {
                var units = await _unitService.GetFilteredUnitsAsync(type, projectId, minPrice, maxPrice, bedrooms, true, minArea, bathrooms, saleType, searchTerm);

                var projects = await _projectService.GetAllProjectsAsync();
                ViewBag.Projects = new SelectList(projects, "Id", "Name");

                return View(units);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available units");
                TempData["ErrorMessage"] = "Error loading available units. Please try again.";
                return RedirectToAction("Index");
            }
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
            return _helperService.GetCurrentUserId();
        }
        [HttpGet]
        public async Task<IActionResult> SearchOwner(string term)
        {
            try
            {
                var result = await _leadService.SearchOwnerAsync(term);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching owner");
                return Json(new
                {
                    found = false,
                    error = "An error occurred while searching"
                });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOwnerAjax(string name, string phone, string email)
        {
            try
            {
                var result = await _leadService.AddOwnerAsync(name, phone, email);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding owner");
                return Json(new { success = false, message = "Error adding owner" });
            }
        }
        private async Task LoadViewData()
        {
            var projects = await _projectService.GetAllProjectsAsync();
            ViewBag.Projects = new SelectList(projects, "Id", "Name");

            var owners = await _context.Owners.ToListAsync();
            ViewBag.Owners = new SelectList(owners, "Id", "Name");
        }
    }
}