using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using ReconNessAgent.Application.DataAccess;
using ReconNessAgent.Domain.Core.Entities;
using System.Linq.Expressions;

namespace ReconNessAgent.Infrastructure.Data.EF
{
    public partial class ReconnessDbContext : DbContext, IDbContext
    {
        public ReconnessDbContext(DbContextOptions<ReconnessDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Agent> Agents { get; set; } = null!;
        public virtual DbSet<AgentRunner> AgentRunners { get; set; } = null!;
        public virtual DbSet<AgentRunnerCommand> AgentRunnerCommands { get; set; } = null!;
        public virtual DbSet<AgentRunnerCommandOutput> AgentRunnerCommandOutputs { get; set; } = null!;
        public virtual DbSet<AgentTrigger> AgentTriggers { get; set; } = null!;
        public virtual DbSet<AgentsSetting> AgentsSettings { get; set; } = null!;
        public virtual DbSet<Category> Categories { get; set; } = null!;
        public virtual DbSet<Domain.Core.Entities.Directory> Directories { get; set; } = null!;
        public virtual DbSet<EventTrack> EventTracks { get; set; } = null!;
        public virtual DbSet<Label> Labels { get; set; } = null!;
        public virtual DbSet<Note> Notes { get; set; } = null!;
        public virtual DbSet<Notification> Notifications { get; set; } = null!;
        public virtual DbSet<Reference> References { get; set; } = null!;
        public virtual DbSet<RootDomain> RootDomains { get; set; } = null!;
        public virtual DbSet<Service> Services { get; set; } = null!;
        public virtual DbSet<Subdomain> Subdomains { get; set; } = null!;
        public virtual DbSet<Target> Targets { get; set; } = null!;

        #region IDbContext

        /// <summary>
        /// A transaction Object
        /// </summary>
        private IDbContextTransaction transaction;

        /// <inheritdoc/>
        public void BeginTransaction(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (transaction != null)
            {
                var dbTransaction = transaction.GetDbTransaction();
                try
                {
                    if (dbTransaction != null && dbTransaction?.Connection != null && dbTransaction?.Connection?.State == System.Data.ConnectionState.Open)
                    {
                        return;
                    }
                }
                catch (Exception)
                {

                }
            }

            this.transaction = this.Database.BeginTransaction();
        }

        /// <inheritdoc/>
        public int Commit(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                this.BeginTransaction(cancellationToken);
                var saveChanges = this.SaveChanges();
                this.EndTransaction(cancellationToken);

                return saveChanges;
            }
            catch (Exception)
            {
                this.Rollback(cancellationToken);
                throw;
            }
            finally
            {
                // base.Dispose();
            }
        }

        /// <inheritdoc/>
        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                this.BeginTransaction(cancellationToken);
                var saveChangesAsync = await this.SaveChangesAsync(cancellationToken);
                this.EndTransaction(cancellationToken);

