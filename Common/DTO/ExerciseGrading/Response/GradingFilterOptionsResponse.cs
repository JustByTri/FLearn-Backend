namespace Common.DTO.ExerciseGrading.Response
{
    public class GradingFilterOptionsResponse
    {
        public List<FilterOption> Courses { get; set; } = new();
        public List<FilterOption> Exercises { get; set; } = new();
        public List<string> Statuses { get; set; } = new() { "Assigned", "Returned", "Expired" };
    }
    public class FilterOption
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
