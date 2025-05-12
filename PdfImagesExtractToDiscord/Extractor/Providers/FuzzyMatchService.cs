using FuzzySharp;

namespace PdfImagesExtractToDiscord.Extractor.Providers;

public class FuzzyMatchService : IFuzzyMatchService
{
	public string GetCurrencyPair(string[] words)
	{
		List<string> currencies = new List<string>();
        
		for (var i = 0; i < words.Length - 1; i++)
		{
			string wordPair;
			if (i + 1 < words.Length)
				wordPair = $"{words[i]} {words[i + 1]}";
			else
				wordPair = words[i];
                
			var currencyCode = GetCurrencyCodeWithFuzzy(wordPair);
			if (!string.IsNullOrEmpty(currencyCode))
			{
				currencies.Add(currencyCode);
				i++;
			}
		}

		if (currencies.Count >= 2)
		{
			return $"{currencies[0]}/{currencies[1]}";
		}

		return null;
	}

	private static string GetCurrencyCodeWithFuzzy(string currencyName)
	{
		var currencyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "U.S. Dollar", "USD" },
			{ "Japanese Yen", "JPY" },
			{ "Swiss Franc", "CHF" },
			{ "Canadian Dollar", "CAD" },
			{ "British Pound", "GBP" },
			{ "Euro", "EUR" },
			{ "Australian Dollar", "AUD" },
			{ "Gold spot", "XAU" }
		};

		string bestMatch = null;
		var bestScore = 0;

		foreach (var kvp in currencyMap)
		{
			var score = Fuzz.Ratio(currencyName, kvp.Key);
			if (score > bestScore)
			{
				bestScore = score;
				bestMatch = kvp.Value;
			}
		}

		return bestScore > 70 ? bestMatch : string.Empty; // Adjust threshold as needed
	}
}