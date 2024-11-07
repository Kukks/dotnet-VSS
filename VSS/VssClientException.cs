using VSSProto;

namespace VSS;

public class VSSClientException : Exception
{
    public VSSClientException(ErrorResponse error) : base($"{error.ErrorCode} {error.Message}")
    {
        Error = error;
    }

    public ErrorResponse Error { get; }
}