using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using CrmLabel = Microsoft.Xrm.Sdk.Label;

namespace CopiarCamposDeTabelaDePostgresParaDataverse
{
    public class DataverseService
    {
        private readonly ServiceClient _serviceClient;
        private const int COMPONENT_TYPE_ATTRIBUTE = 2;

        public DataverseService(string connectionString)
        {
            _serviceClient = new ServiceClient(connectionString);

            if (!_serviceClient.IsReady)
                throw new Exception("Falha na conexão com o Dataverse: " + _serviceClient.LastError);

            Console.WriteLine("Conexão com o Dataverse estabelecida com sucesso!");
        }

        public Guid CreateEntityRecord(Entity record)
        {
            return _serviceClient.Create(record);
        }

        public List<(string ColumnName, string DataverseFieldName)> CreateFieldsFromSchema(
            string entityName,
            List<(string ColumnName, string DataType)> schema,
            string prefix,
            string solutionUniqueName)
        {
            var fieldMapping = new List<(string ColumnName, string DataverseFieldName)>();

            foreach (var column in schema)
            {
                string fieldName = prefix + column.ColumnName;
                string displayName = column.ColumnName;

                switch (column.DataType.ToLower())
                {
                    case "integer":
                        CreateIntegerField(entityName, fieldName, displayName, solutionUniqueName);
                        fieldMapping.Add((column.ColumnName, fieldName));
                        break;
                    case "character varying":
                    case "character":
                        CreateTextField(entityName, fieldName, displayName, 100, solutionUniqueName);
                        fieldMapping.Add((column.ColumnName, fieldName));
                        break;
                    case "timestamp without time zone":
                        CreateDateTimeField(entityName, fieldName, displayName, DateTimeFormat.DateAndTime, solutionUniqueName);
                        fieldMapping.Add((column.ColumnName, fieldName));
                        break;
                    case "boolean":
                        CreateBooleanField(entityName, fieldName, displayName, "Sim", "Não", solutionUniqueName);
                        fieldMapping.Add((column.ColumnName, fieldName));
                        break;
                    case "numeric":
                    case "decimal":
                        CreateDecimalField(entityName, fieldName, displayName, 8, 2, solutionUniqueName);
                        fieldMapping.Add((column.ColumnName, fieldName));
                        break;
                    case "text":
                        CreateMemoField(entityName, fieldName, displayName, 500, solutionUniqueName);
                        fieldMapping.Add((column.ColumnName, fieldName));
                        break;
                    default:
                        Console.WriteLine($"[Aviso] Tipo não suportado: {column.DataType} para a coluna {column.ColumnName}");
                        break;
                }
            }

            return fieldMapping;
        }

        public void PublishAll(string entityName)
        {
            Console.WriteLine("[Publish] Publicando atributos...");
            try
            {
                _serviceClient.Execute(new PublishAllXmlRequest());
                Console.WriteLine($"[Publish] Publicação da entidade '{entityName}' concluída.");
            }
            catch (FaultException<OrganizationServiceFault> ex) { LogDataverseError("publicar atributos", entityName, ex); }
            catch (Exception ex) { LogGeneralError("publicar atributos", entityName, ex); }
        }

        private void CreateTextField(string entityName, string schemaName, string displayName, int maxLength, string solutionUniqueName)
        {
            Console.WriteLine($"[TextField] Criando '{schemaName}'...");
            try
            {
                var response = (CreateAttributeResponse)_serviceClient.Execute(new CreateAttributeRequest
                {
                    EntityName = entityName,
                    Attribute = new StringAttributeMetadata
                    {
                        SchemaName = schemaName,
                        DisplayName = new CrmLabel(displayName, 1046),
                        RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None),
                        MaxLength = maxLength,
                        FormatName = StringFormatName.Text
                    }
                });

                Console.WriteLine($"[TextField] '{schemaName}' criado.");
                AddComponentToSolution(response.AttributeId, solutionUniqueName);
            }
            catch (FaultException<OrganizationServiceFault> ex) { LogDataverseError("criar TextField", schemaName, ex); }
            catch (Exception ex) { LogGeneralError("criar TextField", schemaName, ex); }
        }

