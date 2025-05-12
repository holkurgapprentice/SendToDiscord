using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PdfImagesExtractToDiscord.Extractor.Providers;
using PdfImagesExtractToDiscord.Extractor.Validator;
using PdfImagesExtractToDiscord.Interface;
using PdfImagesExtractToDiscord.Model;
using Tesseract;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace PdfImagesExtractToDiscord.Extractor;

public partial class ImageTextExtractor : IImageTextExtractor
{
	private readonly IMlPredictionService _mlPredictionService;
	private readonly IFuzzyMatchService _fuzzyMatchService;
	private readonly ICurrencyPairValidator _currencyPairValidator;
	private readonly ILogger<ImageTextExtractor> _logger;

	public ImageTextExtractor(ILogger<ImageTextExtractor> logger, IMlPredictionService mlPredictionService, IFuzzyMatchService fuzzyMatchService, ICurrencyPairValidator currencyPairValidator)
	{
		_mlPredictionService = mlPredictionService;
		_fuzzyMatchService = fuzzyMatchService;
		_currencyPairValidator = currencyPairValidator;
		_logger = logger;
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
							_logger.LogInformation($"OCR Text: {text}"); // For debugging

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
							_logger.LogInformation($"Currency pair line: {currencyPairLine}");
							var symbolName = ExtractCurrencyPair(currencyPairLine);
							_logger.LogInformation($"Extracted symbol name: {symbolName}");

							// Extract interval
							var intervalMatch = Regex.Match(currencyPairLine, @"(\d*[HDWMhdwm])", RegexOptions.IgnoreCase);
							var interval = intervalMatch.Success ? intervalMatch.Groups[1].Value : "Unknown";

							_logger.LogInformation($"{currencyPairLine} - {symbolName}: {interval}");

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

	public string ExtractCurrencyPair(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			_logger.LogInformation("Input text is empty or null");
			return "Unknown";
		}

		var words = text.Split(' ');
        
		// Try extraction methods in order of reliability
		var extractionMethods = new List<Func<string[], ExtractionResult>>
		{
			TryExtractWithML,
			TryExtractWithFuzzyMatch
		};

		foreach (var method in extractionMethods)
		{
			var result = method(words);
			if (result.IsSuccessful)
			{
				return result.CurrencyPair;
			}
		}

		return "Unknown";
	}
	
	
	private ExtractionResult TryExtractWithML(string[] words)
	{
		var currencyPair = _mlPredictionService.GetCurrencyPair(words);
		return ValidateExtractionResult(currencyPair, "ML");
	}

	private ExtractionResult TryExtractWithFuzzyMatch(string[] words)
	{
		var currencyPair = _fuzzyMatchService.GetCurrencyPair(words);
		return ValidateExtractionResult(currencyPair, "Fuzzy Match");
	}
	
	private ExtractionResult ValidateExtractionResult(string currencyPair, string method)
	{
		if (string.IsNullOrEmpty(currencyPair))
			return ExtractionResult.Failure();

		var isValid = _currencyPairValidator.IsValidCurrencyPair(currencyPair);
		if (isValid)
		{
			_logger.LogInformation($"Result brought by {method} is validated.");
			return ExtractionResult.Success(currencyPair);
		}
        
		_logger.LogInformation($"Result of {method} {currencyPair} invalidated.");
		return ExtractionResult.Failure();
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
}