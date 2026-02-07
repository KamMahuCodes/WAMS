namespace WAMS.Models.ViewModels
{
	public class UserList
	{
		public string Id { get; set; } = null!;
		public string FullName { get; set; } = null!;
		public string Email { get; set; } = null!;
		public string? ManagerName { get; set; }
	}
}
