using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PdfImagesExtractToDiscord;

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
		fileSystem.Clean(fileFeedToProcessModel);

		Console.WriteLine("Finished work, press any key to exit.");
		Console.ReadLine();
	}

	private static IHostBuilder CreateHostBuilder(string[] args)
	{
		return Host.CreateDefaultBuilder(args)
			.ConfigureAppConfiguration((_, config) =>
			{
				config.AddJsonFile("appsettings.json", false, true);
				config.AddUserSecrets<Program>();
			})
			.ConfigureServices((hostContext, services) =>
			{
				services.AddSingleton(hostContext.Configuration);
				services.AddScoped<IImageTextExtractor, ImageTextExtractor>();
				services.AddScoped<IDiscordSender, DiscordSender>();
				services.AddScoped<IFileSystem, FileSystem>();
				services.AddScoped<IImageSaver, ImageSaver>();
				services.AddScoped<IPdfProcessor, PdfProcessor>();
				services.AddScoped<IPngProcessor, PngProcessor>();
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
}