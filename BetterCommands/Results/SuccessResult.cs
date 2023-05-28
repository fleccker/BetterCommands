namespace BetterCommands.Results
{
    public struct SuccessResult : IResult
    {
        public bool IsSuccess { get; }
        public object Result { get; }

        public SuccessResult(object result)
        {
            Result = result;
            IsSuccess = true;
        }
    }
}