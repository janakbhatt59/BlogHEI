using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogManagement.Models.Entity
{
    public class Blog : BaseDbEntity
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public byte[]? BlogPhoto { get; set; }
        public DateTime? PublishedAt { get; set; }
        public bool IsDraft { get; set; } = true;
        [ForeignKey("CreatedBy")]
        public ApplicationUser User { get; set; }
        [DisplayName("Category")]
        public int CategoryId { get; set; }
        public Category Category { get; set; }

        //public ICollection<BlogPostTag> BlogPostTags { get; set; }
    }
}
