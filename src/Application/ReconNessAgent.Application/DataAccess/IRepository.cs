using System.Linq.Expressions;

namespace ReconNessAgent.Application.DataAccess;

/// <summary>
/// The interface IRepository<TEntity> patter
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Obtain the async list of Entities
    /// </summary>
    /// <param name="cancellationToken">Notification that operations should be canceled</param>
    /// <returns>Async list of Entities</returns>
    Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtain the async queryable of generic Entities
    /// </summary>
    /// <returns>The async queryable of generic Entities</returns>
    IQueryable<TEntity> GetAllQueryable();

    /// <summary>
    /// Obtain the async list of Entities
    /// </summary>
    /// <param name="predicate">The criteria</param>
    /// <param name="cancellationToken">Notification that operations should be canceled</param>
    /// <returns>The async list of generic Entities by criteria</returns>
    Task<List<TEntity>> GetAllByCriteriaAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtain if exist some data on BD using that predicate
    /// </summary>
    /// <param name="predicate">The criteria</param>
    /// <param name="cancellationToken">Notification that operations should be canceled</param>
    /// <returns>If exist some data on BD using that predicate</returns>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtain the async queryable of generic Entities by criteria
    /// </summary>
    /// <param name="predicate">The criteria</param>
    /// <returns>The async queryable of generic Entities by criteria</returns>
    IQueryable<TEntity> GetAllQueryableByCriteria(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Obtain the async list of Entities by criteria
    /// </summary>
    /// <param name="predicate">The criteria</param>
    /// <param name="cancellationToken">Notification that operations should be canceled</param>
    /// <returns>Async list of Entities by criteria</returns>
    Task<TEntity?> GetByCriteriaAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtain the entity by Id
    /// </summary>
    /// <param name="id">The entity Id</param>
    /// <param name="cancellationToken">Notification that operations should be canceled</param>
    /// <returns>An Entity</returns>
    Task<TEntity?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtain the entity by Id
    /// </summary>
    /// <param name="predicate">The criteria</param>
    /// <param name="cancellationToken">Notification that operations should be canceled</param>
    /// <returns>An Entity</returns>
    Task<TEntity?> FindByCriteriaAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new Entity into the context
    /// </summary>
    /// <param name="entity">The new Entity</param>
    void Add(TEntity entity);

    /// <summary>
    ///  Add a list of Entities into the context
    /// </summary>
    /// <param name="entities">List of new Entities</param>
    void AddRange(List<TEntity> entities);

    /// <summary>
    /// Update an Entity
    /// </summary>
    /// <param name="entity">The Entity</param>
    void Update(TEntity entity);

    /// <summary>
    /// Update a List of Entities
    /// </summary>
    /// <param name="entities">The entities to update</param>
    void UpdateRange(List<TEntity> entities);

    /// <summary>
    /// Delete an Entity
    /// </summary>
    /// <param name="entity">The Entity to delete</param>
    void Delete(TEntity entity);

    /// <summary>
    /// Delete a list of Entities
    /// </summary>
    /// <param name="entities">The entities to delete</param>
    void DeleteRange(List<TEntity> entities);
}
