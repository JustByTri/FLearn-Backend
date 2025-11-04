namespace Common.DTO.Language.Response
{
    public class ProgramResponse
    {
        public Guid ProgramId { get; set; }
        public string? ProgramName { get; set; }
        public string? Description { get; set; }
        public List<LevelResponse> Levels { get; set; } = new List<LevelResponse>();
    }
    public class LevelResponse
    {
        public Guid LevelId { get; set; }
        public string? LevelName { get; set; }
        public string? Description { get; set; }
    }
}
