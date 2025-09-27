using FGI.Interfaces;
using FGI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FGI.Controllers
{
    [Route("api/[controller]")]
    public class UnitsController : Controller
    {
        private readonly IUnitService _unitService;
        private readonly ILogger<UnitsController> _logger;

        public UnitsController(IUnitService unitService, ILogger<UnitsController> logger)
        {
            _unitService = unitService;
            _logger = logger;
        }

        /// <summary>
        /// Search units by various fields
        /// </summary>
        /// <param name="q">Search query</param>
        /// <param name="limit">Maximum number of results (default: 10)</param>
        /// <returns>List of matching units</returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchUnits([FromQuery] string q = "", [FromQuery] int limit = 10, [FromQuery] int projectId = 0)
        {
            try
            {
                _logger.LogInformation("Searching units with query: '{Query}', limit: {Limit}, projectId: {ProjectId}", q, limit, projectId);

                var units = await _unitService.SearchUnitsAsync(q, limit, projectId);
                _logger.LogInformation("SearchUnitsAsync returned {Count} units", units?.Count ?? 0);

                _logger.LogInformation("Processing {Count} units for response", units.Count);
                
                var items = new List<object>();
                foreach (var u in units)
                {
                    try
                    {
                        var item = new
                        {
                            id = u.Id,
                            ownerName = u.Owner?.Name ?? "N/A",
                            ownerPhone = u.Owner?.Phone ?? "N/A",
                            price = u.Price,
                            area = u.Area,
                            type = u.Type.ToString(),
                            location = u.Location ?? "N/A",
                            description = u.Description ?? "N/A",
                            bedrooms = u.Bedrooms,
                            bathrooms = u.Bathrooms,
                            currency = u.Currency.ToString()
                        };
                        items.Add(item);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing unit {UnitId}: {Error}", u.Id, ex.Message);
                    }
                }
                
                _logger.LogInformation("Successfully processed {Count} items", items.Count);

                var response = new
                {
                    items = items,
                    total = items.Count
                };

                _logger.LogInformation("Found {Count} units matching query", items.Count);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching units: {Error}", ex.Message);
                _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
                return StatusCode(500, new { 
                    error = "Search failed", 
                    details = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Test endpoint to check if API is working
        /// </summary>
        /// <returns>Simple test response</returns>
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { 
                success = true, 
                message = "Units API is working",
                timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Test database connection
        /// </summary>
        /// <returns>Database test response</returns>
        [HttpGet("testdb")]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                _logger.LogInformation("Testing database connection...");
                var units = await _unitService.SearchUnitsAsync("", 1);
                return Ok(new { 
                    success = true, 
                    message = "Database connection working",
                    totalUnits = units.Count,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database test failed: {Error}", ex.Message);
                return StatusCode(500, new { 
                    success = false, 
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Test search with empty query
        /// </summary>
        /// <returns>Test search results</returns>
        [HttpGet("testsearch")]
        public async Task<IActionResult> TestSearch()
        {
            try
            {
                _logger.LogInformation("Testing search with empty query");
                var units = await _unitService.SearchUnitsAsync("", 5);
                return Ok(new { 
                    success = true, 
                    count = units.Count,
                    message = $"Found {units.Count} units",
                    units = units.Select(u => new { 
                        id = u.Id, 
                        unitCode = u.UnitCode,
                        ownerName = u.Owner?.Name ?? "N/A"
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test search failed: {Error}", ex.Message);
                return StatusCode(500, new { 
                    success = false, 
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Test search with specific query
        /// </summary>
        /// <param name="q">Search query</param>
        /// <returns>Test search results</returns>
        [HttpGet("testsearchwithquery")]
        public async Task<IActionResult> TestSearchWithQuery([FromQuery] string q = "test")
        {
            try
            {
                _logger.LogInformation("Testing search with query: '{Query}'", q);
                var units = await _unitService.SearchUnitsAsync(q, 5);
                
                var items = new List<object>();
                foreach (var u in units)
                {
                    try
                    {
                        var item = new
                        {
                            id = u.Id,
                            ownerName = u.Owner?.Name ?? "N/A",
                            ownerPhone = u.Owner?.Phone ?? "N/A",
                            price = u.Price,
                            area = u.Area,
                            type = u.Type.ToString(),
                            location = u.Location ?? "N/A",
                            description = u.Description ?? "N/A",
                            bedrooms = u.Bedrooms,
                            bathrooms = u.Bathrooms,
                            currency = u.Currency.ToString()
                        };
                        items.Add(item);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing unit {UnitId}: {Error}", u.Id, ex.Message);
                    }
                }
                
                return Ok(new { 
                    success = true, 
                    count = items.Count,
                    message = $"Found {items.Count} units for query '{q}'",
                    items = items
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test search with query failed: {Error}", ex.Message);
                return StatusCode(500, new { 
                    success = false, 
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Get unit details partial view
        /// </summary>
        /// <param name="id">Unit ID</param>
        /// <returns>Partial view with unit details</returns>
        [HttpGet("DetailsPartial/{id}")]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            try
            {
                var unit = await _unitService.GetUnitByIdAsync(id);
                if (unit == null)
                {
                    return NotFound();
                }

                return PartialView("_UnitDetailsPartial", unit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unit details for ID {Id}: {Error}", id, ex.Message);
                return StatusCode(500, new { error = "Failed to load unit details" });
            }
        }
    }
}
