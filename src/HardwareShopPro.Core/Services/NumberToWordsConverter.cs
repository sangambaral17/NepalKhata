namespace HardwareShopPro.Core.Services;

/// <summary>
/// Converts a decimal amount to Indian currency words.
/// Example: 14012.50 → "Fourteen Thousand Twelve Rupees and Fifty Paise Only"
/// </summary>
public static class NumberToWordsConverter
{
    private static readonly string[] Ones =
    {
        "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
        "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
        "Seventeen", "Eighteen", "Nineteen"
    };

    private static readonly string[] Tens =
    {
        "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
    };

    public static string Convert(decimal amount)
    {
        if (amount == 0) return "Zero Rupees Only";

        var rupees = (long)Math.Truncate(amount);
        var paise = (int)Math.Round((amount - rupees) * 100);

        var result = "";
        if (rupees > 0)
            result = ConvertNumber(rupees) + " Rupees";

        if (paise > 0)
        {
            result += (rupees > 0 ? " and " : "") + ConvertNumber(paise) + " Paise";
        }

        return result + " Only";
    }

    private static string ConvertNumber(long number)
    {
        if (number == 0) return "";
        if (number < 0) return "Minus " + ConvertNumber(Math.Abs(number));

        var words = "";

        if (number / 10000000 > 0)
        {
            words += ConvertNumber(number / 10000000) + " Crore ";
            number %= 10000000;
        }

        if (number / 100000 > 0)
        {
            words += ConvertNumber(number / 100000) + " Lakh ";
            number %= 100000;
        }

        if (number / 1000 > 0)
        {
            words += ConvertNumber(number / 1000) + " Thousand ";
            number %= 1000;
        }

        if (number / 100 > 0)
        {
            words += ConvertNumber(number / 100) + " Hundred ";
            number %= 100;
        }

        if (number > 0)
        {
            if (words != "") words += "and ";

            if (number < 20)
                words += Ones[number];
            else
            {
                words += Tens[number / 10];
                if (number % 10 > 0)
                    words += " " + Ones[number % 10];
            }
        }

        return words.Trim();
    }
}
