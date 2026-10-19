namespace DS.Application.Services.Boards
{
    public enum BoardOutcome
    {
        Success,
        Invalid,
        NotFound,
        Forbidden
    }

    public record BoardResult(BoardOutcome Outcome, object Value = null);
}
