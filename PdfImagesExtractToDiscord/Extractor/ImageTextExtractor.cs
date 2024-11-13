using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using FuzzySharp;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using PdfImagesExtractToDiscord.Interface;
using PdfImagesExtractToDiscord.Model;
using Tesseract;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace PdfImagesExtractToDiscord.Extractor;

public class ImageTextExtractor : IImageTextExtractor
{
	private MLContext _mlContext;
	private TransformerChain<KeyToValueMappingTransformer>? _mlModel;

	public ImageTextExtractor()
	{
		InitMlModel();
	}

	public ImageDetailsModel GetImageDetails(FileStream fileStream)
	{
		using (var image = Image.FromStream(fileStream))
		using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
		{
			// Create a bitmap of the top-left corner (150px height, 1000px width)
			using (var bitmap = new Bitmap(1000, 150))
			{
				using (var graphics = Graphics.FromImage(bitmap))
				{
					graphics.DrawImage(image, new Rectangle(0, 0, 1000, 150), new Rectangle(0, 0, 1000, 150),
						GraphicsUnit.Pixel);
				}

				// Convert Bitmap to Pix
				using (var ms = new MemoryStream())
				{
					bitmap.Save(ms, ImageFormat.Png);
					ms.Position = 0;
					using (var pix = Pix.LoadFromMemory(ms.ToArray()))
					{
						using (var page = engine.Process(pix))
						{
							var text = page.GetText().Trim();
							Console.WriteLine($"OCR Text: {text}"); // For debugging

							var nonEmptyTrimmedLongLines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
								.Select(line => line?.Trim())
								.Where(line => !string.IsNullOrWhiteSpace(line) && line.Length > 10).ToList();
							if (nonEmptyTrimmedLongLines.Count < 2)
								return new ImageDetailsModel
									{ SymbolName = "Unknown", Interval = "Unknown", DateString = "Unknown" };

							// Extract the date from the first line
							var dateString = ExtractDateFromText(nonEmptyTrimmedLongLines.FirstOrDefault());
							var currencyPairLine = nonEmptyTrimmedLongLines.LastOrDefault();

							// Extract currency pair using ML.NET / Fuzzy
							Console.WriteLine($"Currency pair line: {currencyPairLine}");
							var symbolName = ExtractCurrencyPair(currencyPairLine);
							Console.WriteLine($"Extracted symbol name: {symbolName}");

							// Extract interval
							var intervalMatch = Regex.Match(currencyPairLine, @"(\d+[HDWM])", RegexOptions.IgnoreCase);
							var interval = intervalMatch.Success ? intervalMatch.Groups[1].Value : "Unknown";

							Console.WriteLine($"{currencyPairLine} - {symbolName}: {interval}");

							return new ImageDetailsModel
							{
								SymbolName = symbolName,
								Interval = interval,
								DateString = dateString
							};
						}
					}
				}
			}
		}
	}

	private void InitMlModel()
	{
		_mlContext = new MLContext();

		var data = new List<CurrencyData>
		{
			new() { Text = "U.S. Dollar", CurrencyCode = "USD" },
			new() { Text = "Japanese Yen", CurrencyCode = "JPY" },
			new() { Text = "Swiss Franc", CurrencyCode = "CHF" },
			new() { Text = "Canadian Dollar", CurrencyCode = "CAD" },
			new() { Text = "British Pound", CurrencyCode = "GBP" },
			new() { Text = "Euro", CurrencyCode = "EUR" },
			new() { Text = "Australian Dollar", CurrencyCode = "AUD" },
			new() { Text = "Gold Spot", CurrencyCode = "XAU" }
		};

		var trainData = _mlContext.Data.LoadFromEnumerable(data);
		var pipeline = _mlContext.Transforms.Text
			.FeaturizeText("Features", nameof(CurrencyData.Text))
			.Append(_mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(CurrencyData.CurrencyCode)))
			.Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
			.Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

		_mlModel = pipeline.Fit(trainData);
	}

	private static string ExtractDateFromText(string text)
	{
		// Regex to match the date in the format "Oct 18, 2024"
		var dateMatch = Regex.Match(text, @"(\w{3})\s+(\d{1,2}),\s+(\d{4})");
		if (dateMatch.Success)
		{
			// Extract the month, day, and year
			var month = dateMatch.Groups[1].Value;
			var day = dateMatch.Groups[2].Value;
			var year = dateMatch.Groups[3].Value;

			// Convert month abbreviation to month number
			var monthNumber = DateTime.ParseExact(month, "MMM", CultureInfo.InvariantCulture)
				.Month;

			// Format the date as "YYYY/MM/DD"
			return $"{year}/{monthNumber:D2}/{day.PadLeft(2, '0')}";
		}

		return "Unknown";
	}

	private string ExtractCurrencyPair(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			Console.WriteLine("Input text is empty or null");
			return "Unknown";
		}

		var predictionEngine = _mlContext.Model.CreatePredictionEngine<CurrencyData, CurrencyPrediction>(_mlModel);
		var currencies = new List<string>();

		var words = text.Split(' ');
		for (var i = 0; i < words.Length - 1; i += 2)
		{
			var wordPair = $"{words[i]} {words[i + 1]}";
			var prediction = predictionEngine.Predict(new CurrencyData { Text = wordPair });
			if (!string.IsNullOrEmpty(prediction.PredictedLabel)) currencies.Add(prediction.PredictedLabel);
		}

		if (currencies.Count >= 2)
		{
			Console.WriteLine("Result brought by ML.");
			return $"{currencies[0]}/{currencies[1]}";
		}

		// Fallback
		for (var i = 0; i < words.Length - 1; i++)
		{
			var wordPair = $"{words[i]} {words[i + 1]}";
			var currencyCode = GetCurrencyCode(wordPair);
			if (!string.IsNullOrEmpty(currencyCode))
			{
				currencies.Add(currencyCode);
				i++;
			}
		}

		if (currencies.Count >= 2)
		{
			Console.WriteLine("Result brought by Fuzzy.");
			return $"{currencies[0]}/{currencies[1]}";
		}

		return "Unknown";
	}

	private static string GetCurrencyCode(string currencyName)
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

	private class CurrencyData
	{
		public string Text { get; set; }
		public string CurrencyCode { get; set; }
	}

	private class CurrencyPrediction
	{
		[ColumnName("PredictedLabel")] public string PredictedLabel { get; set; }
	}
}