using RepoAgentMemory.Core;
using Xunit;

namespace RepoAgentMemory.Tests;

public class SymbolExtractionTests
{
    private const string SampleCode = """
    namespace SampleApp.Services;

    public interface IOrderService
    {
        string GetStatus(int orderId);
    }

    public class OrderService : IOrderService
    {
        public string ConnectionString { get; set; }

        public string GetStatus(int orderId)
        {
            return "Active";
        }
    }
    """;

    [Fact]
    public void RoslynSymbolExtractor_ShouldExtractInterfacesAndClasses()
    {
        var symbols = RoslynSymbolExtractor.ExtractFromSource(SampleCode, "OrderService.cs");

        Assert.NotEmpty(symbols);
        Assert.Contains(symbols, s => s.Kind == SymbolKind.Interface && s.Name == "IOrderService");
        Assert.Contains(symbols, s => s.Kind == SymbolKind.Class && s.Name == "OrderService");
        Assert.Contains(symbols, s => s.Kind == SymbolKind.Property && s.Name == "ConnectionString");
        Assert.Contains(symbols, s => s.Kind == SymbolKind.Method && s.Name == "GetStatus");
    }

    [Fact]
    public void SymbolGraph_ShouldComputeConnectivityAndDependents()
    {
        var symbols = RoslynSymbolExtractor.ExtractFromSource(SampleCode, "OrderService.cs");
        var graph = new SymbolGraph();
        foreach (var s in symbols)
        {
            graph.AddSymbol(s);
        }

        var orderServiceClass = symbols.First(s => s.Kind == SymbolKind.Class);
        Assert.Contains("IOrderService", orderServiceClass.Dependencies);

        var interfaceDependents = graph.GetDependents("IOrderService");
        Assert.Contains(orderServiceClass.Id, interfaceDependents);
    }

    [Fact]
    public void HierarchicalContextCompactor_ShouldSignificantlyCompressTokenBudget()
    {
        var symbols = RoslynSymbolExtractor.ExtractFromSource(SampleCode, "OrderService.cs");
        var graph = new SymbolGraph();
        foreach (var s in symbols)
        {
            graph.AddSymbol(s);
        }

        var compacted = HierarchicalContextCompactor.Compact(graph, SampleCode.Length);

        Assert.NotNull(compacted.SummaryOutline);
        Assert.Contains("interface IOrderService", compacted.SummaryOutline);
        Assert.Contains("class OrderService", compacted.SummaryOutline);
        Assert.Contains("ConnectionString", compacted.SummaryOutline);
        Assert.True(compacted.CompactedEstimatedTokens > 0);
    }
}
