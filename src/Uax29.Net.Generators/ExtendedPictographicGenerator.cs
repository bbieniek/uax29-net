using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Uax29.Net.Generators;

[Generator]
public sealed class ExtendedPictographicGenerator : ISourceGenerator
{
    private const string EmojiDataFileName = "emoji-data.txt";

    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var emojiData = context.AdditionalFiles.FirstOrDefault(static f =>
            string.Equals(Path.GetFileName(f.Path), EmojiDataFileName, StringComparison.OrdinalIgnoreCase));

        if (emojiData is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "UAX29001",
                    title: "emoji-data.txt not found",
                    messageFormat: "Could not find required AdditionalFile '{0}'.",
                    category: "Uax29.Generator",
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                Location.None,
                EmojiDataFileName));
            return;
        }

        var text = emojiData.GetText(context.CancellationToken)?.ToString();
        if (text is null || string.IsNullOrWhiteSpace(text))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "UAX29002",
                    title: "emoji-data.txt is empty",
                    messageFormat: "AdditionalFile '{0}' is empty or unreadable.",
                    category: "Uax29.Generator",
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                Location.None,
                EmojiDataFileName));
            return;
        }

        var ranges = ParseExtendedPictographicRanges(text);
        if (ranges.Count == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "UAX29003",
                    title: "No Extended_Pictographic ranges found",
                    messageFormat: "No Extended_Pictographic ranges were parsed from '{0}'.",
                    category: "Uax29.Generator",
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                Location.None,
                EmojiDataFileName));
            return;
        }

        var source = BuildSource(ranges);
        context.AddSource("WordBreakTokenizer.UnicodeData.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private static List<(int Start, int End)> ParseExtendedPictographicRanges(string content)
    {
        var ranges = new List<(int Start, int End)>();

        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var hashIndex = line.IndexOf('#');
            if (hashIndex >= 0)
            {
                line = line.Substring(0, hashIndex);
            }

            line = line.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var parts = line.Split(';');
            if (parts.Length < 2)
            {
                continue;
            }

            var rangePart = parts[0].Trim();
            var propPart = parts[1].Trim();
            if (!string.Equals(propPart, "Extended_Pictographic", StringComparison.Ordinal))
            {
                continue;
            }

            int start;
            int end;
            var dots = rangePart.IndexOf("..", StringComparison.Ordinal);
            if (dots >= 0)
            {
                start = int.Parse(rangePart.Substring(0, dots), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                end = int.Parse(rangePart.Substring(dots + 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
            else
            {
                start = int.Parse(rangePart, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                end = start;
            }

            ranges.Add((start, end));
        }

        ranges.Sort(static (a, b) => a.Start.CompareTo(b.Start));

        // Merge contiguous/overlapping ranges to keep emitted source compact.
        var merged = new List<(int Start, int End)>();
        foreach (var range in ranges)
        {
            if (merged.Count == 0)
            {
                merged.Add(range);
                continue;
            }

            var last = merged[merged.Count - 1];
            if (range.Start <= last.End + 1)
            {
                merged[merged.Count - 1] = (last.Start, Math.Max(last.End, range.End));
            }
            else
            {
                merged.Add(range);
            }
        }

        return merged;
    }

    private static string BuildSource(IEnumerable<(int Start, int End)> ranges)
    {
        var sb = new StringBuilder();
        sb.AppendLine("namespace Uax29.Net");
        sb.AppendLine("{");
        sb.AppendLine("    public static partial class WordBreakTokenizer");
        sb.AppendLine("    {");
        sb.AppendLine("        private readonly struct CodePointRange");
        sb.AppendLine("        {");
        sb.AppendLine("            public readonly int Start;");
        sb.AppendLine("            public readonly int End;");
        sb.AppendLine();
        sb.AppendLine("            public CodePointRange(int start, int end)");
        sb.AppendLine("            {");
        sb.AppendLine("                Start = start;");
        sb.AppendLine("                End = end;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // Source: Unicode emoji-data.txt, property Extended_Pictographic.");
        sb.AppendLine("        private static readonly CodePointRange[] ExtendedPictographicRanges =");
        sb.AppendLine("        {");

        foreach (var range in ranges)
        {
            sb.Append("            new CodePointRange(0x");
            sb.Append(range.Start.ToString("X", CultureInfo.InvariantCulture));
            sb.Append(", 0x");
            sb.Append(range.End.ToString("X", CultureInfo.InvariantCulture));
            sb.AppendLine("),");
        }

        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        private static bool IsInExtendedPictographicRanges(int codePoint)");
        sb.AppendLine("        {");
        sb.AppendLine("            var lo = 0;");
        sb.AppendLine("            var hi = ExtendedPictographicRanges.Length - 1;");
        sb.AppendLine();
        sb.AppendLine("            while (lo <= hi)");
        sb.AppendLine("            {");
        sb.AppendLine("                var mid = lo + ((hi - lo) >> 1);");
        sb.AppendLine("                var range = ExtendedPictographicRanges[mid];");
        sb.AppendLine();
        sb.AppendLine("                if (codePoint < range.Start)");
        sb.AppendLine("                {");
        sb.AppendLine("                    hi = mid - 1;");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine("                if (codePoint > range.End)");
        sb.AppendLine("                {");
        sb.AppendLine("                    lo = mid + 1;");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine("                return true;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
