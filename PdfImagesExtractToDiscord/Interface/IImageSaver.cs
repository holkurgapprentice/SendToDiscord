using System.Drawing;

public interface IImageSaver
{
	void SaveImage(Image img, string filePath);
}