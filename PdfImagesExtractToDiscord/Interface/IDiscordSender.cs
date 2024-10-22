public interface IDiscordSender
{
	Task<IEnumerable<string>> PostAsync(FileFeedToProcessModel fileFeedToProcessModel);
}