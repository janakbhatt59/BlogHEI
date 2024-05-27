using BlogManagement.Models;
using BlogManagement.Models.Entity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.DBContext
{
    public class ApplicationDBContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Blog> Blog { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<SchedulerConfig> SchedulerConfigs { get; set; }
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.ProfilePicture).HasColumnType("bytea");
            });
        }
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);

        //    modelBuilder.Entity<BlogPostTag>()
        //        .HasKey(bpt => new { bpt.Id, bpt.TagId });

        //    // Define relationships
        //    modelBuilder.Entity<Blog>()
        //        .HasOne(bp => bp.User)
        //        .WithMany()
        //        .HasForeignKey(bp => bp.CreatedBy)
        //        .IsRequired()
        //        .OnDelete(DeleteBehavior.Restrict);

        //    modelBuilder.Entity<Blog>()
        //        .HasOne(bp => bp.Category)
        //        .WithMany(c => c.Blog)
        //        .HasForeignKey(bp => bp.CategoryId)
        //        .IsRequired()
        //        .OnDelete(DeleteBehavior.Restrict);

        //    modelBuilder.Entity<BlogPostTag>()
        //        .HasOne(bpt => bpt.Blog)
        //        .WithMany(bp => bp.BlogPostTags)
        //        .HasForeignKey(bpt => bpt.BlogId)
        //        .IsRequired()
        //        .OnDelete(DeleteBehavior.Cascade);

        //    modelBuilder.Entity<BlogPostTag>()
        //        .HasOne(bpt => bpt.Tag)
        //        .WithMany(t => t.BlogPostTags)
        //        .HasForeignKey(bpt => bpt.TagId)
        //        .IsRequired()
        //        .OnDelete(DeleteBehavior.Restrict);
        //}
    }
}
