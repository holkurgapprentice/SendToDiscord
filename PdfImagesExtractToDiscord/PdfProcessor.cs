using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Configuration;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PdfImagesExtractToDiscord;

internal class PdfProcessor : IPdfProcessor
{
	private readonly IConfiguration _configuration;
	private readonly IFileSystem _fileSystem;
	private readonly IImageSaver _imageSaver;
	private readonly string _searchPattern;

	public PdfProcessor(IFileSystem fileSystem, IImageSaver imageSaver, IConfiguration
		configuration)
	{
		_fileSystem = fileSystem;
		_imageSaver = imageSaver;
		_configuration = configuration;
		_searchPattern = _configuration["Files:PdfMask"];
	}

	public List<string> GetPdfsInDirectory(string directory)
	{
		Console.WriteLine($"GetPdfsInDirectory - Current directory: {directory}");

		var pdfFiles = _fileSystem.GetFiles(directory, _searchPattern);

		if (pdfFiles.Length == 0)
		{
			Console.WriteLine(
				$"GetPdfsInDirectory - No PDF files matching the pattern '{_searchPattern}' were found in the current directory.");
			return [];
		}

		return [..pdfFiles];
	}

	public async Task<List<string>> ProcessPdfsInDirectory(List<string> resultPdfs, string directory)
	{
		Console.WriteLine($"ProcessPdfsInDirectory - Current directory: {directory}");

		if (!resultPdfs.Any())
		{
			Console.WriteLine(
				"No PDF files provided.");
			return new List<string>();
		}

		var createdFiles = new List<string>();

		foreach (var pdfFile in resultPdfs)
		{
			Console.WriteLine($"Found PDF file: {pdfFile}");
			var pdfBytes = await _fileSystem.ReadAllBytesAsync(pdfFile);
			createdFiles.AddRange(await ProcessPdfFileAsync(pdfFile, pdfBytes));
		}

		return createdFiles;
	}

	private async Task<List<string>> ProcessPdfFileAsync(string pdfFilePath, byte[] pdfBytes)
	{
		var imageCounter = 1;
		var pdfFileName = _fileSystem.GetFileNameWithoutExtension(pdfFilePath);
		Console.WriteLine($"Processing PDF file: {pdfFilePath}");

		var createdFiles = new List<string>();

		using var document = PdfDocument.Open(pdfBytes);
		foreach (var page in document.GetPages())
		foreach (var pdfImage in page.GetImages())
		{
			var img = ExtractImage(pdfImage);
			if (img == null) continue;

			var imageFileName = CreateImageFileName(pdfFileName, imageCounter, img);
			var imageFilePath =
				_fileSystem.CombinePaths(_fileSystem.GetDirectoryName(pdfFilePath), imageFileName);

			_imageSaver.SaveImage(img, imageFilePath);
			Console.WriteLine($"Saved image: {imageFileName}");

			createdFiles.Add(imageFilePath);
			imageCounter++;
		}

		return createdFiles;
	}

	private Image ExtractImage(IPdfImage pdfImage)
	{
		var bytes = TryGetImage(pdfImage);
		using var mem = new MemoryStream(bytes);
		try
		{
			return Image.FromStream(mem);
		}
		catch
		{
			return null;
		}
	}

	private string CreateImageFileName(string pdfFileName, int imageCounter, Image img)
	{
		var codec = ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == img.RawFormat.Guid);
		var extension = codec.FilenameExtension.Split(';').First().TrimStart('*', '.').ToLower();
		return $"{pdfFileName}_{imageCounter}.{extension}";
	}

	private byte[] TryGetImage(IPdfImage image)
	{
		if (image.TryGetPng(out var bytes))
			return bytes;

		if (image.TryGetBytes(out var iroBytes))
			return iroBytes.ToArray();

		return image.RawBytes.ToArray();
	}
}