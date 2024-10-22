namespace PdfImagesExtractToDiscord;

public interface IPngProcessor
{
	Task<List<string>> ProcessPngsInDirectory(string directory);
}