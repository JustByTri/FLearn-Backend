using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Auth
{
    public class GoogleLoginRequest
    {
        [Required(ErrorMessage = "IdToken is required.")]
        public string IdToken { get; set; } = string.Empty;
    }
}
