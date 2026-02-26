namespace Platform.Domain.Enums;

public enum SubmissionStatus
{
    Pending,
    Validating,
    Accepted,
    Rejected
}

public enum EnrollmentStatus
{
    Active,
    Completed,
    Cancelled
}

public enum OrderStatus
{
    Created,
    Pending,
    Paid,
    Failed,
    Refunded
}

public enum PaymentStatus
{
    Created,
    Processing,
    Completed,
    Failed,
    Cancelled
}

public enum CertificateStatus
{
    Pending,
    Issued,
    Revoked
}
