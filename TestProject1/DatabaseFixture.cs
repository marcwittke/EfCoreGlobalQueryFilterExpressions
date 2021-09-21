using System.Data;
using Npgsql;

namespace TestProject1
{
    public class DatabaseFixture
    {
        public DatabaseFixture()
        {
	        string dbName = "EfCoreGlobalQueryFilterExpressions";

	        PostgresUtil pgUtil = new PostgresUtil("Host=localhost;Username=anicors;Password=P0stgr3s");
	        pgUtil.EnsureDroppedDatabase(dbName);
	        pgUtil.CreateDatabase(dbName);
	        ConnectionString = new NpgsqlConnectionStringBuilder(pgUtil.ConnectionString) {Database = dbName}.ConnectionString;

	        using (IDbConnection connection = new NpgsqlConnection(ConnectionString))
	        {
		        connection.Open();

		        using (IDbCommand createCommand = connection.CreateCommand())
		        {
			        string ddl = @"
CREATE TABLE records (
	id int4 NOT NULL,
	tenant_id int4 NOT NULL,
	name varchar(100) NOT NULL,
	number int4 NOT NULL,
	CONSTRAINT pk_stores PRIMARY KEY (id)
);";

			        createCommand.CommandText = ddl;
			        createCommand.ExecuteNonQuery();
		        }

		        for (int i = 0; i < 100; i++)
		        {
			        using (IDbCommand insertCommand = connection.CreateCommand())
			        {
				        string insert = @"INSERT INTO records (id, tenant_id, name, number) values (:id, :tenant_id, :name, :number)";
						insertCommand.CommandText = insert;
						insertCommand.Parameters.Add(new NpgsqlParameter(":id", i));
						insertCommand.Parameters.Add(new NpgsqlParameter(":tenant_id", i%2==0 ? 1 : 2));
						insertCommand.Parameters.Add(new NpgsqlParameter(":name", "record {i:0000}"));
						insertCommand.Parameters.Add(new NpgsqlParameter(":number", i+1000));
				        insertCommand.ExecuteNonQuery();
			        }
		        }
	        }
        }

        public string ConnectionString { get; }
    }
}