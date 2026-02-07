namespace WAMS.Models.ViewModels
{
	public class RecentLeaveRequest
	{
		public string EmployeeName { get; set; }
		public string ManagerName { get; set; }
		public LeaveStatus Status { get; set; }
		public DateTime CreatedAt { get; set; }
		public string Reason { get; set; }
	}
}
