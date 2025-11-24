using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Payment
{
    public class WalletDto
    {
        public Guid WalletId { get; set; }

      
        public Guid? OwnerId { get; set; }

        // Tên ví (VD: Ví Admin, Ví Giáo Viên...)
        public string Name { get; set; } = string.Empty;

        // Loại chủ sở hữu (Admin, Teacher, Student...) - trả về chuỗi để Client dễ hiển thị
        public string OwnerType { get; set; } = string.Empty;

        // Các loại số dư
        public decimal TotalBalance { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal HoldBalance { get; set; }


        public string Currency { get; set; } = "VND";

        public bool Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

