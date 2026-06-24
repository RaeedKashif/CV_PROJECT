namespace ChequePrintingSystem.Application.Abstractions;

public interface IAmountInWordsConverter
{
    string Convert(decimal amount);
}
