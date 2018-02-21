using System;
using System.Data.Entity;

namespace EfEnumToLookup.LookupGenerator
{
	/// <summary>
	/// 
	/// </summary>
	public interface IEnumToLookup
	{
		/// <summary>
		/// Create any missing lookup tables,
		/// enforce values in the lookup tables
		/// by way of a T-SQL MERGE
		/// </summary>
		/// <param name="context">EF Database context to search for enum references,
		///  context.Database.ExecuteSqlCommand() is used to apply changes.</param>
		void Apply(DbContext context);

		/// <summary>
		/// If set to true (default) enum names will have spaces inserted between
		/// PascalCase words, e.g. enum SomeValue is stored as "Some Value".
		/// </summary>
		bool SplitWords { get; set; }

		/// <summary>
		/// The size of the Name field that will be added to the generated lookup tables.
		/// Adjust to suit your data if required, defaults to 255.
		/// </summary>
		int NameFieldLength { get; set; }

		/// <summary>
		/// Prefix to add to all the generated tables to separate help group them together
		/// and make them stand out as different from other tables.
		/// Defaults to "Enum_" set to null or "" to not have any prefix.
		/// </summary>
		string TableNamePrefix { get; set; }

		/// <summary>
		/// Suffix to add to all the generated tables to separate help group them together
		/// and make them stand out as different from other tables.
		/// Defaults to "" set to null or "" to not have any suffix.
		/// </summary>
		string TableNameSuffix { get; set; }

		/// <summary>
		/// Whether to run the changes inside a database transaction.
		/// </summary>
		bool UseTransaction { get; set; }

		/// <summary>
		/// DB Schema for resulting generated DB objects
		/// </summary>
		string Schema { get; set; }

		/// <summary>
		/// Indicates if description fields should be generated with values read for each enum member decorated by <see cref="System.ComponentModel.DescriptionAttribute"/>
		/// </summary>
		bool GenerateDescription { get; set; }

		/// <summary>
		/// Indicates type of generated DB objects
		/// </summary>
		/// <remarks>
		/// <para>When <see keyword="true"/> target DB objects are generated as tables with foreign key references in referencing tables.</para>
		/// <para>When <see keyword="false"/> target DB objects are generated as views without foreign key references in referencing tables.</para>
		/// <para>Default values is <see keyword="false"/></para>
		/// </remarks>
		bool GenerateViews { get; set; }
	}
}
