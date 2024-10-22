using System.Drawing;

public class ImageSaver : IImageSaver
{
	public void SaveImage(Image img, string filePath)
	{
		img.Save(filePath, img.RawFormat);
	}
}