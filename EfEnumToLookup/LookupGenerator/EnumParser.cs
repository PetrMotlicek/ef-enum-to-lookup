namespace EfEnumToLookup.LookupGenerator
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;
	using System.Reflection;
	using System.Text.RegularExpressions;

	/// <summary>
	/// Loops through the values in an enum type and gets the ids and names
	/// for use in the generated lookup table.
	/// Will use Description attribute on enum values if available for the
	/// name, otherwise it'll use the name from code, optionally split into
	/// words.
	/// </summary>
	internal class EnumParser
	{
		public EnumParser()
		{
			// default settings
			SplitWords = true;
		}

		/// <summary>
		/// If set to true (default) enum names will have spaces inserted between
		/// PascalCase words, e.g. enum SomeValue is stored as "Some Value".
		/// </summary>
		public bool SplitWords { get; set; }

		/// <summary>
		/// Loops through the values in an enum type and gets the ids and names
		/// for use in the generated lookup table.
		/// </summary>
		/// <param name="lookup">Enum to process</param>
		/// <exception cref="System.ArgumentException">Lookup type must be an enum;lookup</exception>
		public IEnumerable<LookupValue> GetLookupValues(Type lookup)
		{
			if (!lookup.IsEnum)
			{
				throw new ArgumentException("Lookup type must be an enum", "lookup");
			}

			var values = new List<LookupValue>();
			foreach (Enum value in Enum.GetValues(lookup))
			{
				if (IsRuntimeOnly(value))
				{
					continue;
				}

				// avoid cast error for byte enums by converting to int before using a cast
				// https://github.com/timabell/ef-enum-to-lookup/issues/20
				var numericValue = Convert.ChangeType(value, typeof(int));

				values.Add(new LookupValue
				{
					Id = (int)numericValue,
					Name = EnumName(value),
					Description = EnumDescriptionValue(value)
				});
			}
			return values;
		}

		/// <summary>
		/// Gets the string to store in the lookup table for the specified
		/// enum value. Will use the DescriptionAttribute of the value
		/// if available, otherwise will use the value's name, optionally
		/// split into words.
		/// </summary>
		private string EnumName(Enum value)
		{
			var name = EnumNameValue(value).ToString();

			if (SplitWords)
			{
				return SplitCamelCase(name);
			}

			return name;
		}

		private static string SplitCamelCase(string name)
		{
			// http://stackoverflow.com/questions/773303/splitting-camelcase/25876326#25876326
			name = Regex.Replace(name, "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled);

			return name;
		}

		/// <summary>
		/// Returns the Name from DisplayAttribute for an enum value,
		/// or Name of the enum value.
		/// <example>
		/// <code>
		/// public enum Shape
		/// {
		///		[Display(Name = "Rounded", Description = "Round it is!")]
		///		Round
		/// }
		/// </code>
		/// 
		/// Will return Rounded as Name
		/// </example>
		/// </summary>
		private static string EnumNameValue(Enum value)
		{
			var enumType = value.GetType();

			// https://stackoverflow.com/questions/1799370/getting-attributes-of-enums-value/1799401#1799401
			var member = enumType.GetMember(value.ToString()).First();
			var name = member.GetCustomAttributes(typeof(DisplayAttribute)).FirstOrDefault() as DisplayAttribute;

			return name == null || string.IsNullOrWhiteSpace(name.Name) ? value.ToString() : name.Name;
		}

		/// <summary>
		/// Returns the Description from DisplayAttribute for an enum value,
		/// or null if there isn't one.
		/// /// <example>
		/// <code>
		/// public enum Shape
		/// {
		///		[Display(Name = "Rounded", Description = "Round it is!")]
		///		Round
		/// }
		/// </code>
		/// 
		/// Will return Round it is! as Description
		/// </example>
		/// </summary>
		private static string EnumDescriptionValue(Enum value)
		{
			var enumType = value.GetType();

			// https://stackoverflow.com/questions/1799370/getting-attributes-of-enums-value/1799401#1799401
			var member = enumType.GetMember(value.ToString()).First();
			var description = member.GetCustomAttributes(typeof(DisplayAttribute)).FirstOrDefault() as DisplayAttribute;

			return description == null ? null : description.Description;
		}

		private static bool IsRuntimeOnly(Enum value)
		{
			var enumType = value.GetType();

			// https://stackoverflow.com/questions/1799370/getting-attributes-of-enums-value/1799401#1799401
			var member = enumType.GetMember(value.ToString()).First();

			return member.GetCustomAttributes(typeof(RuntimeOnlyAttribute)).Any();
		}
	}
}