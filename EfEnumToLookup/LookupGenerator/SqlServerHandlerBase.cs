﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace EfEnumToLookup.LookupGenerator
{
	/// <summary>
	/// Base abstract implementation of <see cref="IDbHandler"/> generating SQL script for MS SQL server.
	/// </summary>
	internal abstract class SqlServerHandlerBase : IDbHandler
	{
		/// <summary>
		/// Description added as metadata to each generated DB object.
		/// </summary>
		public const string DbObjectDescription = "Automatically generated. Contents will be overwritten on app startup. Table & contents generated by https://github.com/timabell/ef-enum-to-lookup";

		/// <summary>
		/// The size of the Name field that will be added to the generated lookup tables.
		/// Adjust to suit your data if required.
		/// </summary>
		public int NameFieldLength { get; set; }

		/// <summary>
		/// Prefix to add to all the generated tables to separate help group them together
		/// and make them stand out as different from other tables.
		/// </summary>
		public string TableNamePrefix { get; set; }

		/// <summary>
		/// Suffix to add to all the generated tables to separate help group them together
		/// and make them stand out as different from other tables.
		/// </summary>
		public string TableNameSuffix { get; set; }

		/// <summary>
		/// Whether to run the changes inside a database transaction.
		/// </summary>
		public bool UseTransaction { get; set; }

		/// <inheritdoc />
		public string Schema { get; set; }

		/// <inheritdoc />
		public bool GenerateDescription { get; set; }

		/// <summary>
		/// Make the required changes to the database.
		/// </summary>
		/// <param name="model">Details of lookups and foreign keys to add/update</param>
		/// <param name="runSql">A callback providing a means to execute sql against the
		/// server. (Or possibly write it to a file for later use.</param>
		public virtual void Apply(LookupDbModel model, Action<string, IEnumerable<SqlParameter>> runSql)
		{
			string sql = BuildSql(model);
			runSql(sql, null);
		}

		/// <summary>
		/// Generates the migration SQL needed to update the database to match
		/// the enums in the current model.
		/// </summary>
		/// <param name="model">Details of lookups and foreign keys to add/update</param>
		/// <returns>The generated SQL script</returns>
		public virtual string GenerateMigrationSql(LookupDbModel model)
		{
			return BuildSql(model);
		}

		protected virtual string TableName(string enumName, bool addSchema = false)
		{
			return SqlServerGenerationHelper.TableName(enumName, TableNamePrefix, TableNameSuffix, addSchema ? GetSchema() : null);
		}

		protected void EnsureSchema(StringBuilder sb)
		{
			string schema = GetSchema();

			if (!string.IsNullOrWhiteSpace(schema) && ! "dbo".Equals(schema, StringComparison.OrdinalIgnoreCase))
			{
				sb.AppendCreateSchema(schema);
			}
		}

		/// <summary>
		/// Gets <see cref="Schema"/> used for target DB object possibly enclosed by specific characters (strings)
		/// </summary>
		/// <param name="enclosing">Optional enclosing character (string) applied when <see cref="Schema"/> is set.</param>
		/// <param name="endEnclosing">Optional ending enclosing string. The <paramref name="enclosing"/> value is used when this parameter is null.</param>
		protected virtual string GetSchema(string enclosing = null, string endEnclosing = null)
		{
			string schema = string.IsNullOrWhiteSpace(Schema) ? "dbo" : Schema;
			return enclosing + schema + (endEnclosing ?? enclosing);
		}

		/// <summary>
		/// Abstract method which creates SQL considering all the <see cref="IDbHandler"/> settings.
		/// </summary>
		/// <param name="model">Details of lookups and foreign keys to add/update</param>
		/// <returns>The generated SQL script</returns>
		protected abstract string BuildSql(LookupDbModel model);
	}
}