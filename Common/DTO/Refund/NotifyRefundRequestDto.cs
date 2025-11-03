using System;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Refund
{
    /// <summary>
    /// DTO ?? Admin g?i email thông báo h?c viên c?n làm ??n hoàn ti?n
    /// </summary>
    public class NotifyRefundRequestDto
    {
     [Required(ErrorMessage = "ID h?c viên là b?t bu?c")]
        public Guid StudentId { get; set; }

     [Required(ErrorMessage = "ID l?p h?c là b?t bu?c")]
    public Guid ClassId { get; set; }

        [Required(ErrorMessage = "Tên l?p h?c là b?t bu?c")]
        public string ClassName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Th?i gian b?t ??u l?p là b?t bu?c")]
     public DateTime ClassStartDateTime { get; set; }

        [StringLength(500, ErrorMessage = "Lý do không ???c v??t quá 500 ký t?")]
      public string? Reason { get; set; }
    }
}
