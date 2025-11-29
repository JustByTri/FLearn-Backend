using System.ComponentModel;

namespace Common.DTO.Paging.Request
{
    public class PaginationParams
    {
        private const int MaxPageSize = 50;
        private const int DefaultPageSize = 10;
        private const int DefaultPage = 1;

        private int _page = DefaultPage;
        private int _pageSize = DefaultPageSize;

        [DefaultValue(DefaultPage)]
        public int PageNumber
        {
            get => _page;
            set => _page = value < 1 ? DefaultPage : value;
        }

        [DefaultValue(DefaultPageSize)]
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (value < 1) _pageSize = DefaultPageSize;
                else if (value > MaxPageSize) _pageSize = MaxPageSize;
                else _pageSize = value;
            }
        }
        public string? Sort { get; set; }
        public int? Rating { get; set; }
    }
}
