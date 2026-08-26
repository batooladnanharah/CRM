namespace CRM.Api.Reports;

public static class ReportsEndpoints
{
    public static void MapReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var reports = app.MapGroup("/api/reports").RequireAuthorization("AdminOnly").WithTags("Reports");

        reports.MapGet("/summary", async (ReportsService service, CancellationToken ct) =>
            Results.Ok(await service.GetSummaryAsync(ct)))
            .WithName("GetReportsSummary")
            .Produces<ReportsSummaryResponse>();
    }
}
