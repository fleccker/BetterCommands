using System;

namespace BetterCommands.Results
{
    public struct ErrorResult : IResult
    {
        public bool IsSuccess { get; }
        public string Reason { get; }
        public Exception Exception { get; }
        public object Result { get; }

        public ErrorResult(string reason, Exception exception = null)
        {
            Reason = reason;
            Exception = exception;
            Result = null;
            IsSuccess = false;
        }
    }
}