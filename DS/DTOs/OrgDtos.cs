namespace DS.DTOs
{
    public class OrgPositionDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int? ParentPositionId { get; set; }
        public bool IsSystem { get; set; }
        public List<string> Groups { get; set; } = [];
        public List<OrgPositionHolderDto> Holders { get; set; } = [];
    }

    public class OrgPositionHolderDto
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public ImageReferenceDto ProfilePicture { get; set; }
    }

    public class OrgPositionCreateDto
    {
        public string Title { get; set; }
        public int? ParentPositionId { get; set; }
        public List<string> Groups { get; set; } = [];
    }

    public class OrgPositionUpdateDto
    {
        public string Title { get; set; }
        public List<string> Groups { get; set; } = [];
    }

    public class OrgPositionMoveDto
    {
        public int? ParentPositionId { get; set; }
    }

    public class OrgPositionHolderAddDto
    {
        public string UserId { get; set; }
    }

    public class OrgInviteDto
    {
        public string Email { get; set; }
    }
}
