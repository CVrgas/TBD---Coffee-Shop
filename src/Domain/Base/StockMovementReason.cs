namespace Domain.Base;

public enum StockMovementReason
{
    Unspecified,
    Adjustment,
    PurchaseOrder,
    Correction,
    Removal,
    Reserve,
    Restore
}