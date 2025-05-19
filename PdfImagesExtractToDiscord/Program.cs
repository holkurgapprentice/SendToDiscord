using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PdfImagesExtractToDiscord.Extractor;
using PdfImagesExtractToDiscord.Extractor.Providers;
using PdfImagesExtractToDiscord.Extractor.Validator;
using PdfImagesExtractToDiscord.FileHandler;
using PdfImagesExtractToDiscord.Interface;
using PdfImagesExtractToDiscord.Model;
using PdfImagesExtractToDiscord.Processor;
using PdfImagesExtractToDiscord.Sender;
using Serilog;
using Serilog.Events;

namespace PdfImagesExtractToDiscord;

internal class Program
{
	private static async Task Main(string[] args)
	{
		// Configure Serilog first
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
			.Enrich.FromLogContext()
			.WriteTo.Console()
			.WriteTo.File(
				path: "logs/app-.log",
				rollingInterval: RollingInterval.Day,
				outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
			.CreateLogger();
		try
		{
			var host = CreateHostBuilder(args).Build();

			Log.Information("Starting application");
			var fileFeedToProcessModel = await GetPngsToProcess(host);
			var discordSender = host.Services.GetRequiredService<IDiscordSender>();

			fileFeedToProcessModel.ProcessedPdfsRelatedPngsList = (await discordSender
					.PostAsync(fileFeedToProcessModel)
				).ToList();

			var fileSystem = host.Services.GetRequiredService<IFileSystem>();
			fileSystem.Clean(fileFeedToProcessModel);

			Log.Information("Finished work, press any key to exit");
			Console.ReadLine();
		}
		catch (Exception ex)
		{
			Log.Fatal(ex, "Application terminated unexpectedly");
		}
		finally
		{
			Log.CloseAndFlush();
		}
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
				services.AddLogging(loggingBuilder =>
					loggingBuilder.AddSerilog(dispose: true));
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