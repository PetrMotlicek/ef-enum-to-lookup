using System;
using System.Text;

namespace EfEnumToLookup
{
	/// <summary>
	/// Helper class with static (extensions) methods supporting SQL server script generation
	/// </summary>
	public static class SqlServerGenerationHelper
	{
		/// <summary>
		/// Ensures that string value is syntactically correct for SQL script.
		/// </summary>
		/// <param name="value">Value which is checked for characters to be encoded as valid SQL script character (string).</param>
		/// <returns>Valid SQL string value, which can be used for '%returnedvalue%' string expressions.</returns>
		/// <seealso cref="SanitizeExecuteSqlString"/>
		public static string SanitizeSqlString(this string value)
		{
			return value?.Replace("'", "''");
		}

		/// <summary>
		/// Ensures that string value is syntactically correct for SQL script used withing the <c>EXECUTE ('....')</c> TSQL statement
		/// </summary>
		/// <param name="value">Value which is checked for characters to be encoded as valid SQL script character (string).</param>
		/// <returns>Valid SQL string value, which can be used for exec ('declare @stringVariable nvarchar(max)=N''%returnedvalue%'';') string expressions.</returns>
		/// <seealso cref="SanitizeSqlString"/>
		public static string SanitizeExecuteSqlString(this string value)
		{
			return value?.Replace("'", "''''");
		}

		/// <summary>
		/// Composes table name from optional parts
		/// </summary>
		/// <param name="tableName">Base table name</param>
		/// <param name="prefix">Prefix added before the <paramref name="tableName"/></param>
		/// <param name="suffix">Suffix added behind the <paramref name="tableName"/></param>
		/// <param name="schema">Optional schema where resulting table can be placed.</param>
		/// <returns></returns>
		public static string TableName(string tableName, string prefix, string suffix, string schema)
		{
			string result = $"{prefix}{tableName}{suffix}";

			if (!string.IsNullOrWhiteSpace(schema))
			{
				result = schema + "." + result;
			}

			return result;
		}

		/// <summary>
		/// Converts enumeration underlaying type to corresponding MS SQL server type.
		/// </summary>
		/// <param name="numericType">Type of underlaying enumeration type. If it is enumeration type, the underlaying type is used.</param>
		/// <returns></returns>
		public static string NumericSqlType(this Type numericType)
		{
			if (numericType.IsEnum)
			{
				numericType = numericType.GetEnumUnderlyingType();
			}

			if (numericType == typeof(byte))
			{
				return "tinyint";
			}

			if (numericType == typeof(long))
			{
				return "bigint";
			}

			return "int";
		}

		/// <summary>
		/// Appends SQL script representing adding metadata description to a database VIEW.
		/// </summary>
		/// <param name="sb"><see cref="StringBuilder"/> to which resulting SQL script is appended.</param>
		/// <param name="schema">Schema where <paramref name="viewName"/> is placed.</param>
		/// <param name="viewName">Target database VIEW name, to which <paramref name="description"/> is added.</param>
		/// <param name="description">Description of <paramref name="viewName"/></param>
		/// <param name="prefix">Optional prefix appended as starting sequence to the <paramref name="sb"/></param>
		/// <seealso cref="AppendTableMetadataDescriptionLine"/>
		/// <seealso cref="GetMetadataDescription(string, string, string, string)"/>
		public static void AppendViewMetadataDescriptionLine(this StringBuilder sb, string schema, string viewName, string description, string prefix = null)
		{
			sb.Append(prefix);
			sb.AppendLine(GetMetadataDescription(schema,viewName,"VIEW", description));
		}

		/// <summary>
		/// Appends SQL script representing adding metadata description to a DB TABLE.
		/// </summary>
		/// <param name="sb"><see cref="StringBuilder"/> to which resulting SQL script is appended.</param>
		/// <param name="schema">Schema where <paramref name="tableName"/> is placed.</param>
		/// <param name="tableName">Target database table name, to which <paramref name="description"/> is added.</param>
		/// <param name="description">Description of <paramref name="tableName"/></param>
		/// <param name="prefix">Optional prefix appended as starting sequence to the <paramref name="sb"/></param>
		/// <seealso cref="AppendViewMetadataDescriptionLine"/>
		/// <seealso cref="GetMetadataDescription(string, string, string, string)"/>
		public static void AppendTableMetadataDescriptionLine(this StringBuilder sb, string schema, string tableName, string description, string prefix = null)
		{
			sb.Append(prefix);
			sb.AppendLine(GetMetadataDescription(schema, tableName, "TABLE", description));
		}

		/// <summary>
		/// Composes SQL script representing adding metadata description to a database object.
		/// </summary>
		/// <param name="schema">Schema where <paramref name="dbObjectName"/> is placed.</param>
		/// <param name="dbObjectName">Target database object name, to which <paramref name="description"/> is added.</param>
		/// <param name="dbObjectType">Type of database object name, e.g. VIEW, TABLE.</param>
		/// <param name="description">Description of <paramref name="dbObjectName"/></param>
		/// <seealso cref="AppendViewMetadataDescriptionLine"/>
		/// <seealso cref="AppendTableMetadataDescriptionLine"/>
		public static string GetMetadataDescription(string schema, string dbObjectName, string dbObjectType, string description)
		{
			return $"exec sys.sp_addextendedproperty @name=N'MS_Description', @level0type=N'SCHEMA', @level0name=N'{schema}', @level1type=N'{dbObjectType}',@level1name=N'{dbObjectName}', @value=N'{description.SanitizeSqlString()}'";
		}

		/// <summary>
		/// Appends SQL script to create schema if it does not exists
		/// </summary>
		/// <param name="sb"><see cref="StringBuilder"/> to which resulting SQL script is appended.</param>
		/// <param name="schema">Schema name</param>
		/// <param name="prefix">Optional prefix appended as starting sequence to the <paramref name="sb"/></param>
		/// <seealso cref="CreateSchema"/>
		public static void AppendCreateSchema(this StringBuilder sb, string schema, string prefix = null)
		{
			sb.Append(prefix);
			sb.AppendLine(CreateSchema(schema));
		}

		/// <summary>
		/// Composes SQL script to create schema if it does not exists
		/// </summary>
		/// <seealso cref="AppendCreateSchema"/>
		public static string CreateSchema(string schema)
		{
			return $"IF(SCHEMA_ID('{schema}') IS NULL) exec ('CREATE SCHEMA [{schema}]');";
		}
	}
}