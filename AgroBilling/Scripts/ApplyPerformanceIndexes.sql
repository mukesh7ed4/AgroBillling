/*
  Run once on SQL Server (SSMS) against your AgroBilling database.
  Skips creation if index name already exists.
*/

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Bills_ShopID_Date_Status' AND object_id = OBJECT_ID(N'dbo.Bills'))
BEGIN
  CREATE NONCLUSTERED INDEX IX_Bills_ShopID_Date_Status
  ON dbo.Bills (ShopID, BillDate DESC, PaymentStatus)
  INCLUDE (CustomerID, TotalAmount, AmountPaid, AmountPending, BillNumber);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Customers_ShopID_Name' AND object_id = OBJECT_ID(N'dbo.Customers'))
BEGIN
  CREATE NONCLUSTERED INDEX IX_Customers_ShopID_Name
  ON dbo.Customers (ShopID, FullName)
  INCLUDE (MobileNumber, Village, District);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Expenses_ShopID_Date' AND object_id = OBJECT_ID(N'dbo.Expenses'))
BEGIN
  CREATE NONCLUSTERED INDEX IX_Expenses_ShopID_Date
  ON dbo.Expenses (ShopID, ExpenseDate DESC)
  INCLUDE (Amount, CategoryID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseOrders_ShopID_Date' AND object_id = OBJECT_ID(N'dbo.PurchaseOrders'))
BEGIN
  CREATE NONCLUSTERED INDEX IX_PurchaseOrders_ShopID_Date
  ON dbo.PurchaseOrders (ShopID, PurchaseDate DESC)
  INCLUDE (SupplierID, NetPayable, AmountPaid, PaymentStatus);
END
GO
