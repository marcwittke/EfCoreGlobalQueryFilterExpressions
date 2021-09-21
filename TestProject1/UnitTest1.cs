using System.Linq;
using System.Security.Claims;
using Backend.Fx.Environment.Authentication;
using Backend.Fx.Environment.MultiTenancy;
using EfCoreGlobalQueryFilterExpressions;
using FluentAssertions;
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
        
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void TheSystemIdentityInSpecificTenantSeesAllRecordsForGivenTenantId(int tenantIdToCheck)
        {
            using (var ctx = new QueryDbContext(
                _fixture.DbContextOptions,
                CurrentTenantIdHolder.Create(tenantIdToCheck),
                CurrentIdentityHolder.CreateSystem()))
            {
                var records = ctx.Records.ToArray();
                records.Length.Should().Be(50);
                foreach (var rec in records)
                {
                    rec.TenantId.Should().Be(tenantIdToCheck);
                }
            }
        }
        
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void TheAnonymousIdentityInSpecificTenantSeesNoRecords(int tenantIdToCheck)
        {
            using (var ctx = new QueryDbContext(
                _fixture.DbContextOptions,
                CurrentTenantIdHolder.Create(tenantIdToCheck),
                CurrentIdentityHolder.Create(new AnonymousIdentity())))
            {
                var records = ctx.Records.ToArray();
                records.Should().BeEmpty();
            }
        }
        
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void TheClaimsIdentityWithPermissionClaimInSpecificTenantSeesPermittedRecords(int tenantIdToCheck)
        {
            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim("can:read:all:records", "true"));
            using (var ctx = new QueryDbContext(
                _fixture.DbContextOptions,
                CurrentTenantIdHolder.Create(tenantIdToCheck),
                CurrentIdentityHolder.Create(identity)))
            {
                var records = ctx.Records.ToArray();
                records.Length.Should().Be(50);
                foreach (var rec in records)
                {
                    rec.TenantId.Should().Be(tenantIdToCheck);
                }
            }
        }
        
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void TheDefaultClaimsIdentityInSpecificTenantSeesPermittedRecords(int tenantIdToCheck)
        {
            var expectedCount = _fixture.ExecuteScalar<long>($"SELECT count(*) FROM records WHERE name LIKE '%5%' AND tenant_id = '{tenantIdToCheck}'");
            
            var identity = new ClaimsIdentity();
            using (var ctx = new QueryDbContext(
                _fixture.DbContextOptions,
                CurrentTenantIdHolder.Create(tenantIdToCheck),
                CurrentIdentityHolder.Create(identity)))
            {
                var records = ctx.Records.ToArray();
                records.Length.Should().Be((int)expectedCount);
                foreach (var rec in records)
                {
                    rec.TenantId.Should().Be(tenantIdToCheck);
                }
            }
        }

        
    }
}