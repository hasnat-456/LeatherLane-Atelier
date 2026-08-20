namespace LeatherLane_Atelier.Models
{
    public class PayfastSettings
    {
        public string MerchantId { get; set; } = string.Empty;
        public string MerchantKey { get; set; } = string.Empty;
        public string PassPhrase { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public string NotifyUrl { get; set; } = string.Empty;
        public string ProcessUrl { get; set; } = string.Empty;
    }
}
