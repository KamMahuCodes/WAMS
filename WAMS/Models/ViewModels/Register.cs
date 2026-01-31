using System.ComponentModel.DataAnnotations;

namespace WAMS.ViewModels
{
	public class RegisterViewModel
	{
		[Required]
		[EmailAddress]
		public string? Email { get; set; }

		[Required]
		[Display(Name = "Full Name")]
		public string? FullName { get; set; }

		[Required]
		[DataType(DataType.Password)]
		[MinLength(6)]
		public string? Password { get; set; }

		[Required]
		[DataType(DataType.Password)]
		[Compare("Password")]
		[Display(Name = "Confirm Password")]
		public string? ConfirmPassword { get; set; }
	}
}
