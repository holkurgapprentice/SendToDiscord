namespace PdfImagesExtractToDiscord;

internal interface IPdfProcessor
{
	List<string> GetPdfsInDirectory(string directory);
	Task<List<string>> ProcessPdfsInDirectory(List<string> resultPdfs, string directory);
}