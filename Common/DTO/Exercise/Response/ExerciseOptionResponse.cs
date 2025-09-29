namespace Common.DTO.Exercise.Response
{
    public class ExerciseOptionResponse
    {
        public Guid OptionID { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int Position { get; set; }
    }
}
