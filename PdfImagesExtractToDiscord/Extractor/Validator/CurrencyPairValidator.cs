namespace PdfImagesExtractToDiscord.Extractor.Validator;

public class CurrencyPairValidator : ICurrencyPairValidator
{
	private readonly IEnumerable<string> _possiblePairs;
	private readonly HashSet<string> _validBaseCurrencies;
	private readonly HashSet<string> _validQuoteCurrencies;

	public CurrencyPairValidator(IEnumerable<string> possiblePairs)
	{
		_possiblePairs = possiblePairs ?? [];
		// Extract base currencies (first part of each pair)
		_validBaseCurrencies = new HashSet<string>(
			_possiblePairs
				.Select(pair => pair.Split('/')[0])
				.Distinct()
		);
        
		// Extract quote currencies (second part of each pair)
		_validQuoteCurrencies = new HashSet<string>(
			_possiblePairs
				.Select(pair => pair.Split('/')[1])
				.Distinct()
		);
	}

	public bool IsValidCurrencyPair(string currencyPair)
	{
		if (string.IsNullOrEmpty(currencyPair))
		{
			return false;
		}

		// If we don't have a validation list, any format-valid pair is acceptable
		if (!_possiblePairs.Any())
		{
			return true;
		}

		return _possiblePairs.Contains(currencyPair);
	}

	public bool IsValidCurrencyPairOptional(string currencyPair)
	{
		// Basic validation
		if (string.IsNullOrEmpty(currencyPair))
		{
			return false;
		}

		// If we don't have a validation list, any format-valid pair is acceptable
		if (!_possiblePairs.Any())
		{
			return true;
		}

		// Exact match against known pairs
		if (_possiblePairs.Contains(currencyPair))
		{
			return true;
		}

		// Check if it's a partial match with "Unknown"
		var parts = currencyPair.Split('/');
		if (parts.Length != 2)
		{
			return false; // Must have exactly two parts separated by '/'
		}

		string baseCurrency = parts[0];
		string quoteCurrency = parts[1];

		// "Unknown" can substitute for a base currency if the quote currency is valid in that position
		if (baseCurrency == "Unknown" && _validQuoteCurrencies.Contains(quoteCurrency))
		{
			return true;
		}

		// "Unknown" can substitute for a quote currency if the base currency is valid in that position
		if (quoteCurrency == "Unknown" && _validBaseCurrencies.Contains(baseCurrency))
		{
			return true;
		}

		return false;
	}
}