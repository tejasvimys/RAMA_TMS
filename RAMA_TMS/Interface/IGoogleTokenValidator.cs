namespace RAMA_TMS.Interface
{
    public interface IGoogleTokenValidator
    {
        Task<string?> ValidateAndGetEmailAsync(string idToken);
    }
}
