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
                    && l.CurrentStatus == LeadStatusType.New)
                .Include(l => l.Unit)
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

                // Remove the owner existence check since we're allowing selection of existing owners
                unit.CreatedById = userId.Value;
                unit.CreatedAt = DateTime.Now;
                unit.IsAvailable = true;

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
                // Get base query for available units only
                var query = _context.Units
            .Include(u => u.Project)
            .Include(u => u.Owner) // Include Owner for phone number search
            .Where(u => u.IsAvailable)
            .AsQueryable();

                // Apply search term filter if provided
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var normalizedSearchTerm = searchTerm.Trim().ToLower();
                    query = query.Where(u =>
                        (u.UnitCode != null && u.UnitCode.ToLower().Contains(normalizedSearchTerm)) ||
                        (u.Owner != null && u.Owner.Phone != null && u.Owner.Phone.Contains(normalizedSearchTerm))
                    );
                }

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

                if (saleType.HasValue)
                    query = query.Where(u => u.UnitType == saleType.Value);

                // Get projects for dropdown
                var projects = await _projectService.GetAllProjectsAsync();
                ViewBag.Projects = new SelectList(projects, "Id", "Name");

                // Execute query with sorting by CreatedAt descending (newest first)
                var units = await query
            .OrderByDescending(u => u.CreatedAt)
            .ThenBy(u => u.Project.Name)
            .ThenBy(u => u.UnitCode)
            .ToListAsync();

                units = units.OrderByDescending(u => u.CreatedAt).ToList();

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
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userId, out int id) ? id : null;
        }
        [HttpGet]
        public async Task<IActionResult> SearchOwner(string term)
        {
            try
            {
                var cleanedPhone = new string(term.Where(char.IsDigit).ToArray());

                if (!string.IsNullOrEmpty(cleanedPhone))
                {
                    var ownerByPhone = await _context.Owners
                        .FirstOrDefaultAsync(o => o.Phone != null && o.Phone.Contains(cleanedPhone));

                    if (ownerByPhone != null)
                    {
                        return Json(new
                        {
                            found = true,
                            id = ownerByPhone.Id,
                            name = ownerByPhone.Name,
                            phone = ownerByPhone.Phone,
                            email = ownerByPhone.Email,
                            searchType = "phone"
                        });
                    }
                }

                if (!string.IsNullOrWhiteSpace(term))
                {
                    var ownerByName = await _context.Owners
                        .Where(o => o.Name.Contains(term))
                        .FirstOrDefaultAsync();

                    if (ownerByName != null)
                    {
                        return Json(new
                        {
                            found = true,
                            id = ownerByName.Id,
                            name = ownerByName.Name,
                            phone = ownerByName.Phone,
                            email = ownerByName.Email,
                            searchType = "name"
                        });
                    }
                }

                return Json(new { found = false });
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
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "Owner name is required" });
                }

                var owner = new Owner
                {
                    Name = name,
                    Phone = phone,
                    Email = email,
                    CreatedAt = DateTime.Now
                };

                _context.Owners.Add(owner);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    ownerId = owner.Id,
                    message = "Owner added successfully"
                });
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