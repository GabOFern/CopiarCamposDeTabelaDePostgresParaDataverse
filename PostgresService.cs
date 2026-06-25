using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CopiarCamposDeTabelaDePostgresParaDataverse
{
    public class PostgresService
    {
        private readonly string _connectionString;

        public PostgresService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<(string ColumnName, string DataType)> GetTableSchema(string tableName)
        {
            var columns = new List<(string, string)>();

            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = $@"
                        SELECT column_name, data_type
                        FROM information_schema.columns
                        WHERE table_name = '{tableName.Split('.').Last()}'
                        AND table_schema = 'public'";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            columns.Add((reader.GetString(0), reader.GetString(1)));
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"[Erro PostgreSQL] Falha ao obter esquema da tabela '{tableName}':");
                Console.WriteLine($"Mensagem: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Exceção Interna: {ex.InnerException.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Erro Geral] Falha ao obter esquema da tabela '{tableName}':");
                Console.WriteLine($"Mensagem: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Exceção Interna: {ex.InnerException.Message}");
            }

            return columns;
        }

        public NpgsqlDataReader GetTableData(string tableName, NpgsqlConnection conn)
        {
            var cmd = new NpgsqlCommand($"SELECT * FROM {tableName}", conn);
            return cmd.ExecuteReader();
        }
    }
}