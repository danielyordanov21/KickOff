public class ProjectCatalogueDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Owner { get; set; }
    public required string State { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? FinancialGoal { get; set; }
    public decimal? FinancialRaised { get; set; }
    public DateTime? EndDate { get; set; }
}
