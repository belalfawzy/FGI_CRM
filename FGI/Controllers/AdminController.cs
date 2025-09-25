using FGI.Enums;
using FGI.Interfaces;
using FGI.Models;
using FGI.Services;
using FGI.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using static FGI.Controllers.LeadController;

namespace FGI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly IUnitService _unitService;
        private readonly IUserService _userService;
        private readonly ILeadService _leadService;
        private readonly ILeadFeedbackService _feedbackService;
        private readonly IHelperService _helperService;
        private readonly AppDbContext _context;
        private readonly ILogger<LeadController> _logger;

        public AdminController(
            IProjectService projectService,
            IUnitService unitService,
            IUserService userService,
            ILeadService leadService,
            AppDbContext context,
            ILogger<LeadController> logger,
            ILeadFeedbackService feedbackService,
            IHelperService helperService)
        {
            _projectService = projectService;
            _unitService = unitService;
            _userService = userService;
            _leadService = leadService;
            _context = context;
            _logger = logger;
            _feedbackService = feedbackService;
            _helperService = helperService;
        }

        [HttpGet("Admin/GetUnits")]
        public async Task<IActionResult> GetUnits(int projectId = 0, string term = "")
        {
            try
            {
                var results = await _unitService.GetUnitsForSelectAsync(projectId, term);
                return Json(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching units");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Projects()
        {
            var projects = await _projectService.GetAllProjectsAsync();
            return View(projects);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(Project project)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Project name is required";
                    return RedirectToAction("Projects");
                }

                await _projectService.AddProjectAsync(project);
                TempData["SuccessMessage"] = "Project created successfully!";
                return RedirectToAction("Projects");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project");
                TempData["ErrorMessage"] = $"Error creating project: {ex.Message}";
                return RedirectToAction("Projects");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProjectWithUnit(Project project, List<Unit> units)
        {
            try
            {
                if (!ModelState.IsValid || units == null || !units.Any())
                {
                    TempData["ErrorMessage"] = "Project must have at least one unit";
                    return RedirectToAction("Projects");
                }

                await _projectService.AddProjectAsync(project);

                foreach (var unit in units)
                {
                    unit.ProjectId = project.Id;
                    await _unitService.AddUnitAsync(unit);
                }

                TempData["SuccessMessage"] = "Project created successfully with units!";
                return RedirectToAction("Projects");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating project: {ex.Message}";
                return RedirectToAction("Projects");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ProjectDetails(int id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            var units = await _unitService.GetUnitsByProjectIdAsync(id);
            ViewBag.Units = units;

            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(int id)
        {
            try
            {
                var hasLeads = await _context.Leads.AnyAsync(l => l.ProjectId == id);
                if (hasLeads)
                {
                    TempData["ErrorMessage"] = "Cannot delete project with associated leads";
                    return RedirectToAction("Projects");
                }

                await _projectService.DeleteProjectAsync(id);
                TempData["SuccessMessage"] = "Project deleted successfully!";
                return RedirectToAction("Projects");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting project: {ex.Message}";
                return RedirectToAction("Projects");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUnit(Unit unit)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Invalid unit data. Please check all required fields.";
                    return RedirectToAction("Projects");
                }

                await _unitService.AddUnitAsync(unit);
                TempData["SuccessMessage"] = "Unit created successfully!";
                return RedirectToAction("Projects");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating unit");
                TempData["ErrorMessage"] = $"Error creating unit: {ex.Message}";
                return RedirectToAction("Projects");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await _userService.GetAllUsersAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(User user)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "البيانات المدخلة غير صالحة. يرجى التحقق من الحقول المطلوبة.";
                    return RedirectToAction("Users");
                }

                var result = await _userService.RegisterAsync(user);

                if (Convert.ToBoolean(result))
                {
                    TempData["SuccessMessage"] = "تم إضافة المستخدم بنجاح!";
                }
                else
                {
                    TempData["ErrorMessage"] = "حدث خطأ أثناء إضافة المستخدم. قد يكون البريد الإلكتروني مستخدماً مسبقاً.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"حدث خطأ: {ex.Message}";
            }

            return RedirectToAction("Users");
        }

        [HttpGet]
        public async Task<IActionResult> Leads(
            string searchTerm = null,
            int? projectId = null,
            int? assignedToId = null,
            LeadStatusType? status = null,
            DateTime? filterDate = null,
            bool unassignedOnly = false)
        {
            var leads = await _leadService.GetAllLeadsAsync();
            var salesUsers = await _userService.GetUsersByRoleAsync(UserRole.Sales);
            var projects = await _projectService.GetAllProjectsAsync();

            var assignmentHistories = new Dictionary<int, List<LeadAssignmentHistory>>();
            var leadFeedbacks = new Dictionary<int, List<LeadFeedback>>();

            foreach (var lead in leads)
            {
                var histories = await _context.LeadAssignmentHistories
                    .Include(h => h.FromSales)
                    .Include(h => h.ToSales)
                    .Include(h => h.ChangedBy)
                    .Where(h => h.LeadId == lead.Id)
                    .OrderByDescending(h => h.ChangedAt)
                    .ToListAsync();

                assignmentHistories.Add(lead.Id, histories);

                var feedbacks = await _context.LeadFeedbacks
                    .Include(f => f.Sales)
                    .Where(f => f.LeadId == lead.Id)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();

                leadFeedbacks.Add(lead.Id, feedbacks);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                leads = leads.Where(l =>
                    l.ClientName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    l.ClientPhone.Contains(searchTerm)).ToList();
            }

            if (projectId.HasValue)
            {
                leads = leads.Where(l => l.ProjectId == projectId.Value).ToList();
            }

            if (assignedToId.HasValue)
            {
                leads = leads.Where(l => l.AssignedToId == assignedToId.Value).ToList();
            }

            if (status.HasValue)
            {
                leads = leads.Where(l => l.CurrentStatus == status.Value).ToList();
            }

            if (filterDate.HasValue)
            {
                leads = leads.Where(l => l.CreatedAt.Date == filterDate.Value.Date).ToList();
            }

            if (unassignedOnly)
            {
                leads = leads.Where(l => l.AssignedToId == null).ToList();
            }

            var viewModel = new AdminLeadsViewModel
            {
                Leads = leads,
                SalesUsers = salesUsers,
                Projects = projects,
                SearchTerm = searchTerm,
                SelectedProjectId = projectId,
                SelectedSalesUserId = assignedToId,
                SelectedStatus = status,
                FilterDate = filterDate,
                UnassignedOnly = unassignedOnly,
                AssignmentHistories = assignmentHistories,
                LeadFeedbacks = leadFeedbacks
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DistributeLeads(string distributionMethod)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                await _leadService.DistributeLeadsAsync(distributionMethod, currentUserId);

                var unassignedCount = await _context.Leads.CountAsync(l => l.AssignedToId == null);
                var salesCount = await _context.Users.CountAsync(u => u.Role == UserRole.Sales.ToString());

                TempData["SuccessMessage"] = $"Successfully distributed leads among {salesCount} sales reps";
                return RedirectToAction("Tasks");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error distributing leads: {ex.Message}";
                return RedirectToAction("Tasks");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignLead(int leadId, int salesUserId)
        {
            try
            {
                var changedById = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                await _leadService.AssignLeadAsync(leadId, salesUserId, changedById);

                TempData["SuccessMessage"] = "Lead assigned successfully!";
                return RedirectToAction("Leads");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while assigning the lead.";
                return RedirectToAction("Leads");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReassignLead(int leadId, int newSalesUserId)
        {
            try
            {
                var changedById = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                await _leadService.ReassignLeadAsync(leadId, newSalesUserId, changedById);

                TempData["SuccessMessage"] = $"Lead reassigned to new sales rep (ID: {newSalesUserId})";
                return RedirectToAction("Leads");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while reassigning the lead.";
                return RedirectToAction("Leads");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnassignLead(int leadId)
        {
            try
            {
                var changedById = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                await _leadService.ReassignLeadAsync(leadId, 0, changedById);
                TempData["SuccessMessage"] = "Lead unassigned successfully!";
                return RedirectToAction("Leads");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while unassigning the lead.";
                return RedirectToAction("Leads");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLeadStatus(int leadId, LeadStatusType status)
        {
            await _leadService.UpdateStatusAsync(leadId, status);
            return RedirectToAction("Leads");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFollowUp(int leadId, string followUpNotes, LeadStatusType status)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(followUpNotes))
                {
                    TempData["ErrorMessage"] = "Follow-up notes cannot be empty.";
                    return RedirectToAction("Leads");
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var feedback = new LeadFeedback
                {
                    LeadId = leadId,
                    SalesId = userId,
                    Comment = followUpNotes.Trim(),
                    CreatedAt = DateTime.Now,
                    Status = status
                };

                await _context.LeadFeedbacks.AddAsync(feedback);

                var lead = await _leadService.GetLeadByIdAsync(leadId);
                if (lead != null)
                {
                    lead.CurrentStatus = status;
                    await _leadService.UpdateLeadAsync(lead);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Follow-up added successfully!";
                return RedirectToAction("Leads");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while adding the follow-up.";
                return RedirectToAction("Leads");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Tasks()
        {
            var unassignedLeads = await _context.Leads
                .Include(l => l.Project)
                .Where(l => l.AssignedToId == null)
                .OrderBy(l => l.CreatedAt)
                .ToListAsync();

            var salesUsers = await _userService.GetUsersByRoleAsync(UserRole.Sales);
            ViewBag.SalesUsers = salesUsers;

            return View(unassignedLeads);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignLeadFromTasks(int leadId, int salesUserId)
        {
            var changedById = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _leadService.AssignLeadAsync(leadId, salesUserId, changedById);
            return RedirectToAction("Tasks");
        }


        [HttpGet]
        public async Task<IActionResult> AddUnit()
        {
            // Clear any existing TempData messages to prevent them from showing on this page
            TempData.Remove("SuccessMessage");
            TempData.Remove("ErrorMessage");
            
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
                return RedirectToAction(nameof(AddUnit));
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
        public async Task<IActionResult> SearchClient(string term)
        {
            try
            {
                var result = await _leadService.SearchClientAsync(term);
                return Json(result);
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

        [HttpGet]
        public async Task<IActionResult> EditUnit(int id)
        {
            try
            {
                var unit = await _unitService.GetUnitByIdAsync(id);
                if (unit == null)
                {
                    TempData["ErrorMessage"] = "Unit not found";
                    return RedirectToAction(nameof(AllUnits));
                }

                await LoadViewData();
                return View(unit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading unit for editing");
                TempData["ErrorMessage"] = "Error loading unit for editing";
                return RedirectToAction(nameof(AllUnits));
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
                    return RedirectToAction(nameof(AllUnits));
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
                return RedirectToAction(nameof(AllUnits));
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

        [HttpGet]
        public async Task<IActionResult> AllUnits(
            UnitType? type = null,
            int? projectId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? bedrooms = null,
            bool? isAvailable = null,
            decimal? minArea = null,
            int? bathrooms = null,
            UnitSaleType? saleType = null,
            string searchTerm = null)
        {
            try
            {
                var units = await _unitService.GetFilteredUnitsAsync(type, projectId, minPrice, maxPrice, bedrooms, isAvailable, minArea, bathrooms, saleType, searchTerm);

                var projects = await _projectService.GetAllProjectsAsync();
                ViewBag.Projects = new SelectList(projects, "Id", "Name");

                return View(units);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading all units");
                TempData["ErrorMessage"] = "Error loading units data";
                return View(new List<Unit>());
            }
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

                // Unit already includes Owner, Project, and CreatedBy from the service

                return PartialView("_UnitDetailsPartial", unit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading unit details");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUnitStatus(int id)
        {
            try
            {
                var unit = await _unitService.GetUnitByIdAsync(id);
                if (unit == null)
                {
                    return Json(new { success = false, message = "Unit not found" });
                }

                unit.IsAvailable = !unit.IsAvailable;
                await _unitService.UpdateUnitAsync(unit);

                return Json(new
                {
                    success = true,
                    message = $"Unit status updated to {(unit.IsAvailable ? "Available" : "Not Available")}",
                    isAvailable = unit.IsAvailable
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling unit status");
                return Json(new { success = false, message = "Error updating unit status" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUnit(int id)
        {
            try
            {
                var unit = await _unitService.GetUnitByIdAsync(id);
                if (unit == null)
                {
                    return Json(new { success = false, message = "Unit not found" });
                }

                await _unitService.DeleteUnitAsync(id);
                return Json(new { success = true, message = $"Unit '{unit.UnitCode}' deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting unit");
                return Json(new { success = false, message = "Error deleting unit" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyLeads()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var leads = await _context.Leads
                .Where(l => l.CreatedById == userId)
                .Include(l => l.Project)
                .Include(l => l.Unit)
                .Include(l => l.AssignedTo)
                .ToListAsync();

            foreach (var lead in leads)
            {
                if (lead.Project == null)
                    lead.Project = new Project { Name = "N/A" };

                if (lead.Unit == null)
                    lead.Unit = new Unit { UnitCode = "N/A" };
            }

            return View(leads);
        }

        [HttpGet]
        public async Task<IActionResult> AddLead()
        {
            var projects = await _projectService.GetAllProjectsAsync();
            ViewBag.Projects = new SelectList(projects, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLead(Lead lead)
        {
            try
            {
                // Normalize phone number for checking
                var normalizedPhone = _helperService.NormalizePhoneNumber(lead.ClientPhone);

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
                        _helperService.NormalizePhoneNumber(l.ClientPhone) == normalizedPhone);

                    if (duplicateLead != null)
                    {
                        duplicateWarning = $"Note: This phone number is already associated with lead ID: {duplicateLead.Id}";
                    }
                }

                // Set creation details
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                lead.CreatedById = userId;
                lead.CreatedAt = DateTime.Now;
                lead.CurrentStatus = LeadStatusType.New;
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
                        redirect = Url.Action("AllUnits", new { id = lead.Id })
                    });
                }

                TempData["Success"] = "Lead saved successfully!";
                if (!string.IsNullOrEmpty(duplicateWarning))
                {
                    TempData["Warning"] = duplicateWarning;
                }
                return RedirectToAction("AllUnits", new { id = lead.Id });
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

        [HttpGet]
        public async Task<IActionResult> LeadDetails(int id)
        {
            var lead = await _leadService.GetLeadByIdAsync(id);
            if (lead == null)
            {
                return NotFound();
            }

            lead.Project = await _projectService.GetProjectByIdAsync(lead.ProjectId ?? 0);
            lead.Unit = await _unitService.GetUnitByIdAsync(lead.UnitId ?? 0);
            lead.AssignedTo = await _userService.GetByIdAsync(lead.AssignedToId ?? 0);
            lead.CreatedBy = await _userService.GetByIdAsync(lead.CreatedById);

            ViewBag.Feedbacks = await _feedbackService.GetFeedbacksByLeadAsync(id);
            ViewBag.AssignmentHistories = await _context.LeadAssignmentHistories
                .Include(h => h.FromSales)
                .Include(h => h.ToSales)
                .Include(h => h.ChangedBy)
                .Where(h => h.LeadId == id)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();

            return View(lead);
        }

        private async Task LoadProjects()
        {
            var projects = await _projectService.GetAllProjectsAsync();
            ViewBag.Projects = projects.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Name
            }).ToList();

            var owners = await _context.Owners.ToListAsync();
            ViewBag.Owners = new SelectList(owners, "Id", "Name");
        }

        private int? GetCurrentUserId()
        {
            return _helperService.GetCurrentUserId();
        }
        [HttpGet]
        public async Task<IActionResult> ExportLeadsToCsv()
        {
            try
            {
                var csvBytes = await _leadService.ExportLeadsToCsvAsync();
                return File(csvBytes, "text/csv", $"Leads_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting leads to CSV");
                TempData["ErrorMessage"] = "Error exporting leads to CSV";
                return RedirectToAction("Leads");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportUnitsToCsv()
        {
            try
            {
                var units = await _unitService.GetAllUnitsAsync();
                var csvBytes = await _unitService.ExportUnitsToCsvAsync(units);
                return File(csvBytes, "text/csv", $"Units_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting units to CSV");
                TempData["ErrorMessage"] = "Error exporting units to CSV";
                return RedirectToAction("AllUnits");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditLead(int id)
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
        public async Task<IActionResult> EditLead(int id, [Bind("Id,ClientName,ClientPhone,Comment,ProjectId,UnitId,CreatedById,CreatedAt,AssignedToId,CurrentStatus")] Lead lead)
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
        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}