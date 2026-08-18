using Microsoft.AspNetCore.Mvc;
using MovieContracts;
using MovieCore.DTOs;

namespace MoviePresentation.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(IServiceManager services) : ControllerBase
{
    // GET /api/reports/dashboard
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> GetDashboard() =>
        Ok(await services.ReportService.GetDashboardAsync());
}
