using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WAMS.Models;

namespace WAMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<User>? Users { get; set; }
        public DbSet<EmployeeRequest>? LeaveRequests { get; set; }
        public DbSet<ApprovalAction>? ApprovalActions { get; set; }
		public DbSet<Notification> Notifications { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EmployeeRequest>()
                .HasOne(l => l.Employee)
                .WithMany(u => u.LeaveRequests)
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApprovalAction>()
                .HasOne(a => a.Approver)
                .WithMany(u => u.ApprovalActions)
                .HasForeignKey(a => a.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<User>()
		        .HasOne(u => u.Manager)
		        .WithMany()
		        .HasForeignKey(u => u.ManagerId)
		        .OnDelete(DeleteBehavior.Restrict);
		}
    }
}
