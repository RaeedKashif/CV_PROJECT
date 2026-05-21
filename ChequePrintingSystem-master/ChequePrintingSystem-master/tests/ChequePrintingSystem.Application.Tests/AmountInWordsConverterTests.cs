using ChequePrintingSystem.Application.Services;

namespace ChequePrintingSystem.Application.Tests;

public class AmountInWordsConverterTests
{
    [Fact]
    public void Convert_ShouldReturnFormattedWords()
    {
        var converter = new AmountInWordsConverter();
        var result = converter.Convert(125.75m);
        Assert.Contains("One Hundred Twenty Five", result);
        Assert.EndsWith("75/100 Only", result);
    }
}
