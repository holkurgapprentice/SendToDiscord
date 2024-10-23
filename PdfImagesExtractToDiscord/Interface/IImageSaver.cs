using System.Drawing;

namespace PdfImagesExtractToDiscord.Interface;

public interface IImageSaver
{
	void SaveImage(Image img, string filePath);
}