namespace Balance.ViewModels
{
    public class PaginationInfo
    {
        public string SearchTerm { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int[] PageSizes { get; set; }
        public bool HasNext { get; set; }
        public bool HasPrevious { get; set; }
    }
}