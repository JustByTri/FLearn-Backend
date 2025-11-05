namespace Common.DTO.Teacher
{
 public class TeacherAssignmentDto
 {
 public Guid ProgramAssignmentId { get; set; }
 public Guid ProgramId { get; set; }
 public string? ProgramName { get; set; }
 public Guid LevelId { get; set; }
 public string? LevelName { get; set; }
 public int OrderIndex { get; set; }
 public string? LanguageName { get; set; }
 public string? LanguageCode { get; set; }
 public bool Active { get; set; }
 }
}
