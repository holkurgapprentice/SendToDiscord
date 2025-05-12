using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PdfImagesExtractToDiscord.Extractor;
using PdfImagesExtractToDiscord.Extractor.Providers;
using PdfImagesExtractToDiscord.Extractor.Validator;
using PdfImagesExtractToDiscord.FileHandler;
using PdfImagesExtractToDiscord.Interface;
using PdfImagesExtractToDiscord.Model;
using PdfImagesExtractToDiscord.Processor;
using PdfImagesExtractToDiscord.Sender;

namespace PdfImagesExtractToDiscord;

internal class Program
{
	private static async Task Main(string[] args)
	{
		var host = CreateHostBuilder(args).Build();

		var fileFeedToProcessModel = await GetPngsToProcess(host);
		var discordSender = host.Services.GetRequiredService<IDiscordSender>();

		fileFeedToProcessModel.ProcessedPdfsRelatedPngsList = (await discordSender
				.PostAsync(fileFeedToProcessModel)
			).ToList();

		var fileSystem = host.Services.GetRequiredService<IFileSystem>();
		var _logger = host.Services.GetRequiredService<ILogger>();
		fileSystem.Clean(fileFeedToProcessModel);

		_logger.LogInformation("Finished work, press any key to exit.");
		Console.ReadLine();
	}

	private static IHostBuilder CreateHostBuilder(string[] args)
	{
		return Host.CreateDefaultBuilder(args)
			.ConfigureAppConfiguration((_, config) =>
			{
				config.AddJsonFile("appsettings.json", false, true);
#if DEBUG
				config.AddUserSecrets<Program>();
#endif
			})
			.ConfigureServices((hostContext, services) =>
			{
				services.Configure<CurrencyPairOptions>(hostContext.Configuration.GetSection("PossiblePairs"));

				services.AddSingleton(hostContext.Configuration);
				services.AddScoped<IImageTextExtractor, ImageTextExtractor>();
				services.AddScoped<IDiscordSender, DiscordSender>();
				services.AddScoped<IFileSystem, FileSystem>();
				services.AddScoped<IImageSaver, ImageSaver>();
				services.AddScoped<IPdfProcessor, PdfProcessor>();
				services.AddScoped<IPngProcessor, PngProcessor>();
				services.AddSingleton<IMlPredictionService, MlPredictionService>();
				services.AddSingleton<IFuzzyMatchService, FuzzyMatchService>();
				services.AddSingleton<ICurrencyPairValidator>(provider => 
				{
					var options = provider.GetRequiredService<IOptions<CurrencyPairOptions>>();
					return new CurrencyPairValidator(options.Value.PossiblePairs);
				});
				services.AddLogging(configure => 
				{
					configure.AddConsole();
					configure.AddDebug();
				});
			});
	}

	private static async Task<FileFeedToProcessModel> GetPngsToProcess(IHost host)
	{
		var pdfProcessor = host.Services.GetRequiredService<IPdfProcessor>();
		var pngProcessor = host.Services.GetRequiredService<IPngProcessor>();
		var result = new FileFeedToProcessModel
		{
			Pdfs = pdfProcessor.GetPdfsInDirectory(Directory.GetCurrentDirectory()),
			ManuallyProvidedPngsList = await pngProcessor.ProcessPngsInDirectory(Directory.GetCurrentDirectory())
		};
		result.PdfsRelatedPngsList =
			await pdfProcessor.ProcessPdfsInDirectory(result.Pdfs, Directory.GetCurrentDirectory());
		return result;
	}
	
	public class CurrencyPairOptions
	{
		public List<string> PossiblePairs { get; set; } = new List<string>();
	}
}