using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WAMS.Models
{
	public class User : IdentityUser
	{
		[Required]
		[MaxLength(50)]
		public string FullName { get; set; } = null!;

		// Self-referencing Manager relationship
		public string? ManagerId { get; set; }

		[ForeignKey(nameof(ManagerId))]
		public User? Manager { get; set; }

		public string? Department { get; set; }
		public int AnnualLeaveBalance { get; set; } = 20;

		// Navigation
		public ICollection<EmployeeRequest> LeaveRequests { get; set; } = new List<EmployeeRequest>();
		public ICollection<ApprovalAction> ApprovalActions { get; set; } = new List<ApprovalAction>();
	}
}
