namespace BlogManagement.Models.Entity
{
    public class BaseDbEntity
    {
        public int Id { get; set; }
        public string CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; }
        public bool IsDeleted { get; set; }
    }
}
