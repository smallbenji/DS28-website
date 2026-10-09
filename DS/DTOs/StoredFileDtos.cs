using DS.Models;

namespace DS.DTOs
{
    public class ImageReferenceDto
    {
        public ImageReferenceDto() { }

        public ImageReferenceDto(StoredFile model)
        {
            PublicId = model.PublicId;
            Url = StoredFileDto.BuildUrl(model);
        }

        public Guid PublicId { get; set; }
        public string Url { get; set; }
    }

    public class UpdateProfilePictureDto
    {
        public ImageReferenceDto Image { get; set; }
    }

    public class StoredFileDto
    {
        public StoredFileDto() { }

        public StoredFileDto(StoredFile model)
        {
            Id = model.Id;
            PublicId = model.PublicId;
            OriginalName = model.OriginalName;
            ContentType = model.ContentType;
            Status = model.Status.ToString();
            Purpose = model.Purpose.ToString();
            Width = model.Width;
            Height = model.Height;
            Url = model.Status == StoredFileStatus.Ready ? BuildUrl(model) : null;
        }

        public int Id { get; set; }
        public Guid PublicId { get; set; }
        public string OriginalName { get; set; }
        public string ContentType { get; set; }
        public string Status { get; set; }
        public string Purpose { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string Url { get; set; }

        public static string BuildUrl(StoredFile model)
        {
            return model.IsPublic
                ? $"/api/v1/files/public/{model.PublicId}"
                : $"/api/v1/files/{model.PublicId}";
        }
    }
}
