namespace ReconNessAgent.Domain.Core.Entities;

public partial class Reference : BaseEntity
{
    public Guid Id { get; set; }
    public string? Url { get; set; }
    public string? Categories { get; set; }
}
