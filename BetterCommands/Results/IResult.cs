namespace BetterCommands.Results
{
    public interface IResult
    {
        bool IsSuccess { get; }

        object Result { get; }
    }
}