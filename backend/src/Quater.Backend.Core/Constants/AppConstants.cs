namespace Quater.Backend.Core.Constants;

public static class AppConstants
{
    public static class Invitations
    {
        public const int TokenLengthBytes = 32;
        public const int ExpirationDays = 7;
        public const int MaxPendingInvitationsPerUser = 10;
    }
}
