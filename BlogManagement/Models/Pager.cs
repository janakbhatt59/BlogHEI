namespace BlogManagement.Models
{
    public class Pager
    {
        public int PageNo { get; set; } = 1;
        public int ItemsPerPage { get; set; } = 10;
        public int PagePerDisplay { get; set; } = 5;
        public int TotalNextPages { get; set; }
        public int TotalRecords { get; set; }
    }
    public class PagedDataItem<T>
    {
        public IEnumerable<T> Data { get; set; }

        public Pager Pager { get; set; }
    }
}