                return saveChangesAsync;
            }
            catch (Exception)
            {
                this.Rollback(cancellationToken);
                throw;
            }
            finally
            {
                // base.Dispose();
            }
        }

        /// <inheritdoc/>
        public void Rollback(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (this.transaction != null && this.transaction.GetDbTransaction().Connection != null)
            {
                this.transaction.Rollback();
            }
        }

        /// <inheritdoc/>
        private void EndTransaction(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            this.transaction.Commit();
        }

        /// <inheritdoc/>
        public void SetAsAdded<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            this.UpdateEntityState<TEntity>(entity, EntityState.Added, cancellationToken);
        }

        /// <inheritdoc/>
        public void SetAsAdded<TEntity>(List<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            entities.ForEach(entity => this.SetAsAdded<TEntity>(entity, cancellationToken));
        }

        /// <inheritdoc/>
        public void SetAsModified<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            this.UpdateEntityState<TEntity>(entity, EntityState.Modified, cancellationToken);
        }

        /// <inheritdoc/>
        public void SetAsModified<TEntity>(List<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            entities.ForEach(entity => this.SetAsModified<TEntity>(entity, cancellationToken));
        }

        /// <inheritdoc/>
        public void SetAsDeleted<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            this.UpdateEntityState<TEntity>(entity, EntityState.Deleted, cancellationToken);
        }

        /// <inheritdoc/>
        public void SetAsDeleted<TEntity>(List<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            entities.ForEach(entity => this.SetAsDeleted<TEntity>(entity, cancellationToken));
        }

        /// <inheritdoc/>
        public Task<TEntity> FindAsync<TEntity>(Guid id, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            return this.Set<TEntity>().FindAsync(id, cancellationToken).AsTask();
        }

        /// <inheritdoc/>
        public Task<TEntity> FindByCriteriaAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            return this.Set<TEntity>().Local.AsQueryable().FirstOrDefaultAsync(predicate, cancellationToken) ?? this.FirstOrDefaultAsync<TEntity>(predicate, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<TEntity> FirstOrDefaultAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            return this.Set<TEntity>().FirstOrDefaultAsync(predicate, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<List<TEntity>> ToListAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            return this.Set<TEntity>().ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<List<TEntity>> ToListByCriteriaAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            return this.Set<TEntity>().Where(predicate).ToListAsync<TEntity>(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<bool> AnyAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            return this.Set<TEntity>().AnyAsync(predicate, cancellationToken);
        }

        /// <inheritdoc/>
        public IQueryable<TEntity> ToQueryable<TEntity>() where TEntity : class
        {
            return this.Set<TEntity>().AsQueryable();
        }

        /// <inheritdoc/>
        public IQueryable<TEntity> ToQueryableByCriteria<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            return this.Set<TEntity>().Where(predicate).AsQueryable();
        }

        /// <summary>
        /// Update entity state
        /// </summary>
        private void UpdateEntityState<TEntity>(TEntity entity, EntityState entityState, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entityEntry = this.GetDbEntityEntrySafely<TEntity>(entity, cancellationToken);
            if (entityEntry.State == EntityState.Unchanged)
            {
                entityEntry.State = entityState;
            }
        }

        /// <summary>
        /// Attach entity
        /// </summary>
        private EntityEntry GetDbEntityEntrySafely<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entityEntry = Entry<TEntity>(entity);
            if (entityEntry.State == EntityState.Detached)
            {
                this.Set<TEntity>().Attach(entity);
            }

            return entityEntry;
        }

        #endregion IDbContext

        /// <inheritdoc/>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var changes = ChangeTracker.Entries<BaseEntity>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var item in changes)
            {
                item.Property(p => p.UpdatedAt).CurrentValue = DateTime.UtcNow;

                if (item.State == EntityState.Added)
                {
                    item.Property(p => p.CreatedAt).CurrentValue = DateTime.UtcNow;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public override int SaveChanges()
        {
            var changes = ChangeTracker.Entries<BaseEntity>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var item in changes)
            {
                item.Property(p => p.UpdatedAt).CurrentValue = DateTime.UtcNow;
                if (item.State == EntityState.Added)
                {
                    item.Property(p => p.CreatedAt).CurrentValue = DateTime.UtcNow;
                }
            }

            return base.SaveChanges();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Agent>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasMany(d => d.Categories)
                    .WithMany(p => p.Agents)
                    .UsingEntity<Dictionary<string, object>>(
                        "AgentCategory",
                        l => l.HasOne<Category>().WithMany().HasForeignKey("CategoriesId"),
                        r => r.HasOne<Agent>().WithMany().HasForeignKey("AgentsId"),
                        j =>
                        {
                            j.HasKey("AgentsId", "CategoriesId");

                            j.ToTable("AgentCategory");

                            j.HasIndex(new[] { "CategoriesId" }, "IX_AgentCategory_CategoriesId");
                        });
            });

            modelBuilder.Entity<AgentRunner>(entity =>
            {
                entity.HasIndex(e => e.AgentId, "IX_AgentRunners_AgentId");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.Agent)
                    .WithMany(p => p.AgentRunners)
                    .HasForeignKey(d => d.AgentId);
            });

            modelBuilder.Entity<AgentRunnerCommand>(entity =>
            {
                entity.HasIndex(e => e.AgentRunnerId, "IX_AgentRunnerCommands_AgentRunnerId");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.AgentRunner)
                    .WithMany(p => p.AgentRunnerCommands)
                    .HasForeignKey(d => d.AgentRunnerId);
            });

            modelBuilder.Entity<AgentRunnerCommandOutput>(entity =>
            {
                entity.HasIndex(e => e.AgentRunnerCommandId, "IX_AgentRunnerCommandOutputs_AgentRunnerCommandId");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.AgentRunnerCommand)
                    .WithMany(p => p.AgentRunnerCommandOutputs)
                    .HasForeignKey(d => d.AgentRunnerCommandId)
                    .HasConstraintName("FK_AgentRunnerCommandOutputs_AgentRunnerCommands_AgentRunnerCo~");
            });

            modelBuilder.Entity<AgentTrigger>(entity =>
            {
                entity.HasIndex(e => e.AgentId, "IX_AgentTriggers_AgentId")
                    .IsUnique();

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.SubdomainIncExcIp).HasColumnName("SubdomainIncExcIP");

                entity.Property(e => e.SubdomainIp).HasColumnName("SubdomainIP");

                entity.HasOne(d => d.Agent)
                    .WithOne(p => p.AgentTrigger)
                    .HasForeignKey<AgentTrigger>(d => d.AgentId);
            });

            modelBuilder.Entity<AgentsSetting>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<Domain.Core.Entities.Directory>(entity =>
            {
                entity.HasIndex(e => e.SubdomainId, "IX_Directories_SubdomainId");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.Subdomain)
                    .WithMany(p => p.Directories)
                    .HasForeignKey(d => d.SubdomainId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<EventTrack>(entity =>
            {
                entity.HasIndex(e => e.AgentId, "IX_EventTracks_AgentId");

                entity.HasIndex(e => e.RootDomainId, "IX_EventTracks_RootDomainId");

                entity.HasIndex(e => e.SubdomainId, "IX_EventTracks_SubdomainId");

                entity.HasIndex(e => e.TargetId, "IX_EventTracks_TargetId");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.Agent)
                    .WithMany(p => p.EventTracks)
                    .HasForeignKey(d => d.AgentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.RootDomain)
                    .WithMany(p => p.EventTracks)
                    .HasForeignKey(d => d.RootDomainId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.Subdomain)
                    .WithMany(p => p.EventTracks)
                    .HasForeignKey(d => d.SubdomainId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.Target)
                    .WithMany(p => p.EventTracks)
                    .HasForeignKey(d => d.TargetId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Label>(entity =>
            {
                entity.ToTable("Label");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasMany(d => d.Subdomains)
                    .WithMany(p => p.Labels)
                    .UsingEntity<Dictionary<string, object>>(
                        "LabelSubdomain",
                        l => l.HasOne<Subdomain>().WithMany().HasForeignKey("SubdomainsId"),
                        r => r.HasOne<Label>().WithMany().HasForeignKey("LabelsId"),
                        j =>
                        {
                            j.HasKey("LabelsId", "SubdomainsId");

                            j.ToTable("LabelSubdomain");

                            j.HasIndex(new[] { "SubdomainsId" }, "IX_LabelSubdomain_SubdomainsId");
                        });
            });

            modelBuilder.Entity<Note>(entity =>
            {
                entity.ToTable("Note");

                entity.HasIndex(e => e.RootDomainId, "IX_Note_RootDomainId");

                entity.HasIndex(e => e.SubdomainId, "IX_Note_SubdomainId");

                entity.HasIndex(e => e.TargetId, "IX_Note_TargetId");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.RootDomain)
                    .WithMany(p => p.Notes)
                    .HasForeignKey(d => d.RootDomainId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Subdomain)
                    .WithMany(p => p.Notes)
                    .HasForeignKey(d => d.SubdomainId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Target)
                    .WithMany(p => p.Notes)
                    .HasForeignKey(d => d.TargetId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<Reference>(entity =>
            {
                entity.ToTable("Reference");

                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<RootDomain>(entity =>
            {
                entity.HasIndex(e => e.TargetId, "IX_RootDomains_TargetId");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.Target)
                    .WithMany(p => p.RootDomains)
                    .HasForeignKey(d => d.TargetId);
            });

            modelBuilder.Entity<Service>(entity =>
            {
                entity.HasIndex(e => e.SubdomainId, "IX_Services_SubdomainId");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.Subdomain)
                    .WithMany(p => p.Services)
                    .HasForeignKey(d => d.SubdomainId);
            });

            modelBuilder.Entity<Subdomain>(entity =>
            {
                entity.HasIndex(e => e.RootDomainId, "IX_Subdomains_RootDomainId");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.RootDomain)
                    .WithMany(p => p.Subdomains)
                    .HasForeignKey(d => d.RootDomainId);
            });

            modelBuilder.Entity<Target>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
