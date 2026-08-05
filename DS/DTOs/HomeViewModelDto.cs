namespace DS.DTOs
{
    public class HomeViewModelDto
    {
        public List<HQPanelEntryDto> Shortcuts { get; set; }
    }

    public class HQPanelEntryDto
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string[] Icon { get; set; }
        public string RequiredRole { get; set; }
    }
}
