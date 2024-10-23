using System.Drawing;
using PdfImagesExtractToDiscord.Interface;

namespace PdfImagesExtractToDiscord.FileHandler;

public class ImageSaver : IImageSaver
{
	public void SaveImage(Image img, string filePath)
	{
		img.Save(filePath, img.RawFormat);
	}
}