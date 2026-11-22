using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DS.Domain.DTOs;

namespace DS.Application.Services.BackgroundJobs
{
    public static partial class BackgroundJobArgumentMasker
    {
        private const string Bullets = "•••••";
        private const int MaxValueLength = 200;

        private static readonly string[] HiddenKeys = ["body", "html", "content", "password", "token", "secret", "code"];

        [GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}")]
        private static partial Regex EmailPattern();

        public static List<BackgroundJobArgumentDto> Mask(string parameterName, JsonElement value)
        {
            if (value.ValueKind != JsonValueKind.Object) return [MaskValue(parameterName, value)];

            return value.EnumerateObject()
                .Select(property => MaskValue(ToCamelCase(property.Name), property.Value))
                .ToList();
        }

        public static string MaskText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            return EmailPattern().Replace(text, match => MaskEmail(match.Value));
        }

        private static BackgroundJobArgumentDto MaskValue(string name, JsonElement value)
        {
            var key = name.ToLowerInvariant();

            switch (value.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return new BackgroundJobArgumentDto { Name = name, Value = "null" };
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return new BackgroundJobArgumentDto { Name = name, Value = value.GetRawText() };
                case JsonValueKind.String:
                    return MaskString(name, key, value.GetString() ?? "");
                default:
                    return new BackgroundJobArgumentDto { Name = name, Hidden = true, Size = Encoding.UTF8.GetByteCount(value.GetRawText()) };
            }
        }

        private static BackgroundJobArgumentDto MaskString(string name, string key, string text)
        {
            if (HiddenKeys.Any(key.Contains))
            {
                return new BackgroundJobArgumentDto { Name = name, Hidden = true, Size = Encoding.UTF8.GetByteCount(text) };
            }

            if (key.Contains("email"))
            {
                var maskedEmail = EmailPattern().IsMatch(text) ? MaskText(text) : MaskName(text);
                return new BackgroundJobArgumentDto { Name = name, Value = maskedEmail, Masked = true };
            }

            if (key.Contains("name"))
            {
                return new BackgroundJobArgumentDto { Name = name, Value = MaskName(text), Masked = true };
            }

            var masked = MaskText(text);
            if (masked.Length > MaxValueLength) masked = masked[..MaxValueLength] + "…";

            return new BackgroundJobArgumentDto
            {
                Name = name,
                Value = masked,
                Masked = masked != text && EmailPattern().IsMatch(text)
            };
        }

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 0) return Bullets;

            return email[0] + Bullets + email[at..];
        }

        private static string MaskName(string name)
        {
            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return string.Join(' ', words.Select(word => word[0] + Bullets));
        }

        private static string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name) || char.IsLower(name[0])) return name;

            return char.ToLowerInvariant(name[0]) + name[1..];
        }
    }
}
