using RAMA_TMS.DTO;

namespace RAMA_TMS.Interface
{
    public interface IPdfGeneratorService
    {
        Task<byte[]> GenerateEndOfDayReportPdfAsync(EndOfDayReportDto report, DateTime date);
    }
}
