using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Teacher.Response
{
    public class TeacherBankAccountDto
    {
        public Guid BankAccountId { get; set; }
        public Guid TeacherId { get; set; }
        public string BankBranch { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
