namespace DS.Models;

public enum StoredFileStatus
{
    Pending,
    Processing,
    Ready,
    Failed,
}

public enum FilePurpose
{
    CatalogImage,
    ProfilePicture,
}

public class StoredFile : IAuditableEntity, ISoftDeleteable
{
    public int Id { get; set; }
    public Guid PublicId { get; set; }
    public string OriginalName { get; set; }
    public string ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public string Sha256 { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string StorageKey { get; set; }
    public StoredFileStatus Status { get; set; }
    public string Error { get; set; }
    public FilePurpose Purpose { get; set; }
    public bool IsPublic { get; set; }
    public string UploadedByUserId { get; set; }
    public User UploadedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
