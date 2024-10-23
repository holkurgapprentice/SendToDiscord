using PdfImagesExtractToDiscord.Model;

namespace PdfImagesExtractToDiscord.Interface;

public interface IFileSystem
{
	string[] GetFiles(string path, string fileNamePattern);
	Task<byte[]> ReadAllBytesAsync(string path);
	string GetFileNameWithoutExtension(string path);
	string GetDirectoryName(string path);
	string CombinePaths(string path1, string path2);
	void Delete(string filePath);
	void Clean(FileFeedToProcessModel fileFeedToProcessModel);
}