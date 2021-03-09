using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modular.Interfaces;

//TODO: MOVE this file into the more generic, non-application specific project (i.e. METSv2.Core.EntityFramework)
namespace Modular.EfCore
{
  public static class DbContextExtensions
  {
    private static readonly string ENTITY_AUTOAUDIT_EXCLUDE_ANNOTATION = "custom:audit-exclude";
    private static readonly string MODEL_AUTOAUDIT_ANNOTATION = "custom:audit";
    private static readonly string AUTOAUDIT_COLUMNNAME = "UpdatedBy";
    //TODO: implement this same concept for a CreatedBy Column
    //TODO: implement this same concept for a DeletedBy Column
    //TODO: it would be cool to configure the audit column as a FK using a builder.HasOne<???>().WithMany(???)

    #region AutoAudit
    public static ModelBuilder ApplyAuditProperties(this ModelBuilder modelBuilder, string AuditUserColumnName = null)
    {
      //get entities that have not been explicitly excluded configured to be included from auditing
      var IncludeInAudit = modelBuilder.Model.GetEntityTypes().Where(e => !e.IsAbstract() && !e.IsOwned() && e.FindPrimaryKey() is IMutableKey && e.FindAnnotation(ENTITY_AUTOAUDIT_EXCLUDE_ANNOTATION) is null);

      foreach (var item in IncludeInAudit)
      {
        modelBuilder.Entity(item.ClrType, b =>
        {
          b.AutoAudit(AuditUserColumnName);
        });
      }
      return modelBuilder;
    }

    //TODO: allow Func<IIdentity> Identity to be DI-Service configurable 
    //TODO: allow audit column's type to be configurable so that it can be a different type - like Guid, and can be configured to be a FK
    public static void AutoAuditEntities<TDbContext>(this TDbContext context, Func<IIdentity> Identity) where TDbContext : DbContext
    {
      context.AutoAuditEntities(Identity().Name);
    }

    public static void AutoAuditEntities<TDbContext>(this TDbContext context, Func<string> Identity) where TDbContext : DbContext
    {
      context.AutoAuditEntities(Identity());
    }

    public static void AutoAuditEntities<TDbContext>(this TDbContext context, string Identity) where TDbContext : DbContext
    {
      try
      {
        if (!string.IsNullOrWhiteSpace(Identity))
        {
          var modified = context.ChangeTracker.Entries()
            .Where(x => x.IsAutoAudited())
            .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified || x.State == EntityState.Deleted);

          foreach (var item in modified)
          {
            if (item.Entity is IAuditable entity)
            {
              item.CurrentValues[nameof(IAuditable.UpdatedBy)] = Identity.Trim();
            }
            else if (item.Properties.Any(p => p.Metadata.Name == nameof(IAuditable.UpdatedBy)) && item.Property(nameof(IAuditable.UpdatedBy)) is PropertyEntry)
            {
              item.CurrentValues[nameof(IAuditable.UpdatedBy)] = Identity.Trim();
            }
          }
        }
      }
      catch
      {
        //TODO: Add Logging that autoAudit failed
      }
    }

    public static void DoNotAutoAudit<TEntity>(this EntityTypeBuilder<TEntity> entityBuilder) where TEntity : class
    {
      if (entityBuilder.Metadata.FindAnnotation(MODEL_AUTOAUDIT_ANNOTATION) is IAnnotation)
      {
        string AuditUserColumnName = (entityBuilder.Metadata.FindAnnotation(MODEL_AUTOAUDIT_ANNOTATION).Value as string).Trim();
        entityBuilder.Metadata.RemoveAnnotation(MODEL_AUTOAUDIT_ANNOTATION);
        entityBuilder.Metadata.RemoveProperty(AuditUserColumnName);
      }
      entityBuilder.HasAnnotation(ENTITY_AUTOAUDIT_EXCLUDE_ANNOTATION, string.Empty);
    }

