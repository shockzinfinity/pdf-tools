using System.Globalization;

namespace PdfTool.Commands;

/// <summary>
/// Parses page range expressions such as "1-3,5,8-10" into a sorted, distinct,
/// 1-based set of page numbers. Validates each page against the document's
/// total page count.
/// </summary>
public static class PageRangeParser
{
  public static IReadOnlyList<int> Parse(string? expression, int totalPages)
  {
    if (totalPages <= 0)
      throw new ArgumentOutOfRangeException(nameof(totalPages), "Document has no pages.");

    // No expression → every page
    if (string.IsNullOrWhiteSpace(expression))
      return Enumerable.Range(1, totalPages).ToArray();

    var result = new SortedSet<int>();
    var tokens = expression.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    foreach (var token in tokens)
    {
      if (token.Contains('-'))
      {
        var parts = token.Split('-', 2);
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var start)
            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var end))
        {
          throw new FormatException($"Invalid range token: '{token}'. Expected 'N-M'.");
        }

        if (start < 1 || end < 1)
          throw new FormatException($"Page numbers must be >= 1 (got '{token}').");
        if (start > end)
          throw new FormatException($"Range start must be <= end (got '{token}').");
        if (end > totalPages)
          throw new ArgumentOutOfRangeException(nameof(expression),
              $"Range '{token}' exceeds document page count ({totalPages}).");

        for (var p = start; p <= end; p++)
          result.Add(p);
      }
      else
      {
        if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var page))
          throw new FormatException($"Invalid page token: '{token}'.");
        if (page < 1 || page > totalPages)
          throw new ArgumentOutOfRangeException(nameof(expression),
              $"Page '{page}' is out of range (1..{totalPages}).");
        result.Add(page);
      }
    }

    return result.ToArray();
  }
}
