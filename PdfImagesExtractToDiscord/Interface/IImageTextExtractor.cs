using PdfImagesExtractToDiscord.Model;

namespace PdfImagesExtractToDiscord.Interface;

public interface IImageTextExtractor
{
	ImageDetailsModel GetImageDetails(FileStream fileStream);
}