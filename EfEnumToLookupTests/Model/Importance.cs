namespace EfEnumToLookupTests.Model
{
	using System.ComponentModel.DataAnnotations;

	public enum Importance
	{
		Bovverd = 1,
		
		[Display(Description = Constants.AintBovveredDescription)]
		AintBovverd,
		
		[Display(Description = Constants.BovveredDescription)]
		NotBovverd
	}
}
