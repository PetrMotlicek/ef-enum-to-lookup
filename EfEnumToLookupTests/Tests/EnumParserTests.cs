namespace EfEnumToLookupTests.Tests
{
	using System.ComponentModel.DataAnnotations;
	using System.Linq;
	using EfEnumToLookup.LookupGenerator;
	using NUnit.Framework;

	[TestFixture]
	public class EnumParserTests
	{
		[Test]
		public void ReadsName()
		{
			// arrange
			var parser = new EnumParser { SplitWords = false };

			// act
			var lookupValue = parser.GetLookupValues(typeof(BareEnum)).Single();

			// assert
			Assert.AreEqual("FooBar", lookupValue.Name);
		}

		[Test]
		public void ReadsSplitName()
		{
			// arrange
			var parser = new EnumParser { SplitWords = true };

			// act
			var lookupValue = parser.GetLookupValues(typeof(BareEnum)).Single();

			// assert
			Assert.AreEqual("Foo Bar", lookupValue.Name);
		}

		[Test]
		public void ReadsByteEnum()
		{
			// arrange
			var parser = new EnumParser { SplitWords = false };

			// act
			var lookupValue = parser.GetLookupValues(typeof(ByteEnum)).Single();

			// assert
			Assert.AreEqual("FooBar", lookupValue.Name);
		}

		[Test]
		public void ReadsDecoratedName()
		{
			// arrange
			var parser = new EnumParser { SplitWords = true };

			// act
			var lookupValue = parser.GetLookupValues(typeof(NameDecoratedEnum)).Single();

			// assert
			Assert.AreEqual("Wide boy", lookupValue.Name);
		}

		[Test]
		public void ReadsDecoratedDescription()
		{
			// arrange
			var parser = new EnumParser { SplitWords = true };

			// act
			var lookupValue = parser.GetLookupValues(typeof(DescriptionDecoratedEnum)).Single();

			// assert
			Assert.AreEqual("Wide boy description", lookupValue.Description);
		}

		[Test]
		public void ReadsDecoratedNameAndDescription()
		{
			// arrange
			var parser = new EnumParser { SplitWords = true };

			// act
			var lookupValue = parser.GetLookupValues(typeof(NameAndDescriptionDecoratedEnum)).Single();

			// assert
			Assert.AreEqual("Wide boy", lookupValue.Name);
			Assert.AreEqual("Wide boy description", lookupValue.Description);
		}

		[Test]
		public void ReadsDecoratedNameAndNullDescription()
		{
			// arrange
			var parser = new EnumParser { SplitWords = false };

			// act
			var lookupValue = parser.GetLookupValues(typeof(NameDecoratedEnum)).Single();

			// assert
			Assert.AreEqual("Wide boy", lookupValue.Name);
			Assert.AreEqual(null, lookupValue.Description);
		}

		private enum BareEnum
		{
			// ReSharper disable once UnusedMember.Local
			// used by test suite
			FooBar
		}

		private enum ByteEnum : byte
		{
			// ReSharper disable once UnusedMember.Local
			// used by test suite
			FooBar
		}

		private enum NameDecoratedEnum
		{
			// ReSharper disable once UnusedMember.Local
			// used by test suite
			[Display(Name = "Wide boy")]
			FooBar,
		}

		private enum DescriptionDecoratedEnum
		{
			// ReSharper disable once UnusedMember.Local
			// used by test suite
			[Display(Description = "Wide boy description")]
			FooBar,
		}
		private enum NameAndDescriptionDecoratedEnum
		{
			// ReSharper disable once UnusedMember.Local
			// used by test suite
			[Display(Name = "Wide boy", Description = "Wide boy description")]
			FooBar,
		}
	}
}
