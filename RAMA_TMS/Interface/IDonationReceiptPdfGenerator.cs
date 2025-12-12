using RAMA_TMS.Models;

namespace RAMA_TMS.Interface
{
    public interface IDonationReceiptPdfGenerator
    {
        byte[] GenerateReceipt(DonorReceiptDetail receipt, DonorMaster donor);
    }
}
