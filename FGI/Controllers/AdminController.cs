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
        private readonly AppDbContext _context;
        private readonly ILogger<LeadController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminController(
            IProjectService projectService,
            IUnitService unitService,
            IUserService userService,
            ILeadService leadService,
            AppDbContext context,
            ILogger<LeadController> logger,
            ILeadFeedbackService feedbackService,
            IHttpContextAccessor httpContextAccessor)
        {
            _projectService = projectService;
            _unitService = unitService;
            _userService = userService;
            _leadService = leadService;
            _context = context;
            _logger = logger;
            _feedbackService = feedbackService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> GetUnits(int projectId = 0, string term = "")
        {
            try
            {
                _logger.LogInformation($"Searching units - ProjectId: {projectId}, Term: {term}");

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

                units = units.Where(u => u.IsAvailable).ToList();

                var results = units.OrderBy(u => u.UnitCode).Select(u => new
                {
                    id = u.Id,
                    text = $"{(string.IsNullOrWhiteSpace(u.UnitCode) ? "NA" : u.UnitCode)} - {(string.IsNullOrWhiteSpace(u.Location) ? "NA" : u.Location)}",
                    disabled = !u.IsAvailable,
                    unit = new
                    {
                        UnitCode = string.IsNullOrWhiteSpace(u.UnitCode) ? "NA" : u.UnitCode,
                        UnitType = u.Type,
                        Price = u.Price,
                        Area = u.Area
                    }
                }).ToList();

                _logger.LogInformation($"Found {results.Count} units matching search");

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
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Projects");
            }

            await _unitService.AddUnitAsync(unit);
            return RedirectToAction("Projects");
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
                var unassignedLeads = await _context.Leads
                    .Where(l => l.AssignedToId == null)
                    .OrderBy(l => l.CreatedAt)
                    .ToListAsync();

                var salesUsers = await _userService.GetUsersByRoleAsync(UserRole.Sales);
                var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                if (!unassignedLeads.Any())
                {
                    TempData["ErrorMessage"] = "No unassigned leads to distribute";
                    return RedirectToAction("Tasks");
                }

                if (!salesUsers.Any())
                {
                    TempData["ErrorMessage"] = "No sales reps available";
                    return RedirectToAction("Tasks");
                }

                int leadsPerSales = unassignedLeads.Count / salesUsers.Count;
                int remainingLeads = unassignedLeads.Count % salesUsers.Count;

                int leadIndex = 0;
                foreach (var salesUser in salesUsers)
                {
                    int leadsToAssign = leadsPerSales;

                    if (remainingLeads > 0)
                    {
                        leadsToAssign++;
                        remainingLeads--;
                    }

                    for (int i = 0; i < leadsToAssign && leadIndex < unassignedLeads.Count; i++)
                    {
                        var lead = unassignedLeads[leadIndex];
                        lead.AssignedToId = salesUser.Id;

                        var history = new LeadAssignmentHistory
                        {
                            LeadId = lead.Id,
                            FromSalesId = null,
                            ToSalesId = salesUser.Id,
                            ChangedById = currentUserId,
                            ChangedAt = DateTime.Now
                        };

                        _context.LeadAssignmentHistories.Add(history);
                        leadIndex++;
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Successfully distributed {unassignedLeads.Count} leads among {salesUsers.Count} sales reps";
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
                return RedirectToAction(nameof(AllUnits));
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
            bool? isAvailable = null)
        {
            try
            {
                var query = _context.Units
                    .Include(u => u.Project)
                    .Include(u => u.CreatedBy)
                    .AsQueryable();

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

                if (isAvailable.HasValue)
                    query = query.Where(u => u.IsAvailable == isAvailable.Value);

                var projects = await _projectService.GetAllProjectsAsync();
                ViewBag.Projects = new SelectList(projects, "Id", "Name");

                var units = await query
                    .OrderBy(u => u.Project.Name)
                    .ThenBy(u => u.UnitCode)
                    .ToListAsync();

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

                unit.Project = await _projectService.GetProjectByIdAsync(unit.ProjectId ?? 0);
                unit.CreatedBy = await _userService.GetByIdAsync(unit.CreatedById ?? 0);

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
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new { success = false, message = "Validation failed", errors });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                await _leadService.CreateLeadAsync(lead, userId);

                return Json(new
                {
                    success = true,
                    message = "Lead saved successfully!",
                    redirect = Url.Action("MyLeads")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lead");
                return Json(new
                {
                    success = false,
                    message = $"Error saving lead: {ex.Message}"
                });
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
        }

        private int? GetCurrentUserId()
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userId, out int id) ? id : null;
        }
    }
}