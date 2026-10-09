namespace DS.Website.Services
{
    public static class FileKeys
    {
        public static string Pending(int fileId) => $"pending/{fileId}";

        public static string Image(Guid publicId) => $"images/{publicId:N}.webp";
    }
}
