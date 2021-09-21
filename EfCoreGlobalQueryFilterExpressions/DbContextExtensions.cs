using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EfCoreGlobalQueryFilterExpressions
{
    public static class DbContextExtensions
    {
        public static void ApplySnakeCaseMapping(this ModelBuilder modelBuilder)
        {
            foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
            {
                if (entity.BaseType == null && !entity.IsOwned())
                {
                    entity.SetTableName(ToSnakeCase(entity.GetTableName()));
                }

                foreach (IMutableProperty property in entity.GetProperties())
                {
                    var storeObjectId = StoreObjectIdentifier.Create(property.DeclaringEntityType, StoreObjectType.Table);
                    property.SetColumnName(ToSnakeCase(property.GetColumnName(storeObjectId.GetValueOrDefault())));
                }

                foreach (IMutableKey key in entity.GetKeys())
                {
                    key.SetName(ToSnakeCase(key.GetName()));
                }

                foreach (IMutableForeignKey key in entity.GetForeignKeys())
                {
                    key.SetConstraintName(ToSnakeCase(key.GetConstraintName()));
                }

                foreach (IMutableIndex index in entity.GetIndexes())
                {
                    index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()));
                }
            }
        }

        private static string ToSnakeCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            Match startUnderscores = Regex.Match(input, @"^_+");
            string snakeCase = startUnderscores + Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").Replace(" ", "_").ToLower();
            //Console.WriteLine($"{input}->{snakeCase}");
            return snakeCase;
        }
    }
}