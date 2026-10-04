using RepoAgentMemory.Core;

Console.WriteLine("===============================================================================");
Console.WriteLine(" RepoAgentMemory: Roslyn AST Symbol Indexing & Hierarchical Context Compactor");
Console.WriteLine(" Backing Architecture for Autonomous Coding Agent Knowledge Retrieval");
Console.WriteLine("===============================================================================\n");

const string sampleSource = """
namespace EnterprisePlatform.Payments;

public interface IPaymentGateway
{
    Task<string> AuthorizePaymentAsync(string customerId, decimal amount);
    Task<bool> RefundAsync(string transactionId);
}

public sealed class StripePaymentGateway : IPaymentGateway
{
    public string ApiKey { get; set; }

    public async Task<string> AuthorizePaymentAsync(string customerId, decimal amount)
    {
        // 50 lines of complex network and validation logic omitted in compaction
        await Task.Delay(10);
        return $"txn_{Guid.NewGuid():N}";
    }

    public async Task<bool> RefundAsync(string transactionId)
    {
        // Complex refund retry logic omitted in compaction
        await Task.Delay(10);
        return true;
    }
}
""";

Console.WriteLine("Extracting C# Roslyn AST Symbols...");
var symbols = RoslynSymbolExtractor.ExtractFromSource(sampleSource, "StripePaymentGateway.cs");

var graph = new SymbolGraph();
foreach (var s in symbols)
{
    graph.AddSymbol(s);
}

Console.WriteLine($"Extracted {symbols.Count} symbols across AST hierarchy:");
foreach (var s in symbols)
{
    Console.WriteLine($" - [{s.Kind,-10}] {s.Name,-25} (Line {s.StartLine}-{s.EndLine}) Score: {graph.GetConnectivityScore(s.Id)}");
}

Console.WriteLine("\nExecuting Hierarchical Context Compaction for LLM Prompt Injection...");
var compacted = HierarchicalContextCompactor.Compact(graph, sampleSource.Length);

Console.WriteLine($"Original Tokens (est):  {compacted.OriginalEstimatedTokens}");
Console.WriteLine($"Compacted Tokens (est): {compacted.CompactedEstimatedTokens}");
Console.WriteLine($"Token Space Savings:    {compacted.ReductionPercentage:F1}%\n");

Console.WriteLine("Compacted Context Output:");
Console.WriteLine("-------------------------------------------------------------------------------");
Console.WriteLine(compacted.SummaryOutline);
Console.WriteLine("-------------------------------------------------------------------------------");
