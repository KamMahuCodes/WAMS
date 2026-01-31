namespace WAMS.Models.ViewModels
{
	public class Dashboard
	{
		public int PendingApprovals { get; set; }
		public int ApprovedRequests { get; set; }
		public int RejectedRequests { get; set; }

		public List<string> Months { get; set; } = new();
		public List<int> RequestsPerMonth { get; set; } = new();
	}
}
