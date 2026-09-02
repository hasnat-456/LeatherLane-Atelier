using System;

namespace LeatherLane_Atelier.Services
{
    public static class OrderHelper
    {
        // 32 unambiguous characters (removed I, O, 1, 0)
        private const string Alphabet = "X7B92A4K3M8C5F6HJLNPQRSTUVWYZDGE";
        private const long Offset = 1250000; 

        public static string FormatOrderNumber(int id)
        {
            if (id <= 0) return $"LLA-{id}";
            long num = id + Offset;
            string result = "";
            while (num > 0)
            {
                result = Alphabet[(int)(num % 32)] + result;
                num /= 32;
            }
            return $"LLA-{result}";
        }

        public static int ParseOrderNumber(string formatted)
        {
            if (string.IsNullOrEmpty(formatted)) return 0;
            
            string raw = formatted.Replace("LLA-", "").ToUpper();
            long num = 0;
            foreach (char c in raw)
            {
                int index = Alphabet.IndexOf(c);
                if (index < 0) return 0;
                num = (num * 32) + index;
            }
            return (int)(num - Offset);
        }
    }
}
