using System.Text;

namespace RepoAgentMemory.Core;

public sealed record CompactedContext(
    string SummaryOutline,
    int OriginalEstimatedTokens,
    int CompactedEstimatedTokens,
    double ReductionPercentage
);

public sealed class HierarchicalContextCompactor
{
    public static CompactedContext Compact(SymbolGraph graph, int originalCharCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// === HIERARCHICAL REPOSITORY SYMBOL SKELETON ===");
        sb.AppendLine("// Generated for LLM prompt context injection");

        var byFile = graph.Symbols.Values.GroupBy(s => s.FilePath);

        foreach (var fileGroup in byFile)
        {
            sb.AppendLine($"\n// File: {fileGroup.Key}");
            var types = fileGroup.Where(s => s.Kind is SymbolKind.Class or SymbolKind.Interface or SymbolKind.Record).ToList();

            foreach (var typeSymbol in types)
            {
                var deps = typeSymbol.Dependencies.Count > 0 ? $" : {string.Join(", ", typeSymbol.Dependencies)}" : "";
                sb.AppendLine($"{typeSymbol.Signature}{deps}");
                sb.AppendLine("{");

                var members = fileGroup
                    .Where(s => s.Id.StartsWith(typeSymbol.Id + ".") && (s.Kind == SymbolKind.Method || s.Kind == SymbolKind.Property))
                    .OrderBy(s => s.StartLine);

                foreach (var member in members)
                {
                    sb.AppendLine($"    {member.Signature};");
                }

                sb.AppendLine("}");
            }
        }

        var compactedText = sb.ToString();
        var origTokens = Math.Max(1, originalCharCount / 4);
        var compactTokens = Math.Max(1, compactedText.Length / 4);
        var reduction = (1.0 - ((double)compactTokens / Math.Max(origTokens, compactTokens))) * 100.0;

        return new CompactedContext(
            SummaryOutline: compactedText,
            OriginalEstimatedTokens: origTokens,
            CompactedEstimatedTokens: compactTokens,
            ReductionPercentage: Math.Max(0.0, reduction)
        );
    }
}
