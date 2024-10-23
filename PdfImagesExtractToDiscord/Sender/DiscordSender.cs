using Discord;
using Discord.Webhook;
using Microsoft.Extensions.Configuration;
using PdfImagesExtractToDiscord.Extension;
using PdfImagesExtractToDiscord.Interface;
using PdfImagesExtractToDiscord.Model;

namespace PdfImagesExtractToDiscord.Sender;

public class DiscordSender : IDiscordSender
{
	private readonly IConfiguration _config;
	private readonly IFileSystem _fileSystem;
	private readonly IImageTextExtractor _imageTextExtractor;
	private readonly string _webhookUrl;

	public DiscordSender(IConfiguration config, IImageTextExtractor imageTextExtractor, IFileSystem fileSystem)
	{
		_config = config;
		_webhookUrl = _config["discordWebHook"];
		_imageTextExtractor = imageTextExtractor;
		_fileSystem = fileSystem;
	}

	public async Task<IEnumerable<string>> PostAsync(FileFeedToProcessModel feedToProcessModel)
	{
		var result = new List<string>();
		using (var client = new DiscordWebhookClient(_webhookUrl))
		{
			await PostSummaryAsync(client, feedToProcessModel);

			foreach (var imagePath in feedToProcessModel.PdfsRelatedPngsList)
				if (await PostSingleImageAsync(client, imagePath))
					result.Add(imagePath);

			foreach (var imagePath in feedToProcessModel.ManuallyProvidedPngsList)
				await PostSingleImageAsync(client, imagePath, false);
		}

		Console.WriteLine($"Posted {result.Count} images to Discord.");
		return result;
	}

	private async Task PostSummaryAsync(DiscordWebhookClient client, FileFeedToProcessModel feedToProcessModel)
	{
		try
		{
			var attachments = new List<FileAttachment>();
			foreach (var filePath in feedToProcessModel.Pdfs)
			{
				var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
				var fileName = Path.GetFileName(filePath);
				attachments.Add(new FileAttachment(fileStream, fileName));
			}

			var messagePostSummary = GetMessagePostSummary(feedToProcessModel);
			await client.SendFilesAsync(attachments, messagePostSummary);

			foreach (var attachment in attachments) attachment.Stream.Dispose(); // Ensure streams are disposed

			Console.WriteLine("Posted multiple files with");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error posting multiple files: {ex.Message}");
			Console.WriteLine($"Exception details: {ex}");
		}
	}

	private static string? GetMessagePostSummary(FileFeedToProcessModel feedToProcessModel)
	{
		return $"{Path.GetFileName(feedToProcessModel.Pdfs.EmptyWhenNull().FirstOrDefault())}{Environment.NewLine}" +
		       $"Currencies: {feedToProcessModel.PdfsRelatedPngsList.EmptyWhenNull().Count()}{Environment.NewLine}" +
		       $"Commodities :{feedToProcessModel.ManuallyProvidedPngsList.EmptyWhenNull().Count()}{Environment.NewLine}" +
		       $"Pdfs: {feedToProcessModel.Pdfs.EmptyWhenNull().Count()}{Environment.NewLine}";
	}

	private async Task<bool> PostSingleImageAsync(DiscordWebhookClient client, string imagePath,
		bool isCleanAfterSuccess = true)
	{
		try
		{
			using (var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
			{
				var fileName = Path.GetFileName(imagePath);
				var imageDetails = _imageTextExtractor.GetImageDetails(fileStream);
				await client.SendFileAsync(fileStream, fileName, imageDetails.Message);
			}

			Console.WriteLine($"Posted image: {imagePath}");
			if (isCleanAfterSuccess)
			{
				_fileSystem.Delete(imagePath);
				Console.WriteLine($"Deleted image: {imagePath}");
			}

			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error posting image {imagePath}, isClean: {isCleanAfterSuccess}, ex mes: {ex.Message}");
			Console.WriteLine($"Exception details: {ex}");
			return false;
		}
	}
}