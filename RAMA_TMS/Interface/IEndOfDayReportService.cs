using RAMA_TMS.DTO;
namespace RAMA_TMS.Interface
{
    public interface IEndOfDayReportService
    {
        Task<EndOfDayReportDto> GetReportAsync(DateTime date, int userId, string userRole);
        Task<List<string>> GetAdminEmailsAsync();
    }
}
