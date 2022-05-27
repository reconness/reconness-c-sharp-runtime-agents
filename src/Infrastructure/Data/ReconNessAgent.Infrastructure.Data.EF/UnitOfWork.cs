using ReconNessAgent.Application.DataAccess;
using System.Collections;

namespace ReconNessAgent.Infrastructure.Data.EF;

public class UnitOfWork : IUnitOfWork
{
    /// <summary>
    /// The DataBase Context
    /// </summary>
    private readonly IDbContext context;

    /// <summary>
    /// A hash of repositories
    /// </summary>
    private Hashtable repositories;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork" /> class
    /// </summary>
    /// <param name="context">The implementation of Database Context <see cref="IDbContext" /></param>
    public UnitOfWork(IDbContext context)
    {
        this.context = context;
        this.repositories = new Hashtable();
    }

    /// <inheritdoc/>
    public IRepository<TEntity>? Repository<TEntity>(CancellationToken cancellationToken = default)
        where TEntity : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        var type = typeof(TEntity).Name;

        if (this.repositories.ContainsKey(type))
        {
            return repositories[type] as IRepository<TEntity>;
        }

        var repositoryType = typeof(Repository<TEntity>);

        this.repositories.Add(type, Activator.CreateInstance(repositoryType, this.context));

        return repositories[type] as IRepository<TEntity>;
    }

    /// <inheritdoc/>
    public void BeginTransaction(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        this.context.BeginTransaction(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return this.context.CommitAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void Rollback(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        this.context.Rollback(cancellationToken);
    }
}
