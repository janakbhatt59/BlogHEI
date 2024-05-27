using System.ComponentModel;

namespace BlogManagement.Models.Entity
{
    public class Category : BaseDbEntity
    {
        public int Id { get; set; }
        [DisplayName("Category")]
        public string Name { get; set; }

        public ICollection<Blog> Blog { get; set; }
    }
}
