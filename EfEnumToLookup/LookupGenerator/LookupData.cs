using System;
using System.Diagnostics;

namespace EfEnumToLookup.LookupGenerator
{
	using System.Collections.Generic;

	[DebuggerDisplay("LookupData {Name}")]
	internal class LookupData
	{
		public string Name { get; set; }
		public ICollection<LookupValue> Values { get; set; }
		public Type NumericType { get; set; }
		public Type EnumType { get; set; }
	}
}
