namespace WAMS.Models.ViewModels
{
	public class HRDashboardViewModel
	{
		public int PendingHRApprovals { get; set; }
		public int ApprovedByHR { get; set; }
		public int RejectedByHR { get; set; }

		public double ApprovalRate { get; set; }
		public double AverageDecisionHours { get; set; }

		public List<RecentDecisionViewModel>? RecentHRDecisions { get; set; }
	}

}
