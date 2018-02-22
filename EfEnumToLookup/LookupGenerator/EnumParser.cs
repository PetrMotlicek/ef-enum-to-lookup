using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace EfEnumToLookup.LookupGenerator
{
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
		/// <param name="getDescriptions">Descriptions of enumeration members should be got out of <see cref="DescriptionAttribute"/> got, because it will be used for DB description column.</param>
		/// <exception cref="System.ArgumentException">Lookup type must be an enum;lookup</exception>
		public ICollection<LookupValue> GetLookupValues(Type lookup, bool getDescriptions = false)
		{
			if (!lookup.IsEnum)
			{
				throw new ArgumentException("Lookup type must be an enum", "lookup");
			}

			var namesWithDescription = !getDescriptions;

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

				var lookupValue = new LookupValue
				{
					Id = (int)numericValue,
					Name = EnumName(value, !getDescriptions),
				};

				if (getDescriptions)
				{
					lookupValue.Description = EnumParser.EnumDescriptionValue(value);
				}

				values.Add(lookupValue);
			}
			return values;
		}

		/// <summary>
		/// Gets the string to store in the lookup table for the specified
		/// enum value. Will use the DescriptionAttribute of the value
		/// if available, otherwise will use the value's name, optionally
		/// split into words.
		/// </summary>
		private string EnumName(Enum value, bool preferDescription)
		{
			string name;

			if (preferDescription)
			{
				name = EnumDescriptionValue(value) ?? value.ToString();
			}
			else
			{
				name = value.ToString();
			}
			

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
		/// Returns the value of the DescriptionAttribute for an enum value,
		/// or null if there isn't one.
		/// </summary>
		private static string EnumDescriptionValue(Enum value)
		{
			var enumType = value.GetType();

			// https://stackoverflow.com/questions/1799370/getting-attributes-of-enums-value/1799401#1799401
			var member = enumType.GetMember(value.ToString()).First();
			DescriptionAttribute description = (DescriptionAttribute)member.GetCustomAttributes(typeof(DescriptionAttribute)).FirstOrDefault();

		    if (description != null)
		    {
		        return description.Description;
		    }

		    DisplayAttribute displayAttribute = (DisplayAttribute)member.GetCustomAttributes(typeof(DisplayAttribute)).FirstOrDefault();

			if (displayAttribute != null)
			{
				return displayAttribute.ToString();
			}

			return null;
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