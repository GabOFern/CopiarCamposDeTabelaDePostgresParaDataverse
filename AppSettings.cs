namespace CopiarCamposDeTabelaDePostgresParaDataverse
{
    public class AppSettings
    {
        public PostgreSQLSettings PostgreSQL { get; set; }
        public DataverseSettings Dataverse { get; set; }
    }

    public class PostgreSQLSettings
    {
        public string ConnectionString { get; set; }
        public string TableName { get; set; }
    }

    public class DataverseSettings
    {
        public string ConnectionString { get; set; }
        public string EntityName { get; set; }
        public string FieldPrefix { get; set; }
        public string SolutionUniqueName { get; set; }
    }
}