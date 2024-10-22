public class ImageDetailsModel
{
	public string SymbolName { get; set; }
	public string Interval { get; set; }

	public string DateString { get; set; }
	public string Message => $"{DateString} - {SymbolName}: {Interval}";
}