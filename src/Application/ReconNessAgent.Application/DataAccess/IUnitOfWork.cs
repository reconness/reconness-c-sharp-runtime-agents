namespace ReconNessAgent.Application.DataAccess;

/// <summary>
/// This interface define a Unit of Work patter
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Obtain a generic repository
    /// </summary>
    /// <returns>A generic repository</returns>
    IRepository<TEntity> Repository<TEntity>() where TEntity : class;

    /// <summary>
    /// Begin a context transaction
    /// </summary>
    void BeginTransaction();

    /// <summary>
    /// Do the context commit async
    /// </summary>
    /// <param name="cancellationToken">Notification that operations should be canceled</param>
    /// <returns>An identifier value</returns>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback the context transaction
    /// </summary>
    void Rollback();
}
