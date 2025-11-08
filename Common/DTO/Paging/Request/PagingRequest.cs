using System.ComponentModel;

namespace Common.DTO.Paging.Request
{

    public class PagingRequest
    {
        /// <summary>
        /// Số trang (>=1, mặc định 1).
        /// </summary>
        [DefaultValue(1)]
        public int Page { get; set; } = 1;

        /// <summary>
        /// Số lượng item mỗi trang (>=1, mặc định 10).
        /// </summary>
        [DefaultValue(10)]
        public int PageSize { get; set; } = 10;

   
        /// <summary>
        /// (Tùy chọn) Từ khóa tìm kiếm chung (tên, giáo viên, chương trình, level).
        /// </summary>
        public string? SearchTerm { get; set; }


        /// <summary>
        /// (Tùy chọn) Kiểu sắp xếp (mặc định: newest).
        /// <br/>- newest, created_desc (mới nhất)
        /// <br/>- oldest, created_asc (cũ nhất)
        /// <br/>- mostLearned, learners_desc (học nhiều nhất)
        /// <br/>- learners_asc (học ít nhất)
        /// <br/>- title_asc, name (tên A→Z)
        /// <br/>- title_desc, name_desc (tên Z→A)
        /// <br/>- price_asc (giá thấp→cao)
        /// <br/>- price_desc (giá cao→thấp)
        /// <br/>- rating_desc (điểm đánh giá cao→thấp)
        /// <br/>- rating_asc (điểm đánh giá thấp→cao)
        /// </summary>
        [DefaultValue("newest")]
        public string? SortBy { get; set; }

   
        /// <summary>
        /// (Tùy chọn) Lọc theo trạng thái.
        /// <br/>- Draft
        /// <br/>- PendingApproval
        /// <br/>- Published
        /// <br/>- Rejected
        /// <br/>- Archived
        /// </summary>
        public string? Status { get; set; }
     
    }
}
