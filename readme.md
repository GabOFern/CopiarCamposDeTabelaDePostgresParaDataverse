Aplicação console .NET que lê o schema de uma tabela PostgreSQL, cria os campos correspondentes em uma entidade do Dataverse e insere os dados.

---

## Como funciona

1. Lê o `appsettings.json` para obter as configurações de conexão
2. Conecta ao PostgreSQL e extrai o schema da tabela configurada
3. Cria os campos na entidade do Dataverse com base nos tipos de cada coluna
4. Adiciona os campos à solução configurada
5. Publica as alterações no Dataverse
6. Lê os dados da tabela e insere registro a registro na entidade

### Mapeamento de tipos

| PostgreSQL                     | Dataverse            |
|-------------------------------|----------------------|
| integer                       | Integer              |
| character varying / character | Text (String)        |
| text                          | Memo                 |
| numeric / decimal             | Decimal              |
| boolean                       | Boolean (Two Option) |
| timestamp without time zone   | DateTime             |

---

## Pré-requisitos

- .NET Framework 4.6.2
- Visual Studio 2019 ou superior
- Acesso ao banco PostgreSQL com permissão de leitura na tabela
- Usuário com permissão de customização no ambiente Dataverse
- A entidade de destino já deve existir no Dataverse antes de executar

---

## Como configurar

1. Copie o arquivo `appsettings.example.json` e renomeie para `appsettings.json`
2. Preencha os campos conforme o seu ambiente (veja a seção abaixo)
3. Certifique-se de que `appsettings.json` está marcado como **Copy to Output Directory: Always** no projeto

### Campos do appsettings.json

**PostgreSQL**
- `ConnectionString` — string de conexão completa com o banco PostgreSQL
- `TableName` — nome da tabela a ser lida (sem schema, apenas o nome da tabela)

**Dataverse**
- `ConnectionString` — string de conexão OAuth com o ambiente Dataverse
- `EntityName` — nome lógico da entidade de destino (ex: `dev_minhaentidade`)
- `FieldPrefix` — prefixo a ser adicionado em todos os campos criados (ex: `dev_`)
- `SolutionUniqueName` — nome único da solução onde os campos serão adicionados

---

## Como compilar e executar

O repositório não inclui os binários compilados. É necessário compilar antes de executar.

**Via Visual Studio:**
1. Abra o arquivo `CopiarCamposDeTabelaDePostgresParaDataverse.sln`
2. Clique com botão direito na solução → **Restore NuGet Packages**
3. Menu **Build → Build Solution** (ou `Ctrl+Shift+B`)
4. Execute com `F5` ou `Ctrl+F5`

**Via linha de comando:**

(bash)
nuget restore
msbuild CopiarCamposDeTabelaDePostgresParaDataverse.sln /p:Configuration=Release
bin\Release\CopiarCamposDeTabelaDePostgresParaDataverse.exe




> O `nuget restore` via terminal exige que o [NuGet CLI](https://www.nuget.org/downloads) esteja instalado e no PATH.

O progresso é exibido no console campo a campo e registro a registro. Em caso de erro em um campo específico, o processo continua para os demais.

---

## Observações

- Campos com tipos não mapeados são ignorados com aviso no console
- Valores nulos no PostgreSQL são ignorados silenciosamente (o campo fica vazio no Dataverse)
- O arquivo `appsettings.json` contém credenciais — **não versione este arquivo**
- Adicione `appsettings.json` ao `.gitignore`

---

## Estrutura do projeto

```
CopiarCamposDeTabelaDePostgresParaDataverse/
├── AppSettings.cs
├── DataverseService.cs
├── PostgresService.cs
├── Program.cs
├── App.config
├── appsettings.example.json
├── appsettings.json              # não versionado (.gitignore)
├── packages.config
├── CopiarCamposDeTabelaDePostgresParaDataverse.csproj
├── CopiarCamposDeTabelaDePostgresParaDataverse.sln
├── Properties/
│   └── AssemblyInfo.cs
└── packages/                     # não versionado (.gitignore)