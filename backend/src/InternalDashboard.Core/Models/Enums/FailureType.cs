namespace InternalDashboard.Core.Models.Enums;

public enum FailureType
{
    Timeout               = 1,
    AuthenticationError   = 2,
    RateLimitExceeded     = 3,
    NetworkError          = 4,
    DataValidationError   = 5,
    ThirdPartyServiceDown = 6
}
