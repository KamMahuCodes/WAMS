using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WAMS.Models
{
	public class EmployeeRequest
	{
		public int Id { get; set; }

		public string? EmployeeId { get; set; } = null!;

		[ForeignKey(nameof(EmployeeId))]
		public User Employee { get; set; } = null!;

		public string? ManagerId { get; set; }

		[ForeignKey(nameof(ManagerId))]
		public User? Manager { get; set; }

		[Required]
		public DateTime StartDate { get; set; }

		[Required]
		public DateTime EndDate { get; set; }

		[MaxLength(200)]
		public string? Reason { get; set; }

		public LeaveStatus Status { get; set; } = LeaveStatus.Draft;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		// Navigation
		public ICollection<ApprovalAction> ApprovalActions { get; set; } = new List<ApprovalAction>();
	}
}
