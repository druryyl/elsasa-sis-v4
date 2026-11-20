namespace ElsasaSis.Modules.Fin.Domain.Model;

public static class ChargeFrequencies
{
    public const string Monthly = "MONTHLY";
    public const string Semester = "SEMESTER";
    public const string Yearly = "YEARLY";
    public const string OneTime = "ONE_TIME";

    private static readonly HashSet<string> ValidFrequencies = new(StringComparer.OrdinalIgnoreCase)
    {
        Monthly,
        Semester,
        Yearly,
        OneTime
    };

    public static bool IsValid(string? frequency) =>
        !string.IsNullOrWhiteSpace(frequency) && ValidFrequencies.Contains(frequency.Trim());
}

public static class ChargeTypeStatuses
{
    public const string Draft = "DRAFT";
    public const string Active = "ACTIVE";

    public static bool IsValid(string? status) =>
        string.Equals(status, Draft, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, Active, StringComparison.OrdinalIgnoreCase);
}

public static class DiscountTypes
{
    public const string FixedAmount = "FIXED_AMOUNT";
    public const string Percentage = "PERCENTAGE";

    public static bool IsValid(string? type) =>
        string.Equals(type, FixedAmount, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(type, Percentage, StringComparison.OrdinalIgnoreCase);
}

public static class ChargeStatuses
{
    public const string Unpaid = "UNPAID";
    public const string PartiallyPaid = "PARTIALLY_PAID";
    public const string Paid = "PAID";
    public const string Cancelled = "CANCELLED";

    public static bool IsValid(string? status) =>
        string.Equals(status, Unpaid, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, PartiallyPaid, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, Paid, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, Cancelled, StringComparison.OrdinalIgnoreCase);
}

public static class PaymentMethods
{
    public const string Cash = "CASH";
    public const string BankTransfer = "BANK_TRANSFER";

    public static bool IsValid(string? method) =>
        string.Equals(method, Cash, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(method, BankTransfer, StringComparison.OrdinalIgnoreCase);
}
