using System.ComponentModel.DataAnnotations;

namespace WAMS.Models
{
	public class EmployeeRequest
	{
		public int Id { get; set; }

		[Required]
		public string? EmployeeId { get; set; }
		public User? Employee { get; set; }

		[Required]
		public DateTime StartDate { get; set; }

		[Required]
		public DateTime EndDate { get; set; }

		[MaxLength(200)]
		public string? Reason { get; set; }

		public LeaveStatus Status { get; set; } = LeaveStatus.Draft;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		// Navigation
		public ICollection<ApprovalAction>? ApprovalActions { get; set; }
	}
}
