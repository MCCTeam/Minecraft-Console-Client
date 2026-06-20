namespace DebugTools.MccMcpWebPlayground.Harness;

public sealed class MccContextCompressor
{
    public void CompactIfNeeded(MccRunState runState)
    {
        if (runState.Evidence.Count <= 6)
            return;

        IReadOnlyList<MccEvidenceRecord> olderEvidence = runState.Evidence
            .Take(Math.Max(0, runState.Evidence.Count - 6))
            .ToArray();

        if (olderEvidence.Count == 0)
            return;

        runState.CompactionSummary = string.Join('\n', olderEvidence
            .TakeLast(8)
            .Select(record => $"- {record.Id} {record.ToolName}: {record.Summary}"));
    }
}
