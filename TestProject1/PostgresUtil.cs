using System.Data;
using Npgsql;

namespace TestProject1
{
    public class PostgresUtil
    {
        public PostgresUtil(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; }


        public void EnsureDroppedDatabase(string dbName)
        {
            using (IDbConnection connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                bool exists = ExistsDatabase(dbName);

                if (exists)
                {
                    using (IDbCommand singleUserCmd = connection.CreateCommand())
                    {
                        singleUserCmd.CommandText = $"UPDATE pg_database SET datallowconn = 'false' WHERE datname = '{dbName}';";
                        singleUserCmd.ExecuteNonQuery();
                    }

                    using (IDbCommand termConnCmd = connection.CreateCommand())
                    {
                        termConnCmd.CommandText = $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{dbName}';";
                        termConnCmd.ExecuteNonQuery();
                    }

                    using (IDbCommand dropDbCmd = connection.CreateCommand())
                    {
                        dropDbCmd.CommandText = $"DROP DATABASE \"{dbName}\";";
                        dropDbCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public bool ExistsDatabase(string dbName)
        {
            bool exists;
            using (IDbConnection connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                using (IDbCommand existsCommand = connection.CreateCommand())
                {
                    existsCommand.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'";
                    exists = (int?) existsCommand.ExecuteScalar() == 1;
                }
            }

            return exists;
        }
        
        public void CreateDatabase(string dbName)
        {
            using (IDbConnection connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                using (IDbCommand createCommand = connection.CreateCommand())
                {
                    createCommand.CommandText = $"CREATE DATABASE \"{dbName}\"";
                    createCommand.ExecuteNonQuery();
                }
            }
        }
    }
}