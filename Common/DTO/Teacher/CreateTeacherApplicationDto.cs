using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Teacher
{
    public class CreateTeacherApplicationDto
    {
        [Required(ErrorMessage = "Ngôn ngữ ứng tuyển là bắt buộc")]
        public Guid LanguageID { get; set; }
        [Required(ErrorMessage = "Động lực ứng tuyển là bắt buộc")]
        [StringLength(1000, MinimumLength = 50, ErrorMessage = "Động lực ứng tuyển phải từ 50 đến 1000 ký tự")]
        public string Motivation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ít nhất một chứng chỉ là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất một chứng chỉ")]
        public List<CreateCredentialDto> Credentials { get; set; } = new List<CreateCredentialDto>();
    }

    public class CreateCredentialDto
    {
        [Required(ErrorMessage = "Tên chứng chỉ là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên chứng chỉ không được vượt quá 200 ký tự")]
        public string CredentialName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại chứng chỉ là bắt buộc")]
        public CredentialType Type { get; set; }

        [Required(ErrorMessage = "URL file chứng chỉ là bắt buộc")]
        public string CredentialFileUrl { get; set; } = string.Empty;
    }
        public enum CredentialType
    {
        IdentityProof = 0,
        TeachingCertificate = 1,
        Other = 2
    }
}
