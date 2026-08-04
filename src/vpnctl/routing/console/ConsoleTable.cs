using System.Text;

public class ConsoleTableHeader
{
    public string Name { get; init; } = string.Empty;
    public int Spacing { get; init; } = 10;
}

public class ConsoleTable
{
    private readonly List<string> _lines = new();

    private List<ConsoleTableHeader> _headers = new();

    public int PaddingLeft { get; init; } = 2;
    public int Width { get; init; } = 75;

    public IReadOnlyList<string> Build() => _lines;

    public void AddHeaders(params ConsoleTableHeader[] headers)
    {
        _headers = headers.ToList();

        _lines.Add(GetRow(_headers.Select(x => x.Name).ToArray()));
    }

    public void AddRow(params string[] values)
    {
        _lines.Add(GetRow(values));
    }

    public void AddText(string text)
    {
        _lines.Add(new string(' ', PaddingLeft) + text);
    }

    public void AddBorder()
    {
        _lines.Add(new string('=', Width + PaddingLeft));
    }

    public void AddSeparator()
    {
        _lines.Add(new string(' ', PaddingLeft) + new string('-', Width));
    }

    private string GetRow(params string[] values)
    {
        var sb = new StringBuilder();

        sb.Append(' ', PaddingLeft);

        for (int i = 0; i < _headers.Count; i++)
        {
            var value = i < values.Length ? values[i] : string.Empty;
            var isLast = i == _headers.Count - 1;

            sb.Append(FormatValue(value, _headers[i].Spacing, !isLast));
            if (!isLast) sb.Append(' ');
        }

        return sb.ToString();
    }

    private string FormatValue(string value, int width, bool truncate)
    {
        if (truncate && value.Length > width)
            return value[..(width - 2)] + "..";

        return value.PadRight(width);
    }
}