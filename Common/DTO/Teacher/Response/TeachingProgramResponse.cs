namespace Common.DTO.Teacher.Response
{
    public class TeachingProgramResponse
    {
        public Guid ProgramId { get; set; }
        public string? ProgramName { get; set; }
        public Guid LevelId { get; set; }
        public string? LevelName { get; set; }
    }
}
