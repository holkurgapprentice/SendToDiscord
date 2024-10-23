namespace PdfImagesExtractToDiscord.Model;

public class FileFeedToProcessModel
{
	public List<string> Pdfs { get; set; }
	public List<string> PdfsRelatedPngsList { get; set; }
	public List<string> ManuallyProvidedPngsList { get; set; }
	public List<string> ProcessedPdfsRelatedPngsList { get; set; }
}