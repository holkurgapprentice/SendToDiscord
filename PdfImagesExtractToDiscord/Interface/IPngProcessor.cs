namespace PdfImagesExtractToDiscord.Interface;

public interface IPngProcessor
{
	Task<List<string>> ProcessPngsInDirectory(string directory);
}