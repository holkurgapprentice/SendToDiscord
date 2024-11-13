using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using PdfImagesExtractToDiscord.Extension;
using PdfImagesExtractToDiscord.Interface;
using PdfImagesExtractToDiscord.Model;

namespace PdfImagesExtractToDiscord.Sender;

public class DiscordSender : IDiscordSender
{
	private DiscordSocketClient _client;
	private readonly IConfiguration _configuration;
	private readonly IFileSystem _fileSystem;
	private readonly IImageTextExtractor _imageTextExtractor;
	private IEnumerable<IMessage>? _lastMessages;
	private IMessageChannel _mainChannel;

	public DiscordSender(IConfiguration configuration, IImageTextExtractor imageTextExtractor, IFileSystem fileSystem)
	{
		_configuration = configuration;
		_imageTextExtractor = imageTextExtractor;
		_fileSystem = fileSystem;
	}

	private async Task InitHistoryOnReady()
	{
		try
		{
			var channel = await _client.GetChannelAsync(Convert.ToUInt64(_configuration["discord:channel"]));
			if (channel != null && channel is IMessageChannel msgChannel)
			{
				_mainChannel = msgChannel;
				_lastMessages = await msgChannel.GetMessagesAsync(100).FlattenAsync();
			}
			_lastMessages = _lastMessages ?? new List<IMessage>();
		}
		catch (Exception e)
		{
			Console.WriteLine($"Possibly insufficient privileges error, {e}");
			throw;
		}
	}

	public async Task<IEnumerable<string>> PostAsync(FileFeedToProcessModel feedToProcessModel)
	{
		if (_client == null)
			await InitAsync();

		var result = new List<string>();
		
		await PostSummaryAsync(feedToProcessModel);

		foreach (var imagePath in feedToProcessModel.PdfsRelatedPngsList)
			if (await PostSingleImageAsync(imagePath))
				result.Add(imagePath);

		foreach (var imagePath in feedToProcessModel.ManuallyProvidedPngsList)
			await PostSingleImageAsync(imagePath, false);

		Console.WriteLine($"Posted {result.Count} images to Discord.");
		return result;
	}

	private async Task InitAsync()
	{
		_client = new DiscordSocketClient();
		_client.Ready += InitHistoryOnReady;
		await _client.LoginAsync(TokenType.Bot, _configuration["discord:token"]);
		await _client.StartAsync();

		int cycles = 0;
		while (_lastMessages == null)
		{
			await Task.Delay(5000);
			cycles++;

			if (cycles > 10)
				throw new Exception("List of old messages not initialised");
		}
	}

	private async Task PostSummaryAsync(FileFeedToProcessModel feedToProcessModel)
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
			
			await _mainChannel.SendFilesAsync(attachments, messagePostSummary);

			foreach (var attachment in attachments)
				attachment.Stream.Dispose(); // Ensure streams are disposed

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

	private async Task<bool> PostSingleImageAsync(string imagePath,
		bool isCleanAfterSuccess = true)
	{
		try
		{
			using (var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
			{
				var fileName = Path.GetFileName(imagePath);
				var imageDetails = _imageTextExtractor.GetImageDetails(fileStream);
				var lastMessage = _lastMessages
					.Where(message => message.Content.Contains(imageDetails.SymbolName))
					.OrderByDescending(messgae => messgae.Timestamp)
					.FirstOrDefault();

				IMessage currentMessage = null;
				
				if (lastMessage == null)
					currentMessage = await _mainChannel.SendFileAsync(fileStream, fileName, imageDetails.Message);
				else
					currentMessage = await _mainChannel.SendFileAsync(fileStream, fileName, imageDetails.Message, messageReference: new MessageReference(lastMessage.Id));

				if (currentMessage != null)
					_lastMessages = _lastMessages.Append(currentMessage);

				Console.WriteLine($"Posted image: {imagePath}, replied: {(lastMessage == null ? "no" : "yes" )}");
			}

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