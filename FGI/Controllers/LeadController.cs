using FGI.Enums;
using FGI.Interfaces;
using FGI.Models;
using FGI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace FGI.Controllers
{
    [Authorize(Roles = "Admin,Marketing")]
    public class LeadController : Controller
    {
        private readonly ILeadService _leadService;
        private readonly IProjectService _projectService;
        private readonly IUnitService _unitService;
        private readonly IUserService _userService;
        private readonly ILeadFeedbackService _feedbackService;
        private readonly AppDbContext _context;
        private readonly ILogger<LeadController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LeadController(
            ILeadService leadService,
            IProjectService projectService,
            IUnitService unitService,
            IUserService userService,
            ILeadFeedbackService feedbackService,
            AppDbContext context,
            ILogger<LeadController> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _leadService = leadService;
            _projectService = projectService;
            _unitService = unitService;
            _userService = userService;
            _feedbackService = feedbackService;
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        /* for choose unitCode */
        [HttpGet("Lead/GetUnits")]
        public async Task<IActionResult> GetUnits(int projectId = 0, string term = "")
        {
            try
            {
                IEnumerable<Unit> units;

                if (projectId > 0)
                {
                    units = await _unitService.GetUnitsByProjectIdAsync(projectId);
                }
                else
                {
                    units = await _unitService.GetAvailableUnitsAsync();
                }

                if (!string.IsNullOrWhiteSpace(term))
                {
                    term = term.Trim();
                    units = units.Where(u =>
                        (!string.IsNullOrWhiteSpace(u.UnitCode) && u.UnitCode.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(u.Location) && u.Location.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(u.Description) && u.Description.Contains(term, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                var results = units.OrderBy(u => u.UnitCode).Select(u => new
                {
                    id = u.Id,
                    text = $"{(string.IsNullOrWhiteSpace(u.UnitCode) ? "NA" : u.UnitCode)} - {(string.IsNullOrWhiteSpace(u.Location) ? "NA" : u.Location)}",
                    disabled = !u.IsAvailable,
                    projectId = u.ProjectId, // Include project ID in the response
                    unit = new
                    {
                        UnitCode = string.IsNullOrWhiteSpace(u.UnitCode) ? "NA" : u.UnitCode,
                        UnitType = u.Type,
                        UnitSaleType = u.UnitType,
                        Price = u.Price,
                        Area = u.Area
                    }
                }).ToList();

                return Json(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching units");
                return Json(new List<object>());
            }
        }
        /*-----------------------------------------------------------------------*/
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var currentUser = await _userService.GetByIdAsync(userId);

            List<Lead> leads;

            if (User.IsInRole("Admin"))
            {
                leads = await _context.Leads
                    .Include(l => l.Project)
                    .Include(l => l.AssignedTo)
                    .ToListAsync();
            }
            else if (User.IsInRole("Marketing"))
            {
                leads = await _context.Leads
                    .Where(l => l.CreatedById == userId)
                    .Include(l => l.Project)
                    .Include(l => l.Unit)
                    .Include(l => l.AssignedTo)
                    .ToListAsync();
            }
            else
            {
                leads = await _leadService.GetLeadsBySalesPersonAsync(userId);
            }

            // لو البروجيكت مش موجود نحط NA
            foreach (var lead in leads)
            {
                if (lead.Project == null)
                {
                    lead.Project = new Project
                    {
                        Name = "NA"
                    };
                }
            }

            return View(leads);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var projects = await _projectService.GetAllProjectsAsync();
            ViewBag.Projects = new SelectList(projects, "Id", "Name");
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Lead lead)
        {
            try
            {
                // Normalize phone number for checking
                var normalizedPhone = PhoneNumberHelper.NormalizePhoneNumber(lead.ClientPhone);

                // Basic phone validation
                if (string.IsNullOrWhiteSpace(normalizedPhone) || normalizedPhone.Length < 8)
                {
                    ModelState.AddModelError("ClientPhone", "Please enter a valid phone number (at least 8 digits)");
                }

                // Validate unit selection
                if (!lead.UnitId.HasValue)
                {
                    ModelState.AddModelError("UnitId", "Please select a unit");
                }
                else
                {
                    // Handle project association based on unit
                    var unit = await _unitService.GetUnitByIdAsync(lead.UnitId.Value);
                    if (unit != null && unit.ProjectId.HasValue)
                    {
                        lead.ProjectId = unit.ProjectId;
                    }
                    else
                    {
                        lead.ProjectId = null;
                    }
                }

                if (!ModelState.IsValid)
                {
                    var projects = await _projectService.GetAllProjectsAsync();
                    ViewBag.Projects = new SelectList(projects, "Id", "Name", lead.ProjectId);

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Validation failed",
                            errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                        });
                    }
                    return View(lead);
                }

                // Check for duplicates (just for warning, not blocking)
                var duplicateWarning = "";
                if (!string.IsNullOrEmpty(normalizedPhone))
                {
                    var existingLeads = await _context.Leads.ToListAsync();
                    var duplicateLead = existingLeads.FirstOrDefault(l =>
                        PhoneNumberHelper.NormalizePhoneNumber(l.ClientPhone) == normalizedPhone);

                    if (duplicateLead != null)
                    {
                        duplicateWarning = $"Note: This phone number is already associated with lead ID: {duplicateLead.Id}";
                    }
                }

                // Set creation details
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                lead.CreatedById = userId;
                lead.CreatedAt = DateTime.Now;

                // Add and save lead
                _context.Leads.Add(lead);
                await _context.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = true,
                        message = string.IsNullOrEmpty(duplicateWarning)
                            ? "Lead saved successfully!"
                            : $"Lead saved successfully! {duplicateWarning}",
                        redirect = Url.Action("Details", new { id = lead.Id })
                    });
                }

                TempData["Success"] = "Lead saved successfully!";
                if (!string.IsNullOrEmpty(duplicateWarning))
                {
                    TempData["Warning"] = duplicateWarning;
                }
                return RedirectToAction("Details", new { id = lead.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lead");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Error saving lead: {ex.Message}"
                    });
                }

                TempData["Error"] = $"Error saving lead: {ex.Message}";
                var projects = await _projectService.GetAllProjectsAsync();
                ViewBag.Projects = new SelectList(projects, "Id", "Name");
                return View(lead);
            }
        }

        private bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            // تحقق أساسي من رقم الهاتف
            return phone.All(c => char.IsDigit(c) || c == '+');
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reassign(int leadId, int newSalesId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var currentUser = await _userService.GetByIdAsync(currentUserId);
            var lead = await _leadService.GetLeadByIdAsync(leadId);

            if (lead == null) return NotFound();

            // التحقق من حالة الليد
            if (lead.CurrentStatus == LeadStatusType.DoneDeal || lead.CurrentStatus == LeadStatusType.Canceled)
            {
                TempData["Error"] = "لا يمكن إعادة تعيين عميل تم إنهاؤه أو إلغاؤه";
                return RedirectToAction("Details", new { id = leadId });
            }

            // التحقق من الصلاحيات
            if (User.IsInRole("Marketing") && lead.AssignedToId == null)
            {
                TempData["Error"] = "لا يمكنك إعادة تعيين عميل غير معين";
                return RedirectToAction("Details", new { id = leadId });
            }

            // تسجيل التاريخ السابق
            var oldSalesId = lead.AssignedToId;

            // تحديث البيانات
            lead.AssignedToId = newSalesId;
            await _leadService.UpdateLeadAsync(lead);

            // تسجيل في سجل التعديلات
            var history = new LeadAssignmentHistory
            {
                LeadId = leadId,
                FromSalesId = oldSalesId,
                ToSalesId = newSalesId,
                ChangedById = currentUserId,
                ChangedAt = DateTime.Now
            };

            _context.LeadAssignmentHistories.Add(history);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إعادة التعيين بنجاح";
            return RedirectToAction("Details", new { id = leadId });
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
        public async Task<IActionResult> Edit(int id)
        {
            var lead = await _context.Leads
                .Include(l => l.Project)
                .Include(l => l.Unit) // تأكد من تضمين الـ Unit
                .Include(l => l.AssignedTo)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lead == null)
            {
                return NotFound();
            }

            // Ensure projects list is never null
            var projects = await _projectService.GetAllProjectsAsync() ?? new List<Project>();
            ViewBag.Projects = new SelectList(projects, "Id", "Name", lead.ProjectId);

            return View(lead);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClientName,ClientPhone,Comment,ProjectId,UnitId,CreatedById,CreatedAt,AssignedToId,CurrentStatus")] Lead lead)
        {
            if (id != lead.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the existing lead with all relationships
                    var existingLead = await _context.Leads
                        .Include(l => l.Unit)
                        .FirstOrDefaultAsync(l => l.Id == id);

                    if (existingLead == null)
                    {
                        return NotFound();
                    }

                    // Update only the editable fields
                    existingLead.ClientName = lead.ClientName;
                    existingLead.ClientPhone = lead.ClientPhone;
                    existingLead.Comment = lead.Comment;
                    existingLead.ProjectId = lead.ProjectId;
                    existingLead.UnitId = lead.UnitId;
                    existingLead.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    if (IsAjaxRequest())
                    {
                        return Json(new
                        {
                            success = true,
                            message = "Lead updated successfully",
                            redirect = Url.Action("Details", new { id = lead.Id })
                        });
                    }

                    TempData["SuccessMessage"] = "Lead updated successfully";
                    return RedirectToAction("Details", new { id = lead.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _leadService.LeadExists(lead.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating lead");

                    if (IsAjaxRequest())
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Error updating lead: " + ex.Message,
                            errors = ModelState.Values
                                .SelectMany(v => v.Errors)
                                .Select(e => e.ErrorMessage)
                                .ToList()
                        });
                    }

                    TempData["ErrorMessage"] = "Error updating lead: " + ex.Message;
                }
            }

            // If we got this far, something failed; redisplay form
            var projects = await _projectService.GetAllProjectsAsync() ?? new List<Project>();
            ViewBag.Projects = new SelectList(projects, "Id", "Name", lead.ProjectId);

            // Reload the unit for the view
            lead.Unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == lead.UnitId);

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = false,
                    message = "Validation failed",
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            return View(lead);
        }

        [HttpGet]
        public async Task<IActionResult> MyUnits()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Forbid();

            var units = await _unitService.GetUnitsByCreatorAsync(userId.Value);
            return View(units);
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
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var lead = await _context.Leads
                    .Include(l => l.Project)
                    .Include(l => l.Unit)
                    .Include(l => l.AssignedTo)
                    .Include(l => l.Feedbacks)
                        .ThenInclude(f => f.Sales)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (lead == null)
                {
                    return NotFound();
                }


                ViewBag.SalesUsers = await _context.Users
                    .Where(u => u.Role == UserRole.Sales.ToString())
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id.ToString(),
                        Text = u.FullName
                    })
                    .ToListAsync();

                ViewBag.Feedbacks = await _context.LeadFeedbacks
                    .Where(f => f.LeadId == id)
                    .Include(f => f.Sales)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();

                return View(lead);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading lead details");
                TempData["ErrorMessage"] = "Error loading lead details";
                return RedirectToAction(nameof(Index));
            }
        }
        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }

        [HttpGet]
        public async Task<IActionResult> SearchClient(string term)
        {
            try
            {
                var normalizedPhone = PhoneNumberHelper.NormalizePhoneNumber(term);

                if (!string.IsNullOrEmpty(normalizedPhone))
                {
                    // Get all leads and normalize their phone numbers for comparison
                    var allLeads = await _context.Leads.ToListAsync();

                    var matchingLead = allLeads.FirstOrDefault(l =>
                        !string.IsNullOrEmpty(l.ClientPhone) &&
                        PhoneNumberHelper.NormalizePhoneNumber(l.ClientPhone) == normalizedPhone);

                    if (matchingLead != null)
                    {
                        return Json(new
                        {
                            found = true,
                            id = matchingLead.Id,
                            name = matchingLead.ClientName,
                            phone = matchingLead.ClientPhone,
                            searchType = "phone"
                        });
                    }
                }

                // Also check by name if no phone match found
                if (!string.IsNullOrWhiteSpace(term))
                {
                    var leadByName = await _context.Leads
                        .Where(l => l.ClientName.Contains(term))
                        .FirstOrDefaultAsync();

                    if (leadByName != null)
                    {
                        return Json(new
                        {
                            found = true,
                            id = leadByName.Id,
                            name = leadByName.ClientName,
                            phone = leadByName.ClientPhone,
                            searchType = "name"
                        });
                    }
                }

                return Json(new { found = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching client");
                return Json(new
                {
                    found = false,
                    error = "An error occurred while searching"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchOwner(string term)
        {
            try
            {
                var normalizedPhone = PhoneNumberHelper.NormalizePhoneNumber(term);

                if (!string.IsNullOrEmpty(normalizedPhone))
                {
                    // Get all owners and normalize their phone numbers for comparison
                    var owners = await _context.Owners.ToListAsync();

                    var matchingOwner = owners.FirstOrDefault(o =>
                        !string.IsNullOrEmpty(o.Phone) &&
                        PhoneNumberHelper.NormalizePhoneNumber(o.Phone) == normalizedPhone);

                    if (matchingOwner != null)
                    {
                        return Json(new
                        {
                            found = true,
                            id = matchingOwner.Id,
                            name = matchingOwner.Name,
                            phone = matchingOwner.Phone,
                            email = matchingOwner.Email,
                            searchType = "phone"
                        });
                    }
                }

                // Also check by name if no phone match found
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

        public static class PhoneNumberHelper
        {
            public static string NormalizePhoneNumber(string phoneNumber)
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                    return string.Empty;

                // Remove all non-digit characters
                var normalized = new string(phoneNumber.Where(char.IsDigit).ToArray());

                // Remove leading zeros and country codes (assuming Egyptian numbers)
                // For Egypt: numbers typically start with 1 after removing +20 or 0
                if (normalized.StartsWith("20")) // Egypt country code without +
                    normalized = normalized.Substring(2);

                if (normalized.StartsWith("0"))
                    normalized = normalized.Substring(1);

                return normalized;
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

        private async Task LoadViewData()
        {
            var projects = await _projectService.GetAllProjectsAsync();
            ViewBag.Projects = new SelectList(projects, "Id", "Name");

            var owners = await _context.Owners.ToListAsync();
            ViewBag.Owners = new SelectList(owners, "Id", "Name");
        }
    }
}