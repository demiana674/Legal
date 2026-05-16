namespace LegalMateAI.Domain.Enums
{
    public enum CancelInitiator
    {
        User = 0,
        Lawyer = 1
    }

    public enum CancelRequestStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }
}