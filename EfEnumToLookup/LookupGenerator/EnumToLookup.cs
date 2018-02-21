﻿namespace EfEnumToLookup.LookupGenerator
{
	using System;
	using System.Collections.Generic;
	using System.Data.Entity;
	using System.Data.Entity.Infrastructure;
	using System.Data.SqlClient;
	using System.Linq;
	using System.Reflection;
	using System.Text;

	/// <summary>
	/// Makes up for a missing feature in Entity Framework 6.1
	/// Creates lookup tables and foreign key constraints based on the enums
	/// used in your model.
	/// Use the properties exposed to control behaviour.
	/// Run <c>Apply</c> from your Seed method in either your database initializer
	/// or your EF Migrations.
	/// It is safe to run repeatedly, and will ensure enum values are kept in line
	/// with your current code.
	/// Source code: https://github.com/timabell/ef-enum-to-lookup
	/// License: MIT
	/// </summary>
	public class EnumToLookup : IEnumToLookup
	{
		private readonly EnumParser _enumParser;

		public EnumToLookup()
		{
			// set default behaviour, can be overridden by setting properties on object before calling Apply()
			NameFieldLength = 255;
			TableNamePrefix = "Enum_";
			_enumParser = new EnumParser { SplitWords = true };
			UseTransaction = true;
		}

		/// <summary>
		/// If set to true (default) enum names will have spaces inserted between
		/// PascalCase words, e.g. enum SomeValue is stored as "Some Value".
		/// </summary>
		public bool SplitWords
		{
			set { _enumParser.SplitWords = value; }
			get { return _enumParser.SplitWords; }
		}

		/// <summary>
		/// The size of the Name field that will be added to the generated lookup tables.
		/// Adjust to suit your data if required, defaults to 255.
		/// </summary>
		public int NameFieldLength { get; set; }

		/// <summary>
		/// Prefix to add to all the generated tables to separate help group them together
		/// and make them stand out as different from other tables.
		/// Defaults to "Enum_" set to null or "" to not have any prefix.
		/// </summary>
		public string TableNamePrefix { get; set; }

		/// <summary>
		/// Suffix to add to all the generated tables to separate help group them together
		/// and make them stand out as different from other tables.
		/// Defaults to "" set to null or "" to not have any suffix.
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

		/// <inheritdoc />
		public bool GenerateViews { get; set; }

		/// <summary>
		/// Create any missing lookup tables,
		/// enforce values in the lookup tables
		/// by way of a T-SQL MERGE
		/// </summary>
		/// <param name="context">EF Database context to search for enum references,
		///  context.Database.ExecuteSqlCommand() is used to apply changes.</param>
		public void Apply(DbContext context)
		{
			var model = BuildModelFromContext(context);

			var dbHandler = GetDbHandler();

			dbHandler.Apply(model, (sql, parameters) => ExecuteSqlCommand(context, sql, parameters));
		}

		/// <summary>
		/// Rather than applying the changes directly to the database as Apply() does,
		/// this will give you a copy of the sql that would have been run to bring the
		/// database up to date. This is useful for generating migration scripts or
		/// for environments where your application isn't allowed to make schema changes;
		/// in this scenario you can generate the sql in advance and apply it separately.
		/// </summary>
		/// <param name="context">EF Database context to search for enum references</param>
		/// <returns>SQL statements needed to update the target database</returns>
		public string GenerateMigrationSql(DbContext context)
		{
			var model = BuildModelFromContext(context);

			var dbHandler = GetDbHandler();

			var sb = new StringBuilder();
			sb.AppendLine("-- sql generated by https://github.com/timabell/ef-enum-to-lookup");
			sb.AppendLine();
			sb.Append(dbHandler.GenerateMigrationSql(model));
			return sb.ToString();
		}

		private IDbHandler GetDbHandler()
		{
			// todo: support MariaDb etc. Issue #16

			IDbHandler dbHandler;
			if (GenerateViews)
			{
				dbHandler = new SqlViewServerHandler();
			}
			else
			{
				dbHandler = new SqlServerHandler();
			}

			dbHandler.NameFieldLength = NameFieldLength;
			dbHandler.TableNamePrefix = TableNamePrefix;
			dbHandler.TableNameSuffix = TableNameSuffix;
			dbHandler.UseTransaction = UseTransaction;
			dbHandler.Schema = Schema;
			dbHandler.GenerateDescription = GenerateDescription;

			return dbHandler;
		}

		private LookupDbModel BuildModelFromContext(DbContext context)
		{
			// recurse through dbsets and references finding anything that uses an enum
			var enumReferences = FindEnumReferences(context);

			// for the list of enums generate and missing tables
			var enums = enumReferences
				.Select(r => r.EnumType)
				.Distinct()
				.OrderBy(r => r.Name)
				.ToList();

			var lookups = (
				from enm in enums
				select new LookupData
				{
					Name = enm.Name,
					NumericType = enm.GetEnumUnderlyingType(),
					EnumType = enm,
					Values = _enumParser.GetLookupValues(enm, GenerateDescription),
				}).ToList();

			var model = new LookupDbModel
			{
				Lookups = lookups,
				References = enumReferences,
			};
			return model;
		}

		private static int ExecuteSqlCommand(DbContext context, string sql, IEnumerable<SqlParameter> parameters = null)
		{
			if (parameters == null)
			{
				return context.Database.ExecuteSqlCommand(sql);
			}
			return context.Database.ExecuteSqlCommand(sql, parameters.Cast<object>().ToArray());
		}


		internal IList<EnumReference> FindEnumReferences(DbContext context)
		{
			var metadataWorkspace = ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;

			var metadataHandler = new MetadataHandler();
			return metadataHandler.FindEnumReferences(metadataWorkspace);
		}

		internal IList<PropertyInfo> FindDbSets(Type contextType)
		{
			return contextType.GetProperties()
				.Where(p => p.PropertyType.IsGenericType
										&& p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
				.ToList();
		}

		internal IList<PropertyInfo> FindEnums(Type type)
		{
			return type.GetProperties()
				.Where(p => p.PropertyType.IsEnum
										|| (p.PropertyType.IsGenericType && p.PropertyType.GenericTypeArguments.First().IsEnum))
				.ToList();
		}
	}
}
