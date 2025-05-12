using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PdfImagesExtractToDiscord.Interface;

namespace PdfImagesExtractToDiscord.Processor;

public class PngProcessor : IPngProcessor
{
	private readonly IConfiguration _configuration;
	private readonly ILogger<PngProcessor> _logger;
	private readonly IFileSystem _fileSystem;
	private readonly string _searchPattern;

	public PngProcessor(IFileSystem fileSystem, IConfiguration configuration, ILogger<PngProcessor> logger)
	{
		_fileSystem = fileSystem;
		_configuration = configuration;
		_logger = logger;
		_searchPattern = _configuration["Files:PngMask"];
	}

	public async Task<List<string>> ProcessPngsInDirectory(string directory)
	{
		_logger.LogInformation($"ProcessPngsInDirectory - Current directory: {directory}");

		var pngFiles = _fileSystem.GetFiles(directory, _searchPattern);

		if (pngFiles.Length == 0)
			_logger.LogInformation(
				$"ProcessPngsInDirectory - No PNG files matching the pattern '{_searchPattern}' were found in the current directory.");

		foreach (var pngFile in pngFiles) _logger.LogInformation($"ProcessPngsInDirectory - Found PNG file: {pngFile}");

		return pngFiles.ToList();
	}
}