namespace DS.DTOs
{
    public class AdminViewModelDto
    {
        public List<AdminSectionDto> Sections { get; set; }
        public List<SignupProgressDto> SignupProgress { get; set; }
    }

    public class AdminSectionDto
    {
        public string Title { get; set; }
        public string Icon { get; set; }
        public List<HQPanelEntryDto> Entries { get; set; }
    }

    public class SignupProgressDto
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public int Current { get; set; }
        public int Target { get; set; }
        public string Status { get; set; }
    }
}
