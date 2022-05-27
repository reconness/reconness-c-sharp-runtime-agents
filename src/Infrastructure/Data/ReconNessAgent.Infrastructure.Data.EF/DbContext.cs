using System.Linq.Expressions;

namespace ReconNessAgent.Infrastructure.Data.EF;

public class DbContext : IDbContext
{
    public Task<bool> AnyAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class
    {
        throw new NotImplementedException();
    }

    public void BeginTransaction(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<TEntity> FindAsync<TEntity>(Guid id, CancellationToken cancellationToken = default) where TEntity : class
    {
        throw new NotImplementedException();
    }

    public Task<TEntity> FindByCriteriaAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class
    {
        throw new NotImplementedException();
    }

    public Task<TEntity> FirstOrDefaultAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class
    {
        throw new NotImplementedException();
    }

    public void Rollback(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void SetAsAdded<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
    {
        throw new NotImplementedException();
    }

    public void SetAsAdded<TEntity>(List<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class
    {
        throw new NotImplementedException();
    }

    public void SetAsDeleted<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
    {
        throw new NotImplementedException();
    }

    public void SetAsDeleted<TEntity>(List<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class
    {
        throw new NotImplementedException();
    }

    public void SetAsModified<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
    {
        throw new NotImplementedException();
    }

    public void SetAsModified<TEntity>(List<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class
    {
        throw new NotImplementedException();
    }

    public Task<List<TEntity>> ToListAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
    {
        throw new NotImplementedException();
    }

    public Task<List<TEntity>> ToListByCriteriaAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class
    {
        throw new NotImplementedException();
    }

    public IQueryable<TEntity> ToQueryable<TEntity>() where TEntity : class
    {
        throw new NotImplementedException();
    }

    public IQueryable<TEntity> ToQueryableByCriteria<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
    {
        throw new NotImplementedException();
    }
}
