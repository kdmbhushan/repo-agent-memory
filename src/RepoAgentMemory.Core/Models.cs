namespace RepoAgentMemory.Core;

public enum SymbolKind
{
    Class,
    Interface,
    Record,
    Struct,
    Method,
    Property,
    Enum
}

public sealed record CodeSymbol(
    string Id,
    string Name,
    SymbolKind Kind,
    string FilePath,
    int StartLine,
    int EndLine,
    string Signature,
    string? DocComment = null,
    IReadOnlyList<string>? Dependencies = null
)
{
    public IReadOnlyList<string> Dependencies { get; init; } = Dependencies ?? Array.Empty<string>();
}

public sealed class SymbolGraph
{
    private readonly Dictionary<string, CodeSymbol> _symbols = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _outgoingEdges = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _incomingEdges = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, CodeSymbol> Symbols => _symbols;

    public void AddSymbol(CodeSymbol symbol)
    {
        _symbols[symbol.Id] = symbol;

        if (!_outgoingEdges.ContainsKey(symbol.Id))
            _outgoingEdges[symbol.Id] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!_incomingEdges.ContainsKey(symbol.Id))
            _incomingEdges[symbol.Id] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dep in symbol.Dependencies)
        {
            _outgoingEdges[symbol.Id].Add(dep);

            if (!_incomingEdges.ContainsKey(dep))
                _incomingEdges[dep] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            _incomingEdges[dep].Add(symbol.Id);
        }
    }

    public IReadOnlySet<string> GetDependencies(string symbolId) =>
        _outgoingEdges.TryGetValue(symbolId, out var deps) ? deps : new HashSet<string>();

    public IReadOnlySet<string> GetDependents(string symbolId) =>
        _incomingEdges.TryGetValue(symbolId, out var dependents) ? dependents : new HashSet<string>();

    public IEnumerable<CodeSymbol> FindSymbolsByKind(SymbolKind kind) =>
        _symbols.Values.Where(s => s.Kind == kind);

    public int GetConnectivityScore(string symbolId)
    {
        var outCount = _outgoingEdges.TryGetValue(symbolId, out var o) ? o.Count : 0;
        var inCount = _incomingEdges.TryGetValue(symbolId, out var i) ? i.Count : 0;
        return (inCount * 2) + outCount; // Dependents (incoming references) weighted higher
    }
}
