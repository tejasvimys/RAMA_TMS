namespace RAMA_TMS.Models
{
    public class SmtpEmailSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = false;
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromEmail { get; set; } = "";
        public string FromName { get; set; } = "Ananthaadi Rayara Matha (RAMA), Atlanta, GA.";
        public string Server { get; set; } = string.Empty;
    }
}
