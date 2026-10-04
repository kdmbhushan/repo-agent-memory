using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RepoAgentMemory.Core;

public sealed class RoslynSymbolExtractor : CSharpSyntaxWalker
{
    private readonly string _filePath;
    private readonly List<CodeSymbol> _symbols = new();
    private string _currentNamespace = string.Empty;
    private string _currentType = string.Empty;

    public RoslynSymbolExtractor(string filePath)
    {
        _filePath = filePath;
    }

    public static IReadOnlyList<CodeSymbol> ExtractFromSource(string sourceCode, string filePath = "source.cs")
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetCompilationUnitRoot();
        var extractor = new RoslynSymbolExtractor(filePath);
        extractor.Visit(root);
        return extractor._symbols;
    }

    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        _currentNamespace = node.Name.ToString();
        base.VisitFileScopedNamespaceDeclaration(node);
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        var prev = _currentNamespace;
        _currentNamespace = string.IsNullOrEmpty(_currentNamespace)
            ? node.Name.ToString()
            : $"{_currentNamespace}.{node.Name}";

        base.VisitNamespaceDeclaration(node);
        _currentNamespace = prev;
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        ProcessTypeDeclaration(node, node.Identifier.Text, SymbolKind.Class, node.BaseList);
        base.VisitClassDeclaration(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        ProcessTypeDeclaration(node, node.Identifier.Text, SymbolKind.Interface, node.BaseList);
        base.VisitInterfaceDeclaration(node);
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        ProcessTypeDeclaration(node, node.Identifier.Text, SymbolKind.Record, node.BaseList);
        base.VisitRecordDeclaration(node);
    }

    private void ProcessTypeDeclaration(
        BaseTypeDeclarationSyntax node,
        string name,
        SymbolKind kind,
        BaseListSyntax? baseList)
    {
        var lineSpan = node.GetLocation().GetLineSpan();
        var fullId = string.IsNullOrEmpty(_currentNamespace) ? name : $"{_currentNamespace}.{name}";
        var prevType = _currentType;
        _currentType = fullId;

        var deps = new List<string>();
        if (baseList != null)
        {
            foreach (var baseType in baseList.Types)
            {
                deps.Add(baseType.Type.ToString());
            }
        }

        var keyword = kind.ToString().ToLowerInvariant();
        var symbol = new CodeSymbol(
            Id: fullId,
            Name: name,
            Kind: kind,
            FilePath: _filePath,
            StartLine: lineSpan.StartLinePosition.Line + 1,
            EndLine: lineSpan.EndLinePosition.Line + 1,
            Signature: $"{node.Modifiers} {keyword} {name}".Trim(),
            Dependencies: deps
        );

        _symbols.Add(symbol);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var lineSpan = node.GetLocation().GetLineSpan();
        var name = node.Identifier.Text;
        var fullId = $"{_currentType}.{name}({string.Join(", ", node.ParameterList.Parameters.Select(p => p.Type?.ToString()))})";

        var returnType = node.ReturnType.ToString();
        var deps = new List<string>();
        if (!string.Equals(returnType, "void", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(returnType, "Task", StringComparison.OrdinalIgnoreCase))
        {
            deps.Add(returnType);
        }

        foreach (var p in node.ParameterList.Parameters)
        {
            if (p.Type != null)
            {
                deps.Add(p.Type.ToString());
            }
        }

        var symbol = new CodeSymbol(
            Id: fullId,
            Name: name,
            Kind: SymbolKind.Method,
            FilePath: _filePath,
            StartLine: lineSpan.StartLinePosition.Line + 1,
            EndLine: lineSpan.EndLinePosition.Line + 1,
            Signature: $"{node.Modifiers} {node.ReturnType} {name}{node.ParameterList}",
            Dependencies: deps
        );

        _symbols.Add(symbol);
        base.VisitMethodDeclaration(node);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var lineSpan = node.GetLocation().GetLineSpan();
        var name = node.Identifier.Text;
        var fullId = $"{_currentType}.{name}";

        var deps = new List<string>();
        if (node.Type != null)
        {
            deps.Add(node.Type.ToString());
        }

        var symbol = new CodeSymbol(
            Id: fullId,
            Name: name,
            Kind: SymbolKind.Property,
            FilePath: _filePath,
            StartLine: lineSpan.StartLinePosition.Line + 1,
            EndLine: lineSpan.EndLinePosition.Line + 1,
            Signature: $"{node.Modifiers} {node.Type} {name} {{ get; set; }}",
            Dependencies: deps
        );

        _symbols.Add(symbol);
        base.VisitPropertyDeclaration(node);
    }
}
