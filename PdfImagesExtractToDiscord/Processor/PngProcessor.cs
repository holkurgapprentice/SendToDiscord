using Microsoft.Extensions.Configuration;
using PdfImagesExtractToDiscord.Interface;

namespace PdfImagesExtractToDiscord.Processor;

public class PngProcessor : IPngProcessor
{
	private readonly IConfiguration _configuration;
	private readonly IFileSystem _fileSystem;
	private readonly string _searchPattern;

	public PngProcessor(IFileSystem fileSystem, IConfiguration configuration)
	{
		_fileSystem = fileSystem;
		_configuration = configuration;
		_searchPattern = _configuration["Files:PngMask"];
	}

	public async Task<List<string>> ProcessPngsInDirectory(string directory)
	{
		Console.WriteLine($"ProcessPngsInDirectory - Current directory: {directory}");

		var pngFiles = _fileSystem.GetFiles(directory, _searchPattern);

		if (pngFiles.Length == 0)
			Console.WriteLine(
				$"ProcessPngsInDirectory - No PNG files matching the pattern '{_searchPattern}' were found in the current directory.");

		foreach (var pngFile in pngFiles) Console.WriteLine($"ProcessPngsInDirectory - Found PNG file: {pngFile}");

		return pngFiles.ToList();
	}
}