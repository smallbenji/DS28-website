namespace DS.Application.Services
{
    public enum ServiceOutcome
    {
        Success,
        Invalid,
        NotFound,
        Forbidden,
        Conflict
    }

    public record ServiceResult(ServiceOutcome Outcome, object Error = null);

    public record ServiceResult<T>(ServiceOutcome Outcome, T Value = default, object Error = null)
    {
        public static implicit operator ServiceResult<T>(ServiceResult result)
        {
            return new ServiceResult<T>(result.Outcome, default, result.Error);
        }
    }
}
