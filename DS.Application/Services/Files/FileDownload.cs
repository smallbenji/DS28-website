namespace DS.Application.Services.Files
{
    public record FileDownload(Stream Content, string ContentType, bool IsPublic);
}
