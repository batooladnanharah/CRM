using CRM.Api.Tickets;

namespace CRM.Api.Reports;

public record TicketVolumeResponse(int Total, int Open, int Resolved);

public record StatusCountResponse(TicketStatus Status, int Count);

public record AgentPerformanceResponse(Guid AgentId, string DisplayName, int TicketCount);

public record SlaPerformanceResponse(
    int TotalEvaluated,
    int WithinSla,
    int AtRisk,
    int Breached,
    int WithinSlaPercent,
    int AtRiskPercent,
    int BreachedPercent);

public record ResolutionMetricsResponse(int ResolvedTicketCount, double? AverageResolutionMinutes);

public record ReportsSummaryResponse(
    TicketVolumeResponse TicketVolume,
    IReadOnlyList<StatusCountResponse> StatusDistribution,
    IReadOnlyList<AgentPerformanceResponse> AgentPerformance,
    SlaPerformanceResponse SlaPerformance,
    ResolutionMetricsResponse Resolution);
