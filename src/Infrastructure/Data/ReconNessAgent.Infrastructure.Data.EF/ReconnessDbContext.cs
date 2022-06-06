using Microsoft.EntityFrameworkCore;
using ReconNessAgent.Application.DataAccess;
using ReconNessAgent.Domain.Core.Entities;
using System.Linq.Expressions;

namespace ReconNessAgent.Infrastructure.Data.EF
{
    public partial class ReconnessDbContext : DbContext, IDbContext
    {
        public ReconnessDbContext()
        {
        }

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
        public virtual DbSet<AspNetRole> AspNetRoles { get; set; } = null!;
        public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; } = null!;
        public virtual DbSet<AspNetUser> AspNetUsers { get; set; } = null!;
        public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; } = null!;
        public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; } = null!;
        public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; } = null!;
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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseNpgsql("Host=localhost;Database=reconness;Username=postgres;Password=postgres;Persist Security Info=True");
            }
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

            modelBuilder.Entity<AspNetRole>(entity =>
            {
                entity.HasIndex(e => e.NormalizedName, "RoleNameIndex")
                    .IsUnique();

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Name).HasMaxLength(256);

                entity.Property(e => e.NormalizedName).HasMaxLength(256);
            });

            modelBuilder.Entity<AspNetRoleClaim>(entity =>
            {
                entity.HasIndex(e => e.RoleId, "IX_AspNetRoleClaims_RoleId");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.AspNetRoleClaims)
                    .HasForeignKey(d => d.RoleId);
            });

            modelBuilder.Entity<AspNetUser>(entity =>
            {
                entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

                entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex")
                    .IsUnique();

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Email).HasMaxLength(256);

                entity.Property(e => e.NormalizedEmail).HasMaxLength(256);

                entity.Property(e => e.NormalizedUserName).HasMaxLength(256);

                entity.Property(e => e.UserName).HasMaxLength(256);

                entity.HasMany(d => d.Roles)
                    .WithMany(p => p.Users)
                    .UsingEntity<Dictionary<string, object>>(
                        "AspNetUserRole",
                        l => l.HasOne<AspNetRole>().WithMany().HasForeignKey("RoleId"),
                        r => r.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                        j =>
                        {
                            j.HasKey("UserId", "RoleId");

                            j.ToTable("AspNetUserRoles");

                            j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                        });
            });

            modelBuilder.Entity<AspNetUserClaim>(entity =>
            {
                entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserClaims)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUserLogin>(entity =>
            {
                entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

                entity.HasIndex(e => e.UserId, "IX_AspNetUserLogins_UserId");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserLogins)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUserToken>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserTokens)
                    .HasForeignKey(d => d.UserId);
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
