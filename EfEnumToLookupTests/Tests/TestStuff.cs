namespace EfEnumToLookupTests.Tests
{
	using System.Data.Entity;
	using System.Data.SqlClient;
	using System.Linq;
	using EfEnumToLookup.LookupGenerator;
	using Db;
	using Model;
	using NUnit.Framework;

	[TestFixture]
	public class TestStuff
	{
		[SetUp]
		public void SetUp()
		{
			// Cleanup after other test runs
			// Using setup rather than teardown to make it easier to inspect the database after running a test.
			using (var context = new MagicContext())
			{
				context.Database.Delete();
			}

			Database.SetInitializer(new TestInitializer(new EnumToLookup()));
			using (var context = new MagicContext())
			{
				var roger = new Rabbit { Name = "Roger", TehEars = Ears.Pointy };
				context.PeskyWabbits.Add(roger);
				context.SaveChanges();
			}
		}

		[Test]
		public void DoesStuff()
		{
			using (var context = new MagicContext())
			{
				var actual = context.PeskyWabbits.First();
				Assert.AreEqual("Roger", actual.Name);
				Assert.AreEqual(Ears.Pointy, actual.TehEars);
				Assert.AreEqual(1, context.PeskyWabbits.Count()); // spot unwanted re-use of db
			}
		}

		[Test]
		public void IgnoresRuntimeValues()
		{
			using (var context = new MagicContext())
			{
				const int prototypeId = (int)Ears.Prototype;
				const string sql = "select Convert(int, count(*)) from Enum_Ears where id = @id";
				var matches = context.Database.SqlQuery<int>(sql, new SqlParameter("id", prototypeId)).Single();

				Assert.AreEqual(0, matches, string.Format("Runtime only value '{1}' shouldn't be in db. Enum_Ears id {0}", prototypeId, Ears.Prototype));
			}
		}

		[Test]
		public void UsesDescriptionAttribute()
		{
			using (var context = new MagicContext())
			{
				const string sql = "select Description from Enum_Importance where id = @id";
				var description = context.Database.SqlQuery<string>(sql, new SqlParameter("id", (int)Importance.NotBovverd)).Single();

				Assert.AreEqual(Constants.BovveredDescription, description);
			}
		}
	}
}
