namespace ReconNessAgent.Domain.Core;

public class BaseEntity
{
    /// <summary>
    /// 
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool Deleted { get; set; }
}
