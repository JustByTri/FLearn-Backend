namespace Common.DTO.Teacher
{
    public class TeacherCredentialDto
    {
        public Guid TeacherCredentialID { get; set; }
        public Guid UserID { get; set; }
        public string CredentialName { get; set; } = string.Empty;
        public string CredentialFileUrl { get; set; } = string.Empty;
        public Guid ApplicationID { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
