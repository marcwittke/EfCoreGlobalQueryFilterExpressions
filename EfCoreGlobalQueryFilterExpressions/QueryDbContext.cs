using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Backend.Fx.Environment.Authentication;
using Backend.Fx.Environment.MultiTenancy;
using Backend.Fx.Patterns.DependencyInjection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace EfCoreGlobalQueryFilterExpressions
{
    public sealed class QueryDbContext : DbContext
    {
        private readonly ICurrentTHolder<IIdentity> _identityHolder;
        private readonly ICurrentTHolder<TenantId> _tenantIdHolder;

        public QueryDbContext(DbContextOptions dbContextOptions,
                              ICurrentTHolder<TenantId> tenantIdHolder,
                              ICurrentTHolder<IIdentity> identityHolder)
            : base(dbContextOptions)
        {
            _identityHolder = identityHolder;
            _tenantIdHolder = tenantIdHolder;
            
            // no change tracking boosts performance
            ChangeTracker.AutoDetectChangesEnabled = false;
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public DbSet<Record> Records { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplySnakeCaseMapping();

            // apply permission and tenant global query filter
            foreach (var queryableType in GetQueryableTypes())
            {
                // via reflection, we find the method that returns the permission filter (returning a type-targeted Expression<Func<T:queryClrType, bool>> as LambdaExpression)
                LambdaExpression permissionLambdaExpression = GetPermissionExpression(queryableType);

                // we need this later when joining the two expressions to target the same parameter, otherwise EF fails to translate the query
                var entityParamExpr = permissionLambdaExpression.Parameters.Single();

                // the tenantFilterLambdaExpression is hand crafted (not a lambda, just a binary (property-equals-constant) expression
                var tenantIdPropExpr = Expression.Property(entityParamExpr, "TenantId");
                var constExpr = Expression.Constant(_tenantIdHolder.Current.Value);
                Expression tenantFilterExpression = Expression.Equal(tenantIdPropExpr, constExpr);

                // combine both to a new expression
                Expression combinedExpression = Expression.And(
                    permissionLambdaExpression.Body, // we need just the body here!
                    tenantFilterExpression // this is just a binary expression , not a lambda yet, therefore no body
                );

                // make a new lambda on the original parameter expression but with the new combined body
                var lambda = Expression.Lambda(combinedExpression, entityParamExpr);

                // apply the tenant and permission filter lambda expression as global query filter
                modelBuilder.Entity(queryableType).HasQueryFilter(lambda);
            }
        }

        /// <summary>
        /// Finds and invokes the method that returns a (generic) Expression&lt;Func&lt;T, bool&gt;&gt; while being non generic itself.
        /// </summary>
        /// <param name="t">The type that is used as generic type parameter</param>
        /// <returns>The Expression&lt;Func&lt;T, bool&gt;&gt; as LambdaExpression</returns>
        private LambdaExpression GetPermissionExpression(Type t)
        {
            Type funcType = typeof(Func<,>).MakeGenericType(t, typeof(bool)); // Func<T, bool>
            Type expressionType = typeof(Expression<>).MakeGenericType(funcType); // Expression<Func<T, bool>>
            MethodInfo[] methodInfos = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo getPermissionExpressionMethodInfo = methodInfos.SingleOrDefault(m => m.ReturnType == expressionType)
                                                           ?? throw new MissingMethodException(
                                                               $"No method on {GetType()} returns an Expression<Func<{t}, bool>>");
            object permissionExpressionObj = getPermissionExpressionMethodInfo.Invoke(
                this,
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                null,
                CultureInfo.InvariantCulture);

            LambdaExpression permissionExpression = (LambdaExpression)permissionExpressionObj;
            return permissionExpression;
        }

        /// <summary>
        /// Gets all queryable types in this DbContext.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Note that this implementation iterates over available DbSet&lt;T&gt; properties.</remarks>
        private IEnumerable<Type> GetQueryableTypes()
        {
            // won't work, because it returns all types present in the model, also non aggregate roots that cannot be filtered by tenantId
            // modelBuilder.Model.GetEntityTypes().Select(e => e.ClrType)

            PropertyInfo[] propertyInfos = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return propertyInfos
                   .Where(pi => pi.PropertyType.IsGenericType && typeof(DbSet<>).IsAssignableFrom(pi.PropertyType.GetGenericTypeDefinition()))
                   .Select(pi => pi.PropertyType.GetGenericArguments()[0]);
        }

        #region prevent calling SaveChanges

        private const string SaveChangesNotSupportedMessage =
            "This query context is for querying only and does not support saving changes.";

        public override int SaveChanges()
            => throw new NotSupportedException(SaveChangesNotSupportedMessage);

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
            => throw new NotSupportedException(SaveChangesNotSupportedMessage);

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
            => throw new NotSupportedException(SaveChangesNotSupportedMessage);

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new())
            => throw new NotSupportedException(SaveChangesNotSupportedMessage);

        #endregion
        
        
        
        /// <summary>
        /// this method will be detected via reflection and its return value is used to build the global query filter 
        /// </summary>
        /// <returns></returns>
        [UsedImplicitly]
        private Expression<Func<Record, bool>> GetRecordPermission()
        {
            // system may read everything
            if (_identityHolder.Current is SystemIdentity)
            {
                return rec => true;
            }
            
            // anon may read nothing
            if (_identityHolder.Current is AnonymousIdentity)
            {
                return rec => false;
            }

            // others depend on claims
            if (((ClaimsIdentity)_identityHolder.Current).FindFirst("can:read:all:records") != null)
            {
                return rec => true;    
            }

            // fallback: arbitrary business rule
            return rec => rec.Number > 1050;
        }
    }
}