        private void CreateIntegerField(string entityName, string schemaName, string displayName, string solutionUniqueName)
        {
            Console.WriteLine($"[IntegerField] Criando '{schemaName}'...");
            try
            {
                var response = (CreateAttributeResponse)_serviceClient.Execute(new CreateAttributeRequest
                {
                    EntityName = entityName,
                    Attribute = new IntegerAttributeMetadata
                    {
                        SchemaName = schemaName,
                        DisplayName = new CrmLabel(displayName, 1046),
                        RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None)
                    }
                });

                Console.WriteLine($"[IntegerField] '{schemaName}' criado.");
                AddComponentToSolution(response.AttributeId, solutionUniqueName);
            }
            catch (FaultException<OrganizationServiceFault> ex) { LogDataverseError("criar IntegerField", schemaName, ex); }
            catch (Exception ex) { LogGeneralError("criar IntegerField", schemaName, ex); }
        }

        private void CreateDecimalField(string entityName, string schemaName, string displayName, int precision, int scale, string solutionUniqueName)
        {
            Console.WriteLine($"[DecimalField] Criando '{schemaName}'...");
            try
            {
                var response = (CreateAttributeResponse)_serviceClient.Execute(new CreateAttributeRequest
                {
                    EntityName = entityName,
                    Attribute = new DecimalAttributeMetadata
                    {
                        SchemaName = schemaName,
                        DisplayName = new CrmLabel(displayName, 1046),
                        RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None),
                        Precision = precision,
                        MinValue = 0,
                        MaxValue = 1000000
                    }
                });

                Console.WriteLine($"[DecimalField] '{schemaName}' criado.");
                AddComponentToSolution(response.AttributeId, solutionUniqueName);
            }
            catch (FaultException<OrganizationServiceFault> ex) { LogDataverseError("criar DecimalField", schemaName, ex); }
            catch (Exception ex) { LogGeneralError("criar DecimalField", schemaName, ex); }
        }

        private void CreateDateTimeField(string entityName, string schemaName, string displayName, DateTimeFormat format, string solutionUniqueName)
        {
            Console.WriteLine($"[DateTimeField] Criando '{schemaName}'...");
            try
            {
                var response = (CreateAttributeResponse)_serviceClient.Execute(new CreateAttributeRequest
                {
                    EntityName = entityName,
                    Attribute = new DateTimeAttributeMetadata
                    {
                        SchemaName = schemaName,
                        DisplayName = new CrmLabel(displayName, 1046),
                        RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None),
                        Format = format,
                        DateTimeBehavior = DateTimeBehavior.UserLocal
                    }
                });

                Console.WriteLine($"[DateTimeField] '{schemaName}' criado.");
                AddComponentToSolution(response.AttributeId, solutionUniqueName);
            }
            catch (FaultException<OrganizationServiceFault> ex) { LogDataverseError("criar DateTimeField", schemaName, ex); }
            catch (Exception ex) { LogGeneralError("criar DateTimeField", schemaName, ex); }
        }

        private void CreateBooleanField(string entityName, string schemaName, string displayName, string trueLabel, string falseLabel, string solutionUniqueName)
        {
            Console.WriteLine($"[BooleanField] Criando '{schemaName}'...");
            try
            {
                var response = (CreateAttributeResponse)_serviceClient.Execute(new CreateAttributeRequest
                {
                    EntityName = entityName,
                    Attribute = new BooleanAttributeMetadata
                    {
                        SchemaName = schemaName,
                        DisplayName = new CrmLabel(displayName, 1046),
                        RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None),
                        OptionSet = new BooleanOptionSetMetadata
                        {
                            TrueOption = new OptionMetadata(new CrmLabel(trueLabel, 1046), 1),
                            FalseOption = new OptionMetadata(new CrmLabel(falseLabel, 1046), 0)
                        }
                    }
                });

                Console.WriteLine($"[BooleanField] '{schemaName}' criado.");
                AddComponentToSolution(response.AttributeId, solutionUniqueName);
            }
            catch (FaultException<OrganizationServiceFault> ex) { LogDataverseError("criar BooleanField", schemaName, ex); }
            catch (Exception ex) { LogGeneralError("criar BooleanField", schemaName, ex); }
        }

        private void CreateMemoField(string entityName, string schemaName, string displayName, int maxLength, string solutionUniqueName)
        {
            Console.WriteLine($"[MemoField] Criando '{schemaName}'...");
            try
            {
                var response = (CreateAttributeResponse)_serviceClient.Execute(new CreateAttributeRequest
                {
                    EntityName = entityName,
                    Attribute = new MemoAttributeMetadata
                    {
                        SchemaName = schemaName,
                        DisplayName = new CrmLabel(displayName, 1046),
                        RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None),
                        MaxLength = maxLength
                    }
                });

                Console.WriteLine($"[MemoField] '{schemaName}' criado.");
                AddComponentToSolution(response.AttributeId, solutionUniqueName);
            }
            catch (FaultException<OrganizationServiceFault> ex) { LogDataverseError("criar MemoField", schemaName, ex); }
            catch (Exception ex) { LogGeneralError("criar MemoField", schemaName, ex); }
        }

        private void AddComponentToSolution(Guid componentId, string solutionUniqueName)
        {
            if (string.IsNullOrEmpty(solutionUniqueName)) return;

            try
            {
                _serviceClient.Execute(new AddSolutionComponentRequest
                {
                    ComponentId = componentId,
                    ComponentType = COMPONENT_TYPE_ATTRIBUTE,
                    SolutionUniqueName = solutionUniqueName
                });

                Console.WriteLine($"[Solution] Componente {componentId} adicionado à solução '{solutionUniqueName}'.");
            }
            catch (FaultException<OrganizationServiceFault> ex) { LogDataverseError("adicionar componente", solutionUniqueName, ex); }
            catch (Exception ex) { LogGeneralError("adicionar componente", solutionUniqueName, ex); }
        }

        private void LogDataverseError(string acao, string contexto, FaultException<OrganizationServiceFault> ex)
        {
            Console.WriteLine($"[Erro Dataverse] Falha ao {acao} em '{contexto}':");
            Console.WriteLine($"Mensagem: {ex.Message}");
            Console.WriteLine($"Detalhes: {ex.Detail?.Message}");
            Console.WriteLine($"Código de Erro: {ex.Detail?.ErrorCode}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
                Console.WriteLine($"Exceção Interna: {ex.InnerException.Message}");
        }

        private void LogGeneralError(string acao, string contexto, Exception ex)
        {
            Console.WriteLine($"[Erro Geral] Falha ao {acao} em '{contexto}':");
            Console.WriteLine($"Mensagem: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
                Console.WriteLine($"Exceção Interna: {ex.InnerException.Message}");
        }
    }
}