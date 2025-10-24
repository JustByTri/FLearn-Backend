using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Refund
{
    public class RefundRequestDto
    {
        public Guid RefundRequestID { get; set; }
        public Guid EnrollmentID { get; set; }
        public Guid StudentID { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public Guid ClassID { get; set; }
        public string ClassName { get; set; }
        public RefundRequestType RequestType { get; set; }
        public string RequestTypeDisplay { get; set; }
        public string BankName { get; set; }
        public string BankAccountNumber { get; set; }
        public string BankAccountHolderName { get; set; }
        public string Reason { get; set; }
  
        public RefundRequestStatus Status { get; set; }
        public string StatusDisplay { get; set; }
        public string AdminNote { get; set; }
        public decimal RefundAmount { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
