namespace PdfImagesExtractToDiscord.Extractor;

	public class ExtractionResult
	{
		public bool IsSuccessful { get; }
		public string CurrencyPair { get; }

		private ExtractionResult(bool isSuccessful, string currencyPair = null)
		{
			IsSuccessful = isSuccessful;
			CurrencyPair = currencyPair;
		}

		public static ExtractionResult Success(string currencyPair) => 
			new ExtractionResult(true, currencyPair);

		public static ExtractionResult Failure() => 
			new ExtractionResult(false);
	}
