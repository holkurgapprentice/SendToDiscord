using Microsoft.ML.Data;

namespace PdfImagesExtractToDiscord.Extractor.Providers;

public class CurrencyPrediction
{
	[ColumnName("PredictedLabel")] public string PredictedLabel { get; set; }
}