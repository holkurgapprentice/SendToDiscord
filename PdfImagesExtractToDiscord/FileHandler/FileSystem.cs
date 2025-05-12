using Microsoft.Extensions.Logging;
using PdfImagesExtractToDiscord.Extension;
using PdfImagesExtractToDiscord.Interface;
using PdfImagesExtractToDiscord.Model;

namespace PdfImagesExtractToDiscord.FileHandler;

internal class FileSystem : IFileSystem
{
	private readonly ILogger<FileSystem> _logger;

	public FileSystem(ILogger<FileSystem> logger)
	{
		_logger = logger;
	}
	public string[] GetFiles(string path, string fileNamePattern)
	{
		return Directory.GetFiles(path, fileNamePattern);
	}

	public Task<byte[]> ReadAllBytesAsync(string path)
	{
		return File.ReadAllBytesAsync(path);
	}

	public string GetFileNameWithoutExtension(string path)
	{
		return Path.GetFileNameWithoutExtension(path);
	}

	public string GetDirectoryName(string path)
	{
		return Path.GetDirectoryName(path);
	}

	public string CombinePaths(string path1, string path2)
	{
		return Path.Combine(path1, path2);
	}

	public void Delete(string filePath)
	{
		File.Delete(filePath);
	}

	public void Clean(FileFeedToProcessModel fileFeedToProcessModel)
	{
		if (fileFeedToProcessModel.ProcessedPdfsRelatedPngsList.EmptyWhenNull().Count() ==
		    fileFeedToProcessModel.PdfsRelatedPngsList.EmptyWhenNull().Count())

			CleanOnSuccess(fileFeedToProcessModel);
		else
			CleanOnFail(fileFeedToProcessModel);
	}

	private void CleanOnSuccess(FileFeedToProcessModel fileFeedToProcessModel)
	{
		_logger.LogInformation("Cleaning all files");

		var allFiles = fileFeedToProcessModel.Pdfs.EmptyWhenNull().Concat(
			fileFeedToProcessModel.ProcessedPdfsRelatedPngsList.EmptyWhenNull()).Concat(
			fileFeedToProcessModel.ManuallyProvidedPngsList.EmptyWhenNull()).ToList();

		foreach (var filePath in allFiles)
			try
			{
				_logger.LogInformation($"Cleaning file {filePath}");
				File.Delete(filePath);
			}
			catch (Exception e)
			{
				_logger.LogInformation($"Failed to delete file {filePath}: {e.Message}");
			}
	}

	private void CleanOnFail(FileFeedToProcessModel fileFeedToProcessModel)
	{
		foreach (var temporaryFile in fileFeedToProcessModel.PdfsRelatedPngsList.EmptyWhenNull().Where(
			         pdfRelatedPngPath =>
				         !fileFeedToProcessModel.ProcessedPdfsRelatedPngsList.Contains(pdfRelatedPngPath)))
			try
			{
				_logger.LogInformation($"Cleaning temporary file {temporaryFile}");
				File.Delete(temporaryFile);
			}
			catch (Exception e)
			{
				_logger.LogInformation($"Failed to delete temporary file {temporaryFile}: {e.Message}");
			}
	}
}