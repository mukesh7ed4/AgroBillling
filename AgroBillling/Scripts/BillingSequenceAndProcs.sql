/*
  BILLING — sequence + optional stored procedure (run once on your AgroBilling database).

  The app now reserves bill numbers via:
    UPDATE dbo.Shops ... SET CurrentBillSequence = CurrentBillSequence + 1
  followed by a read (no EF SaveChanges on Shops).

  IMPORTANT — legacy script AgroBilling.sql defines sp_GetNextBillNumber with its own
  BEGIN TRANSACTION / COMMIT. Do NOT call that proc from inside an EF Core transaction;
  it will commit independently and break atomicity. Use the proc below or the C# path only.

  Optional: single round-trip (same semantics as C# two-step). Caller owns the transaction.
*/

IF OBJECT_ID(N'dbo.sp_ReserveNextBillNumber', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ReserveNextBillNumber;
GO

CREATE PROCEDURE dbo.sp_ReserveNextBillNumber
    @ShopID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE dbo.Shops WITH (UPDLOCK, ROWLOCK)
    SET CurrentBillSequence = CurrentBillSequence + 1
    WHERE ShopID = @ShopID;

    IF @@ROWCOUNT = 0
        SELECT CAST(1 AS INT) AS NextBillNo;
    ELSE
        SELECT CAST(BillStartNumber + CurrentBillSequence - 1 AS INT) AS NextBillNo
        FROM dbo.Shops
        WHERE ShopID = @ShopID;
END
GO

/* Optional: index to speed up bill line lookups by bill (if missing) */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_BillItems_BillID_ProductID' AND object_id = OBJECT_ID(N'dbo.BillItems'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BillItems_BillID_ProductID
    ON dbo.BillItems (BillID)
    INCLUDE (ProductID, Quantity);
END
GO
