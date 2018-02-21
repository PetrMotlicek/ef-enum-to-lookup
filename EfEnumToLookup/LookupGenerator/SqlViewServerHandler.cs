using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EfEnumToLookup.LookupGenerator
{
	class SqlViewServerHandler : SqlServerHandlerBase
	{
		/// <inheritdoc />
		protected override string BuildSql(LookupDbModel model)
		{
			StringBuilder sql = new StringBuilder();
			sql.AppendLine("set nocount on;");
			if (UseTransaction)
			{
				sql.AppendLine("set xact_abort on; -- rollback on error");
				sql.AppendLine("begin tran;");
			}

			EnsureSchema(sql);
			CreateViews(sql, model.Lookups);
			AddCheckConstraints(sql, model.References, model.Lookups);

			if (UseTransaction)
			{
				sql.AppendLine("commit;");
			}
			return sql.ToString();
		}

		private static void ForEarchLookupValue(LookupData lookup, StringBuilder sb, string delimiter, Action<StringBuilder, LookupValue> singleEnumValueComposer)
		{
			using (IEnumerator<LookupValue> valueEnumerator = lookup.Values.GetEnumerator())
			{
				if (valueEnumerator.MoveNext())
				{
					singleEnumValueComposer(sb, valueEnumerator.Current);

					while (valueEnumerator.MoveNext())
					{
						sb.Append(delimiter);
						singleEnumValueComposer(sb, valueEnumerator.Current);
					}
				}
			}
		}

		private static string GetDescription(LookupValue lookupValue)
		{
			string description = lookupValue.Description == null ? "CAST(null as nvarchar(max))" : "N''" + lookupValue.Description.SanitizeExecuteSqlString() + "''";
			return description;
		}

		private static void AppendValueTuple(StringBuilder sb, LookupValue lookupValue)
		{
			sb.AppendLine($"({lookupValue.Id},{lookupValue.Name.SanitizeExecuteSqlString()})");
		}

		private static void AppendValueTupleWithDescription(StringBuilder sb, LookupValue lookupValue)
		{
			sb.AppendLine($"({lookupValue.Id},N''{lookupValue.Name.SanitizeExecuteSqlString()}'',{GetDescription(lookupValue)})");
		}

		private static void AppendSelectValues(StringBuilder sb, LookupValue lookupValue)
		{
			sb.AppendLine($"SELECT {lookupValue.Id},N''{lookupValue.Name.SanitizeExecuteSqlString()}''");
		}

		private static void AppendSelectValueWithDescription(StringBuilder sb, LookupValue lookupValue)
		{
			sb.AppendLine($"SELECT {lookupValue.Id},N''{lookupValue.Name.SanitizeExecuteSqlString()}'',{GetDescription(lookupValue)}");
		}

		private void CreateViews(StringBuilder sql, IEnumerable<LookupData> enums)
		{
			string schemaWithBrackets = GetSchema("[", "].");
			string schema = GetSchema();
			string descriptionDefinition = GenerateDescription ? ", CAST(Description as nvarchar(max)) as Description" : null;
			string description = GenerateDescription ? ", Description" : null;

			string appendTuppleDelimiter = Environment.NewLine + "			, ";
			string appendSelectDelimiter = Environment.NewLine + "			UNION " + Environment.NewLine + "			";

			Action<StringBuilder, LookupValue> appendTupleValue = GenerateDescription ? (Action<StringBuilder, LookupValue>)AppendValueTupleWithDescription : AppendValueTuple;
			Action<StringBuilder, LookupValue> appendSelectValue = GenerateDescription ? (Action<StringBuilder, LookupValue>)AppendSelectValueWithDescription : AppendSelectValues;

			foreach (LookupData lookup in enums)
			{
				string tableName = TableName(lookup.Name);
				string schemaAndTableName = TableName(lookup.Name, true);
				string idType = SqlServerGenerationHelper.NumericSqlType(lookup.NumericType);

				sql.AppendLine($"IF (OBJECT_ID('{schemaAndTableName}', 'V') IS NOT NULL) EXEC ('DROP VIEW {schemaWithBrackets}[{tableName}]')");
				sql.AppendLine($"EXEC ('CREATE VIEW {schemaWithBrackets}[{tableName}] AS");
				sql.AppendLine($"	SELECT CAST(Id as {idType}) as Id, CAST(Name as nvarchar({NameFieldLength:0})) as Name{descriptionDefinition}");
				sql.AppendLine("	FROM (");

				if (lookup.Values.Count <= 1000)
				{
					sql.AppendLine("		VALUES");
					ForEarchLookupValue(lookup, sql, appendTuppleDelimiter, appendTupleValue);
				}
				else
				{
					ForEarchLookupValue(lookup, sql, appendSelectDelimiter, appendSelectValue);
				}

				sql.AppendLine($"	) as t(Id,Name{description})");
				sql.AppendLine("');");

				sql.AppendViewMetadataDescriptionLine(schema, tableName, DbObjectDescription);
			}
		}

		private void AddCheckConstraints(StringBuilder sql, IEnumerable<EnumReference> refs, IEnumerable<LookupData> enums)
		{
			Dictionary<Type, ICollection<LookupValue>> enumsByType = enums.ToDictionary(e => e.EnumType, e => e.Values);

			foreach (EnumReference enumReference in refs)
			{
				ICollection<LookupValue> lookupValues = enumsByType[enumReference.EnumType];
				string values = string.Join(",", lookupValues.Select(lv => lv.Id.ToString("0")));

				string constraintName = "CK_" + enumReference.ReferencingField;

				string schema = enumReference.ReferencingTableSchema;
				string table = enumReference.ReferencingTable;
				string schemaSanitized = schema.SanitizeSqlString();
				string tableSanitized = table.SanitizeSqlString();

				string dropConstraint = $"IF Exists(SELECT * FROM sys.check_constraints c WHERE c.object_id = OBJECT_ID('{schemaSanitized}.{tableSanitized}', 'U') AND c.schema_id='{schemaSanitized}' AND c.name = N'{enumReference.ReferencingField.SanitizeSqlString()}') ALTER TABLE [{schema}].[{table}] DROP CONSTRAINT [{constraintName}];";

				string createConstraint = $"ALTER TABLE [{schema}].[{tableSanitized}] ADD CONSTRAINT [{constraintName}] CHECK([{enumReference.ReferencingField}] IN ({values}));";

				sql.AppendLine(dropConstraint);
				sql.AppendLine(createConstraint);
			}
		}
	}
}