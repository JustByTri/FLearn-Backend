using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Teacher
{
    public class TeacherApplicationFormDto
    {
        [Required(ErrorMessage = "Ngôn ngữ ứng tuyển là bắt buộc")]
        public Guid LanguageID { get; set; }

        [Required(ErrorMessage = "Động lực ứng tuyển là bắt buộc")]
        [StringLength(1000, ErrorMessage = "Động lực không được vượt quá 1000 ký tự")]
        public string Motivation { get; set; }

        [Required(ErrorMessage = "Ít nhất một file chứng chỉ là bắt buộc")]
        public List<IFormFile> CredentialFiles { get; set; } = new List<IFormFile>();

        [Required(ErrorMessage = "Tên chứng chỉ là bắt buộc")]
        public List<string> CredentialNames { get; set; } = new List<string>();

        [Required(ErrorMessage = "Loại chứng chỉ là bắt buộc")]
        public List<int> CredentialTypes { get; set; } = new List<int>();
    }
}
