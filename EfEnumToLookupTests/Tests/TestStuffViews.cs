using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using EfEnumToLookup.LookupGenerator;
using EfEnumToLookupTests.Db;
using EfEnumToLookupTests.Model;
using NUnit.Framework;

namespace EfEnumToLookupTests.Tests
{
	[TestFixture]
	public class TestStuffViews
	{
		private void Init(bool generateViews)
		{
			// Cleanup after other test runs
			// Using setup rather than teardown to make it easier to inspect the database after running a test.
			using (var context = new MagicContext())
			{
				if (context.Database.Exists())
				{
					context.Database.Delete();
				}
			}

			Database.SetInitializer(new TestInitializer(new EnumToLookup
			{
				GenerateViews = generateViews,
				GenerateDescription = true,
				TableNamePrefix = null,
				Schema = "enum",
				SplitWords = false,
			}));
		}

		[Test]
		public void CheckConstraintPositiveTest()
		{
			Init(true);

			
			using (var context = new MagicContext())
			{
				var rabit1 = new Rabbit
				{
					BodyFur = Fur.Blue,
					Name = "Test Rabbit 1",
					Pedigree = Pedigree.Dubious,
					TehEars = Ears.Floppy
				};

				var rabit2 = new Rabbit
				{
					BodyFur = Fur.Blue,
					Name = "Test Rabbit 2",
					Pedigree = Pedigree.Dubious,
					TehEars = Ears.Pointy
				};


				context.PeskyWabbits.Add(rabit1);
				context.PeskyWabbits.Add(rabit2);
				context.SaveChanges();
			}
		}

		[Test]
		public void CheckConstraintNegativeTest()
		{
			Init(true);


			using (var context = new MagicContext())
			{
				var rabit1 = new Rabbit
				{
					BodyFur = Fur.Blue,
					Name = "Test Rabbit 1",
					Pedigree = Pedigree.Dubious,
					TehEars = Ears.Prototype //this should be ignored
				};


				context.PeskyWabbits.Add(rabit1);
				Assert.Catch<DbUpdateException>(() => context.SaveChanges());
			}
		}


		[Test, TestCase(false), TestCase(true)]
		public void UsesDescriptionAttribute(bool generateViews)
		{
			Init(generateViews);

			using (var context = new MagicContext())
			{
				const string sql = "select @name = Name, @description = Description from enum.Importance where id = @id";
				var idParam = new SqlParameter("id", (int)Importance.NotBovverd);
				var outParamDescription = new SqlParameter("description", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };
				var outParamName = new SqlParameter("name", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output };
				context.Database.ExecuteSqlCommand(sql, idParam, outParamName, outParamDescription);

				var actualName = outParamName.Value;
				var actualDescription = outParamDescription.Value;

				Assert.AreEqual(Constants.BovveredDisplay, actualDescription);
				Assert.AreEqual(Importance.NotBovverd.ToString(), actualName);
			}
		}
	}
}