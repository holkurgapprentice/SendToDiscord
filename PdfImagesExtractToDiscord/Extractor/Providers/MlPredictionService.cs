using Microsoft.ML;

namespace PdfImagesExtractToDiscord.Extractor.Providers;

public class MlPredictionService : IMlPredictionService
{
	private MLContext _mlContext;
	private ITransformer _mlModel;

	public MlPredictionService()
	{
		InitMlModel();
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

	public string GetCurrencyPair(string[] words)
	{
		List<string> currencies = new List<string>();
		var predictionEngine = _mlContext.Model.CreatePredictionEngine<CurrencyData, CurrencyPrediction>(_mlModel);

		for (var i = 0; i < words.Length - 1; i += 2)
		{
			string wordPair;
			if (i + 1 < words.Length)
				wordPair = $"{words[i]} {words[i + 1]}";
			else
				wordPair = words[i];

			var prediction = predictionEngine.Predict(new CurrencyData { Text = wordPair });
			if (!string.IsNullOrEmpty(prediction.PredictedLabel)) 
				currencies.Add(prediction.PredictedLabel);
		}

		if (currencies.Count >= 2)
		{
			return $"{currencies[0]}/{currencies[1]}";
		}

		return null;
	}
}