    public static void AutoAudit(this EntityTypeBuilder entityBuilder, string AuditUserColumnName = null)
    {
      if (entityBuilder.Metadata.FindAnnotation(ENTITY_AUTOAUDIT_EXCLUDE_ANNOTATION) is IAnnotation)
      {
        entityBuilder.Metadata.RemoveAnnotation(ENTITY_AUTOAUDIT_EXCLUDE_ANNOTATION);
      }

      if (string.IsNullOrWhiteSpace(AuditUserColumnName))
      {
        AuditUserColumnName = AUTOAUDIT_COLUMNNAME;
      }
      entityBuilder.HasAnnotation(MODEL_AUTOAUDIT_ANNOTATION, AuditUserColumnName);
      bool hasAuditColumn = typeof(IAuditable).IsAssignableFrom(entityBuilder.Metadata.ClrType) || entityBuilder.Metadata.FindProperty(AuditUserColumnName) is null;
      if (hasAuditColumn)
      {
        entityBuilder.Property<string>(nameof(IAuditable.UpdatedBy))
          .HasColumnName(AuditUserColumnName)
          .IsRequired()
          .HasDefaultValue("Unknown");
      }
    }

    public static bool IsAutoAudited(this IEntityType entityType)
    {
      return !entityType.IsAbstract() && !entityType.IsOwned() && entityType.FindPrimaryKey() is IMutableKey && entityType.FindAnnotation(MODEL_AUTOAUDIT_ANNOTATION) is IAnnotation && entityType.FindAnnotation(ENTITY_AUTOAUDIT_EXCLUDE_ANNOTATION) is null;
    }

    public static bool IsAutoAudited(this EntityEntry entityType)
    {
      return entityType.Metadata.DefiningEntityType.IsAutoAudited();
    }
    #endregion

    #region SoftDelete
    /*
     * 
      var entity = context.Entry(note);
      if (entity != null)
      {
        note.SoftDeleted = true;
        await context.SaveChangesAsync();
      }

      // per this bug: https://GitLab.bcbsks.com/sales-and-marketing/metsv2/-/merge_requests/215#note_6651
      // we need to bust the cache on the lead and repull
      var lead = await Repository.FindAsync(leadState.Lead.ID);
      context.Entry(lead).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
     */
    public static void SoftDeleteEntities<TDbContext>(this TDbContext context, string Identity) where TDbContext : DbContext
    {
      throw new NotImplementedException("SoftDelete should be implemented similar to AutoAudit.");
      //See: https://www.thereformedprogrammer.net/ef-core-in-depth-soft-deleting-data-with-global-query-filters/
      //or, maybe just replace with https://github.com/JonPSmith/EfCore.SoftDeleteServices (library by the same guy)
    }
    #endregion

    #region IgnoreAllProperties
    private static EntityTypeBuilder IgnoreAllPropertiesCommon(this EntityTypeBuilder builder, bool ignoreInherited = false)
    {
      var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
      if (!ignoreInherited)
      {
        flags |= BindingFlags.DeclaredOnly;
      }
      var props = builder.Metadata.ClrType.GetProperties(flags);
      props./*Where(p => builder.Metadata.GetProperty(p.Name) is null).*/ToList().ForEach(p => builder.Ignore(p.Name));

      return builder;
    }

    public static EntityTypeBuilder IgnoreAllProperties(this EntityTypeBuilder builder, bool ignoreInherited = false)
    {
      builder.IgnoreAllPropertiesCommon(ignoreInherited);
      return builder;
    }

    public static EntityTypeBuilder<TEntity> IgnoreAllProperties<TEntity>(this EntityTypeBuilder<TEntity> builder, bool ignoreInherited = false) where TEntity : class
    {
      builder.IgnoreAllPropertiesCommon(ignoreInherited);
      return builder;
    }

    //TODO: 
    public static OwnedNavigationBuilder<TEntity, TDependentEntity> IgnoreAllProperties<TEntity, TDependentEntity>(this OwnedNavigationBuilder<TEntity, TDependentEntity> builder, bool ignoreInherited = false) where TEntity : class where TDependentEntity : class
    {
      var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
      if (!ignoreInherited)
      {
        flags |= BindingFlags.DeclaredOnly;
      }
      var props = builder.OwnedEntityType.ClrType.GetProperties(flags);
      props./*Where(p => builder.Metadata.GetProperty(p.Name) is null).*/ToList().ForEach(p => builder.Ignore(p.Name));
      return builder;
    }
    #endregion
  }
}
