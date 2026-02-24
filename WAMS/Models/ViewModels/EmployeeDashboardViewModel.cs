namespace WAMS.Models.ViewModels
{
	public class EmployeeDashboardViewModel
	{
		public int MyPendingRequests { get; set; }
		public int MyApprovedRequests { get; set; }
		public int MyRejectedRequests { get; set; }

		public double ApprovalRate { get; set; }
		public double AverageDecisionHours { get; set; }

		public List<RecentDecisionViewModel>? RecentRequests { get; set; }
	}

}
