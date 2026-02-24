namespace WAMS.Models.ViewModels
{
	public class ManagerDashboardViewModel
	{
		public int PendingTeamApprovals { get; set; }
		public int ApprovedByManager { get; set; }
		public int RejectedByManager { get; set; }

		public double ApprovalRate { get; set; }
		public double AverageDecisionHours { get; set; }

		public List<RecentDecisionViewModel>? RecentManagerActions { get; set; }
	}

}
