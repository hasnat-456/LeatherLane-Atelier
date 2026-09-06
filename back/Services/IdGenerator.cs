using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LeatherLane_Atelier.Services
{
    public static class IdGenerator
    {
        // 32 unambiguous characters (excludes 0, O, 1, I to prevent user confusion and typos)
        private const string Charset = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

        /// <summary>
        /// Generates a secure, random alphanumeric code of specified length.
        /// </summary>
        public static string GenerateRandomCode(int length = 7)
        {
            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);
            var result = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                result.Append(Charset[bytes[i] % Charset.Length]);
            }
            return result.ToString();
        }

        /// <summary>
        /// Generates an Order ID in the format: LLA-[DDMM]-[6/7 ALPHANUMERIC]
        /// e.g. LLA-0209-X7B92K1
        /// </summary>
        public static string GenerateOrderId(DateTime? date = null)
        {
            var d = date ?? DateTime.UtcNow;
            string ddmm = d.ToString("ddMM");
            string code = GenerateRandomCode(7);
            return $"LLA-{ddmm}-{code}";
        }

        /// <summary>
        /// Derives a clean 2-3 character uppercase abbreviation for a product category.
        /// Supports standard categories (Chappal -> CH, Peshawari Chappal -> PC, Shoes -> SH, Sandals -> SA)
        /// and dynamically generates abbreviations for any new/future categories.
        /// </summary>
        public static string GetCategoryAbbreviation(string? category)
        {
            if (string.IsNullOrWhiteSpace(category)) return "PR";

            string trimmed = category.Trim();

            // Explicit mappings for known categories
            var lower = trimmed.ToLowerInvariant();
            if (lower == "peshawari chappal" || lower == "peshawari") return "PC";
            if (lower == "chappal" || lower == "chappals") return "CH";
            if (lower == "shoes" || lower == "shoe") return "SH";
            if (lower == "sandals" || lower == "sandal") return "SA";
            if (lower == "boots" || lower == "boot") return "BT";
            if (lower == "slippers" || lower == "slipper") return "SL";
            if (lower == "accessories" || lower == "accessory") return "AC";
            if (lower == "wallets" || lower == "wallet") return "WL";
            if (lower == "bags" || lower == "bag") return "BG";
            if (lower == "belts" || lower == "belt") return "BL";

            // Multi-word category: Take initial letters (e.g. "Formal Shoes" -> "FS", "Casual Loafers" -> "CL")
            var words = trimmed.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2)
            {
                string initials = "";
                for (int i = 0; i < Math.Min(words.Length, 3); i++)
                {
                    if (words[i].Length > 0 && char.IsLetterOrDigit(words[i][0]))
                    {
                        initials += char.ToUpperInvariant(words[i][0]);
                    }
                }
                if (initials.Length >= 2) return initials;
            }

            // Single word fallback: remove non-alphanumeric, take first 2-3 consonants or characters
            var clean = Regex.Replace(trimmed.ToUpperInvariant(), "[^A-Z0-9]", "");
            if (clean.Length <= 3) return clean;

            // Extract consonants after first letter
            var consonants = new StringBuilder();
            consonants.Append(clean[0]);
            for (int i = 1; i < clean.Length && consonants.Length < 3; i++)
            {
                char c = clean[i];
                if (!"AEIOU".Contains(c))
                {
                    consonants.Append(c);
                }
            }

            if (consonants.Length >= 2) return consonants.ToString();
            return clean.Substring(0, Math.Min(3, clean.Length));
        }

        /// <summary>
        /// Generates a Product ID in the format: LLA-[TYPE]-[6/7 ALPHANUMERIC]
        /// e.g. LLA-SH-X7B92K1
        /// </summary>
        public static string GenerateProductId(string? category)
        {
            string abbr = GetCategoryAbbreviation(category);
            string code = GenerateRandomCode(7);
            return $"LLA-{abbr}-{code}";
        }

        /// <summary>
        /// Generates an Exchange ID in the format: EXC-[6/7 ALPHANUMERIC]
        /// e.g. EXC-M82K4P7
        /// </summary>
        public static string GenerateExchangeId()
        {
            string code = GenerateRandomCode(7);
            return $"EXC-{code}";
        }

        /// <summary>
        /// Generates a Return ID in the format: RET-[6/7 ALPHANUMERIC]
        /// e.g. RET-Q7D91KX
        /// </summary>
        public static string GenerateReturnId()
        {
            string code = GenerateRandomCode(7);
            return $"RET-{code}";
        }

        /// <summary>
        /// Legacy compatibility fallback for OrderHelper
        /// </summary>
        public static string FormatLegacyOrderNumber(int id)
        {
            if (id <= 0) return "LLA-0000";
            return $"LLA-{id:D5}";
        }

        /// <summary>
        /// Resolves an Order ID into proper branded format (e.g. LLA-0209-X7B92K1 or LLA-00003).
        /// Guarantees that raw numeric database IDs (e.g. "1", "2", "3") are never displayed directly.
        /// </summary>
        public static string ResolveOrderDisplay(string? orderId, int fallbackId = 0)
        {
            if (!string.IsNullOrWhiteSpace(orderId))
            {
                var trimmed = orderId.Trim();
                if (int.TryParse(trimmed.TrimStart('#'), out int parsedNum))
                {
                    return FormatLegacyOrderNumber(parsedNum > 0 ? parsedNum : fallbackId);
                }
                return trimmed;
            }
            return FormatLegacyOrderNumber(fallbackId);
        }
    }
}
