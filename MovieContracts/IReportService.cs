using MovieCore.DTOs;

namespace MovieContracts;

public interface IReportService
{
    Task<DashboardDto> GetDashboardAsync();
}
