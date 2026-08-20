using System.Security.Cryptography;
using System.Text;
using LeatherLane_Atelier.Models;
using Microsoft.Extensions.Options;
using System.Web;

namespace LeatherLane_Atelier.Services
{
    public interface IPayfastService
    {
        string GenerateSignature(Dictionary<string, string> data);
        bool ValidateSignature(Dictionary<string, string> data);
    }

    public class PayfastService : IPayfastService
    {
        private readonly PayfastSettings _settings;

        public PayfastService(IOptions<PayfastSettings> settings)
        {
            _settings = settings.Value;
        }

        public string GenerateSignature(Dictionary<string, string> data)
        {
            // Payfast requires keys to be in the order they are passed, but generally alphabetical works if constructed consistently.
            // Actually, Payfast requires generating string exactly in the order of the HTML form fields.
            // A common approach is to just use the ordered keys if we control the input.
            var stringBuilder = new StringBuilder();
            
            foreach (var kvp in data)
            {
                if (string.IsNullOrEmpty(kvp.Value) || kvp.Key == "signature")
                    continue;

                stringBuilder.Append($"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value.Trim())}&");
            }

            // Remove the trailing &
            string pfOutput = stringBuilder.ToString().TrimEnd('&');

            if (!string.IsNullOrEmpty(_settings.PassPhrase))
            {
                pfOutput += $"&passphrase={HttpUtility.UrlEncode(_settings.PassPhrase.Trim())}";
            }

            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(pfOutput);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                return Convert.ToHexString(hashBytes).ToLower();
            }
        }
        
        public bool ValidateSignature(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("signature", out string? providedSignature))
                return false;

            var generatedSignature = GenerateSignature(data);

            return string.Equals(providedSignature, generatedSignature, StringComparison.OrdinalIgnoreCase);
        }
    }
}
