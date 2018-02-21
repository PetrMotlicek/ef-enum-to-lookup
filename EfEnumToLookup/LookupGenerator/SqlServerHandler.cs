namespace EfEnumToLookup.LookupGenerator
{
	using System.Collections.Generic;
	using System.Text;

	class SqlServerHandler : SqlServerHandlerBase
	{
		protected override string BuildSql(LookupDbModel model)
		{
			var sql = new StringBuilder();
			sql.AppendLine("set nocount on;");
			if (UseTransaction)
			{
				sql.AppendLine("set xact_abort on; -- rollback on error");
				sql.AppendLine("begin tran;");
			}

			EnsureSchema(sql);
			sql.AppendLine(CreateTables(model.Lookups));
			sql.AppendLine(PopulateLookups(model.Lookups));
			sql.AppendLine(AddForeignKeys(model.References));
			if (UseTransaction)
			{
				sql.AppendLine("commit;");
			}
			return sql.ToString();
		}

		private string CreateTables(IEnumerable<LookupData> enums)
		{
			StringBuilder sql = new StringBuilder();

			string schema = GetSchema();
			string schemaWithBrackets = GetSchema("[", "].");

			string description = GenerateDescription ? ", Description nvarchar(max)" : null;

			foreach (LookupData lookup in enums)
			{
				string tableName = TableName(lookup.Name);
				string schemaAndTableName = TableName(lookup.Name, true);
				string idType = SqlServerGenerationHelper.NumericSqlType(lookup.NumericType);

				sql.AppendLine($"IF OBJECT_ID('{schemaAndTableName}', 'U') IS NULL");
				sql.AppendLine("begin");
				sql.AppendLine($"\tCREATE TABLE {schemaWithBrackets}[{tableName}] (Id {idType} CONSTRAINT PK_{tableName} PRIMARY KEY, Name nvarchar({NameFieldLength:0}){description})");

				sql.AppendTableMetadataDescriptionLine(schema, tableName, DbObjectDescription, "\t");
				sql.AppendLine("end");
			}
			return sql.ToString();
		}

		private string AddForeignKeys(IEnumerable<EnumReference> refs)
		{
			string schemaWithBrackets = GetSchema("[", "].");

			var sql = new StringBuilder();
			foreach (var enumReference in refs)
			{
				var fkName = string.Format("FK_{0}_{1}", enumReference.ReferencingTable, enumReference.ReferencingField);

				sql.AppendFormat(
					" IF OBJECT_ID('{0}', 'F') IS NULL ALTER TABLE [{1}].[{2}] ADD CONSTRAINT {0} FOREIGN KEY ([{3}]) REFERENCES {4}[{5}] (Id);\r\n",
					fkName, enumReference.ReferencingTableSchema, enumReference.ReferencingTable, enumReference.ReferencingField, schemaWithBrackets, TableName(enumReference.EnumType.Name)
				);
			}
			return sql.ToString();
		}

		private string PopulateLookups(IEnumerable<LookupData> lookupData)
		{
			string description = GenerateDescription ? ", Description nvarchar(max) COLLATE database_default" : null;

			var sql = new StringBuilder();
			sql.AppendLine($"CREATE TABLE #lookups (Id int, Name nvarchar({NameFieldLength:0}) COLLATE database_default{description});");

			foreach (LookupData lookup in lookupData)
			{
				sql.AppendLine(PopulateLookup(lookup));
			}
			sql.AppendLine("DROP TABLE #lookups;");
			return sql.ToString();
		}

		private string PopulateLookup(LookupData lookup)
		{
			string descriptionField = GenerateDescription ? ", Description" : null;

			var sql = new StringBuilder();
			foreach (var value in lookup.Values)
			{
				string name = value.Name.SanitizeSqlString();
				string desc = GenerateDescription ? ", N'" + value.Description.SanitizeSqlString() + "'" : null;

				sql.AppendLine($"INSERT INTO #lookups (Id, Name{descriptionField}) VALUES ({value.Id:0}, N'{name}'{desc});");
			}

			string descriptionInsert = GenerateDescription ? ", src.Description" : null;
			string descriptionWhen = GenerateDescription ? " OR src.Description <> dst.Description collate Latin1_General_BIN2" : null;
			string descriptionUpdate = GenerateDescription ? ", Description = src.Description" : null;

			string tableName = TableName(lookup.Name);
			string schemaWithBrackets = GetSchema("[", "].");

			sql.AppendLine(string.Format(@"
MERGE INTO {0}[{1}] dst
	USING #lookups src ON src.Id = dst.Id
	WHEN MATCHED AND (src.Name <> dst.Name collate Latin1_General_BIN2{2}) THEN UPDATE SET Name = src.Name{3}
	WHEN NOT MATCHED THEN
		INSERT (Id, Name{4})
		VALUES (src.Id, src.Name{5})
	WHEN NOT MATCHED BY SOURCE THEN
		DELETE
;"
				, schemaWithBrackets, tableName, descriptionWhen, descriptionUpdate, descriptionField, descriptionInsert));

			sql.AppendLine("TRUNCATE TABLE #lookups;");
			return sql.ToString();
		}
	}
}
