namespace PdfImagesExtractToDiscord.Extractor.Providers;

public interface IMlPredictionService
{
	string GetCurrencyPair(string[] words);
}