namespace PdfImagesExtractToDiscord;

public static class Extenstions
{
	public static IEnumerable<T> EmptyWhenNull<T>(this IEnumerable<T> source)
	{
		return source ?? Enumerable.Empty<T>();
	}
}