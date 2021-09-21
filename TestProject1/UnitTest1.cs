using System.Linq;
using Backend.Fx.Environment.Authentication;
using Backend.Fx.Environment.MultiTenancy;
using EfCoreGlobalQueryFilterExpressions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace TestProject1
{
    public class UnitTest1 : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;

        public UnitTest1(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }
        
        [Fact]
        public void Test1()
        {
            DbContextOptions options = new DbContextOptionsBuilder<QueryDbContext>()
                                       .UseNpgsql(_fixture.ConnectionString)
                                       .Options;

            const int tenantIdToCheck = 1;
            
            using (var ctx = new QueryDbContext(
                options,
                CurrentTenantIdHolder.Create(tenantIdToCheck),
                CurrentIdentityHolder.CreateSystem()))
            {
                var records = ctx.Records.ToArray();
                Assert.Equal(50, records.Length);
                foreach (var rec in records)
                {
                    Assert.Equal(tenantIdToCheck, rec.TenantId);
                }
            }
        }
    }
}