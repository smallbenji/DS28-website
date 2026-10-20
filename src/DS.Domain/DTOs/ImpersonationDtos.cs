namespace DS.Domain.DTOs
{
    public class ImpersonationGroupDto
    {
        public string Name { get; set; }
        public string Title { get; set; }
    }

    public class StartImpersonationDto
    {
        public string Group { get; set; }
    }
}