using BlogManagement.Models.Entity;
using System.ComponentModel;

namespace BlogManagement.Models.ViewModel
{
    public class BlogVM
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int CategoryId { get; set; }
        [DisplayName("Blog Photo")]
        public string? BlogPhotoBase64 { get; set; }
        public DateTime? PublishedAt { get; set; }
        public Category? Category { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
