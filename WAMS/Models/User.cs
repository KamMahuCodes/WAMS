using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;


namespace WAMS.Models
{
    public class User : IdentityUser
    {
		[Required]
		[MaxLength(50)]
        public string? FullName { get; set; }
		public string? ManagerId { get; set; }
		public User? Manager { get; set; }


		// Navigation
		public ICollection<EmployeeRequest>? LeaveRequests { get; set; }
        public ICollection<ApprovalAction>? ApprovalActions { get; set; }
	}
}
