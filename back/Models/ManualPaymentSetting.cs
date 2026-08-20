namespace LeatherLane_Atelier.Models
{
    public class ManualPaymentSetting
    {
        public int Id { get; set; }
        public string MethodName { get; set; } = string.Empty; // Bank Transfer, JazzCash, Easypaisa, Raast ID
        public bool IsEnabled { get; set; } = true;
        
        // Bank Transfer specific fields
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? IBAN { get; set; }
        
        // General Account Title
        public string? AccountTitle { get; set; }
        
        // Mobile-based account fields (JazzCash/Easypaisa)
        public string? MobileNumber { get; set; }
        
        // Raast specific field
        public string? RaastId { get; set; }
    }
}
