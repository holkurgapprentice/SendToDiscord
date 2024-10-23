using PdfImagesExtractToDiscord.Model;

namespace PdfImagesExtractToDiscord.Interface;

public interface IDiscordSender
{
	Task<IEnumerable<string>> PostAsync(FileFeedToProcessModel fileFeedToProcessModel);
}