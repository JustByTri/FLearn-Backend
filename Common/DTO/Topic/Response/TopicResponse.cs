namespace Common.DTO.Topic.Response
{
    public class TopicResponse
    {
        public Guid TopicId { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public string TopicDescription { get; set; } = string.Empty;
        public string ContextPrompt { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool Status { get; set; }
    }
}
