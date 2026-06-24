using ChequePrintingSystem.Application.Abstractions;

namespace ChequePrintingSystem.Application.Services;

public class AmountInWordsConverter : IAmountInWordsConverter
{
    private static readonly string[] Units =
    [
        "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
        "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
        "Seventeen", "Eighteen", "Nineteen"
    ];

    private static readonly string[] Tens =
    [
        "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
    ];

    public string Convert(decimal amount)
    {
        var normalizedAmount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        var whole = (long)decimal.Truncate(normalizedAmount);
        var cents = (int)((normalizedAmount - whole) * 100m);

        if (whole == 0 && cents == 0)
        {
            return "Zero Rupees and 00/100 Only";
        }

        return $"{ConvertWhole(whole)} Rupees and {cents:00}/100 Only";
    }

    private static string ConvertWhole(long number)
    {
        if (number == 0) return "Zero";
        if (number < 20) return Units[number];
        if (number < 100)
        {
            var remainder = number % 10;
            return remainder == 0 ? Tens[number / 10] : $"{Tens[number / 10]} {ConvertWhole(remainder)}";
        }

        if (number < 1000)
        {
            var remainder = number % 100;
            return remainder == 0
                ? $"{Units[number / 100]} Hundred"
                : $"{Units[number / 100]} Hundred {ConvertWhole(remainder)}";
        }

        if (number < 1_000_000)
        {
            var remainder = number % 1000;
            return remainder == 0
                ? $"{ConvertWhole(number / 1000)} Thousand"
                : $"{ConvertWhole(number / 1000)} Thousand {ConvertWhole(remainder)}";
        }

        if (number < 1_000_000_000)
        {
            var remainder = number % 1_000_000;
            return remainder == 0
                ? $"{ConvertWhole(number / 1_000_000)} Million"
                : $"{ConvertWhole(number / 1_000_000)} Million {ConvertWhole(remainder)}";
        }

        var billionRemainder = number % 1_000_000_000;
        return billionRemainder == 0
            ? $"{ConvertWhole(number / 1_000_000_000)} Billion"
            : $"{ConvertWhole(number / 1_000_000_000)} Billion {ConvertWhole(billionRemainder)}";
    }
}
