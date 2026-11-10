namespace DS.Domain.DTOs
{
    public static class SearchResultTypes
    {
        public const string Board = "board";
        public const string Group = "group";
        public const string Activity = "activity";
    }

    public class SearchResultDto
    {
        public string Type { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Url { get; set; }
    }
}
