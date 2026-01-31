using System.ComponentModel.DataAnnotations;

namespace WAMS.Models
{
	public class ApprovalAction
	{
		public int Id { get; set; }

		[Required]
		public int LeaveRequestId { get; set; }
		public EmployeeRequest? LeaveRequest { get; set; }

		[Required]
		public string? ApproverId { get; set; }
		public User? Approver { get; set; }

		[Required]
		public ApprovalDecision Decision { get; set; }

		[MaxLength(500)]
		public string? Comment { get; set; }

		public DateTime ActionedAt { get; set; } = DateTime.UtcNow;
	}
}
