namespace WAMS.Models.ViewModels
{
	public class Dashboard
	{
		// Metrics
		public int PendingApprovals { get; set; }
		public int ApprovedRequests { get; set; }
		public int RejectedRequests { get; set; }
		public int Users { get; set; }

		// Charts
		public List<string> Months { get; set; } = new();
		public List<int> RequestsPerMonth { get; set; } = new();

		// Table
		public List<RecentLeaveRequest> RecentRequests { get; set; } = new();
	}

}
