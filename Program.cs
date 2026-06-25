using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Messages;

namespace CopiarCamposDeTabelaDePostgresParaDataverse
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                AppSettings settings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(configPath));

                Console.WriteLine("Iniciando o processo de criação de campos no Dataverse...");
                Console.WriteLine("Conectando ao PostgreSQL...");

                var postgresService = new PostgresService(settings.PostgreSQL.ConnectionString);
                var schema = postgresService.GetTableSchema(settings.PostgreSQL.TableName);

                if (schema.Count == 0)
                {
                    Console.WriteLine($"Nenhuma coluna encontrada na tabela '{settings.PostgreSQL.TableName}'.");
                    return;
                }

                var dataverseService = new DataverseService(settings.Dataverse.ConnectionString);

                var fieldMapping = dataverseService.CreateFieldsFromSchema(
                    settings.Dataverse.EntityName,
                    schema,
                    settings.Dataverse.FieldPrefix,
                    settings.Dataverse.SolutionUniqueName
                );

                dataverseService.PublishAll(settings.Dataverse.EntityName);
                Console.WriteLine("Campos criados e publicados com sucesso!");

                Console.WriteLine("Iniciando inserção de dados no Dataverse...");
                InsertData(postgresService, dataverseService, settings, schema, fieldMapping);
                Console.WriteLine("Dados inseridos no Dataverse com sucesso!");
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                Console.WriteLine($"[Erro Fatal Dataverse] {ex.Message}");
                Console.WriteLine($"Detalhes: {ex.Detail?.Message}");
                Console.WriteLine($"Código: {ex.Detail?.ErrorCode}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Exceção Interna: {ex.InnerException.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Erro Fatal] {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Exceção Interna: {ex.InnerException.Message}");
            }
            finally
            {
                Console.WriteLine("Pressione qualquer tecla para sair...");
                Console.ReadKey();
            }
        }

        static void InsertData(
            PostgresService postgresService,
            DataverseService dataverseService,
            AppSettings settings,
            List<(string ColumnName, string DataType)> schema,
            List<(string ColumnName, string DataverseFieldName)> fieldMapping)
        {
            // Monta dicionário de tipo por coluna para evitar chamadas repetidas ao banco
            var schemaDict = new Dictionary<string, string>();
            foreach (var col in schema)
                schemaDict[col.ColumnName] = col.DataType.ToLower();

            try
            {
                using (var conn = new NpgsqlConnection(settings.PostgreSQL.ConnectionString))
                {
                    conn.Open();

                    using (var reader = postgresService.GetTableData(settings.PostgreSQL.TableName, conn))
                    {
                        while (reader.Read())
                        {
                            Entity record = new Entity(settings.Dataverse.EntityName);

                            foreach (var mapping in fieldMapping)
                            {
                                if (reader.IsDBNull(reader.GetOrdinal(mapping.ColumnName)))
                                    continue;

                                if (!schemaDict.TryGetValue(mapping.ColumnName, out string dataType))
                                {
                                    Console.WriteLine($"[Aviso] Tipo não encontrado para '{mapping.ColumnName}'. Ignorando.");
                                    continue;
                                }

                                object value = reader[mapping.ColumnName];

                                switch (dataType)
                                {
                                    case "integer":
                                        record[mapping.DataverseFieldName] = Convert.ToInt32(value);
                                        break;
                                    case "character varying":
                                    case "character":
                                    case "text":
                                        record[mapping.DataverseFieldName] = value.ToString();
                                        break;
                                    case "timestamp without time zone":
                                        record[mapping.DataverseFieldName] = Convert.ToDateTime(value);
                                        break;
                                    case "boolean":
                                        record[mapping.DataverseFieldName] = Convert.ToBoolean(value);
                                        break;
                                    case "numeric":
                                    case "decimal":
                                        record[mapping.DataverseFieldName] = Convert.ToDecimal(value);
                                        break;
                                    default:
                                        Console.WriteLine($"[Aviso] Tipo não suportado para inserção: {dataType} ({mapping.ColumnName})");
                                        break;
                                }
                            }

                            Guid id = dataverseService.CreateEntityRecord(record);
                            Console.WriteLine($"[Insert] Registro criado. ID: {id}");
                        }
                    }
                }
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                Console.WriteLine($"[Erro Dataverse] Falha ao inserir dados: {ex.Message}");
                Console.WriteLine($"Detalhes: {ex.Detail?.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Exceção Interna: {ex.InnerException.Message}");
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"[Erro PostgreSQL] Falha ao ler dados: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Exceção Interna: {ex.InnerException.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Erro Geral] Falha ao inserir dados: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Exceção Interna: {ex.InnerException.Message}");
            }
        }
    }
}