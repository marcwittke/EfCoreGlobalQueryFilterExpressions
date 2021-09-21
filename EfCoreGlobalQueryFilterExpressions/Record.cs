
namespace EfCoreGlobalQueryFilterExpressions
{
    public class Record
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string Name { get; set; }
        public int Number { get; set; }

        public Record(int id, int tenantId, string name, int number)
        {
            Id = id;
            TenantId = tenantId;
            Name = name;
            Number = number;
        }
    }
}