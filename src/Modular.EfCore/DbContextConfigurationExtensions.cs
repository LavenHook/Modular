using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Modular.EfCore
{
  public static class EfExtensions
  {

    #region extensions to inject ModelBuilder.ApplyConfiguration delegates


    //this will apply *ONLY* to TDbContext, and the DbContexts that inherit from TDbContext, if applyToInherited is set to 'true'
    public static IServiceCollection AddEfConfiguration<TDbContext, TEntity>(this IServiceCollection services, Func<IServiceProvider, IEntityTypeConfiguration<TEntity>> configurationImplementation, bool applyToInherrited = true) where TDbContext : DbContext where TEntity : class
    {
      services.AddTransient(p => new ApplyConfigurationToModelDelegateFactory<TDbContext, TEntity>(configurationImplementation(p), applyToInherrited));
      services.AddTransient<IApplyConfigurationToModel>(p => p.GetService<ApplyConfigurationToModelDelegateFactory<TDbContext, TEntity>>());
      return services;
    }
    //this will apply *ONLY* to TDbContext, and the DbContexts that inherit from TDbContext, if applyToInherited is set to 'true'
    public static IServiceCollection AddEfConfiguration<TDbContext, TEntity>(this IServiceCollection services, Func<TDbContext> DbContextTypeResolver, Func<IServiceProvider, IEntityTypeConfiguration<TEntity>> configurationImplementation, bool applyToInherrited = true) where TDbContext : DbContext where TEntity : class => AddEfConfiguration<TDbContext, TEntity>(services, configurationImplementation, applyToInherrited);

    //this will apply the configuration to *ALL* DBContexts that invoke 'ApplyConfigurations(...)'
    public static IServiceCollection AddEfConfiguration<TEntity>(this IServiceCollection services, Func<IServiceProvider, IEntityTypeConfiguration<TEntity>> configurationImplementation) where TEntity : class => AddEfConfiguration<DbContext, TEntity>(services, configurationImplementation, true);

    //this will apply the configuration to *ALL* DBContexts that invoke 'ApplyConfigurations(...)'
    public static IServiceCollection AddEfConfiguration<TEntity>(this IServiceCollection services, IEntityTypeConfiguration<TEntity> configurationInstance) where TEntity : class => AddEfConfiguration<DbContext, TEntity>(services, s => configurationInstance, true);

    internal interface IApplyConfigurationToModel
    {
      Func<ModelBuilder, ModelBuilder> ApplyConfigurationDelegate();
      bool ApplyToInherited { get; }
      Type DbContextType { get; }
    }

    internal sealed class ApplyConfigurationToModelDelegateFactory<TDbContext, TEntity> : IApplyConfigurationToModel where TDbContext : DbContext where TEntity : class
    {
      public IEntityTypeConfiguration<TEntity> ConfigurationInstance { get; }
      public bool ApplyToInherited { get; }

      public Type DbContextType => typeof(TDbContext);

      public ApplyConfigurationToModelDelegateFactory(IEntityTypeConfiguration<TEntity> configurationInstance, bool applyToInherrited = true)
      {
        ConfigurationInstance = configurationInstance;
        ApplyToInherited = applyToInherrited;
      }


      public Func<ModelBuilder, ModelBuilder> ApplyConfigurationDelegate()
      {
        return builder =>
        {
          //if (builder.Model.GetType() is TDbContext
          //|| (ApplyToInherited && typeof(TDbContext).IsAssignableFrom(builder.Model.GetType())))
          //{
          //  ;
          //}
          return builder.ApplyConfiguration(ConfigurationInstance);
        };
      }
    }

    public static void ApplyConfigurations<TDbContext>(this TDbContext context, ModelBuilder modelBuilder) where TDbContext : DbContext
    {
      //filter for TDbContext inheritance rules
      Func<IApplyConfigurationToModel, bool> whereFunc = a =>
      {
        //check if the IApplyBuilerToModel was intended for this specific TDbContext OR if inheritance is allowed && TDbContext inherits appropriately
        return a.DbContextType == typeof(TDbContext)
              || (a.ApplyToInherited && a.DbContextType.IsAssignableFrom(typeof(TDbContext)));
      };

      var factories = context.Database.GetInfrastructure().GetServices<IApplyConfigurationToModel>().Where(whereFunc);
      factories.Select(f => f.ApplyConfigurationDelegate()).ToList().ForEach(d => d(modelBuilder));
    }
    #endregion

    #region extensions to inject ModelBuilder Delegates
    //TODO: make a generalized version to allow builders to be applied to all dbcontexts without knowledge of type 
    public static IServiceCollection AddModelBuilderDelegate<TDbContext>(this IServiceCollection services, Func<IServiceProvider, TDbContext> DbContextTypeResolver, Action<IServiceProvider, ModelBuilder> modelBuilderImplementation, int priority = 0, bool applyToInherited = true) where TDbContext : DbContext
    {
      services.AddTransient<IApplyBuilderToModel>(p => new ApplyBuilderToModelDelegateFactory<TDbContext>() { ApplyBuilderDelegate = b => modelBuilderImplementation(p, b), Priority = priority, ApplyToInherited = applyToInherited });
      return services;
    }
    public static IServiceCollection AddModelBuilderDelegate<TDbContext>(this IServiceCollection services, Action<IServiceProvider, ModelBuilder> modelBuilderImplementation, int priority = 0, bool applyToInherited = true) where TDbContext : DbContext => AddModelBuilderDelegate(services, s => s.GetService<TDbContext>(), modelBuilderImplementation);

    public static IServiceCollection AddModelBuilderDelegate(this IServiceCollection services, Action<IServiceProvider, ModelBuilder> modelBuilderImplementation, int priority = 0, bool applyToInherited = true) => AddModelBuilderDelegate<DbContext>(services, modelBuilderImplementation);

    //Higher priority goes first
    public static ModelBuilder ApplyBuilders<TDbContext>(this TDbContext context, ModelBuilder builder, int minPriority = int.MinValue, int maxPriority = int.MaxValue) where TDbContext : DbContext
    {
      //registered ex: ApplyBuilderToModelDelegateFactory<DbContectBase>
      //  and InheritedDbContext : DbContextBase
      //  -> InheritedDbContext.ApplyBuilders(...) 


      var factories = context.Database.GetInfrastructure().GetServices<IApplyBuilderToModel>();
      //set the priority filters
      if(minPriority > maxPriority)
      {
        int tmp = minPriority;
        minPriority = maxPriority;
        maxPriority = tmp;
      }
      if (minPriority > int.MinValue)
      {
        factories = factories.Where(a => a.Priority >= minPriority);
      }
      if(maxPriority < int.MaxValue)
      {
        factories = factories.Where(a => a.Priority <= maxPriority);
      }

      //filter for TDbContext inheritance rules
      Func<IApplyBuilderToModel, bool> whereFunc = a =>
      {
        //check if the IApplyBuilerToModel was intended for this specific TDbContext OR if inheritance is allowed && TDbContext inherits appropriately
        return a.DbContextType == typeof(TDbContext)
              || (a.ApplyToInherited && a.DbContextType.IsAssignableFrom(typeof(TDbContext)));
      };

      factories = factories.Where(whereFunc);
      factories.OrderByDescending(a => a.Priority).Select(a => a.ApplyBuilderDelegate).ToList().ForEach(d => d(builder));
      return builder;
    }

    internal interface IApplyBuilderToModel
    {
      Action<ModelBuilder> ApplyBuilderDelegate { get; }

      Type DbContextType { get; }

      int Priority { get; }

      bool ApplyToInherited { get; }
    }

    internal sealed class ApplyBuilderToModelDelegateFactory<TDbContext> : IApplyBuilderToModel where TDbContext : DbContext
    {
      public Action<ModelBuilder> ApplyBuilderDelegate { get; set; }

      public Type DbContextType => typeof(TDbContext);

      public int Priority { get; set; }

      public bool ApplyToInherited { get; set; }
    }
    #endregion

    #region use common connection string
    public static class ContextConfigurationExtensions
    {
      //public static Action<IServiceProvider, DbContextOptionsBuilder> ContextBuilder => (p, b) =>
      //{
      //  //var config = p.GetRequiredService<IConfiguration>();
      //  var Options = p.GetRequiredService<IOptions<SharedContextOptions>>().Value;
      //  var decrypter = p.GetService<BasicEncryption>();
      //  var cs = Options.Encrypted ? decrypter.Decrypt(Options.ConnectionString) : Options.ConnectionString;
      //  b.UseSqlServer(cs);
      //  b.UseInternalServiceProvider(p);
      //};
    }

    #endregion
  }
}
