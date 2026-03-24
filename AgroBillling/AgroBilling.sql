-- ============================================================
--  AGROBILLING SYSTEM — COMPLETE DATABASE SCHEMA
--  Database: SQL Server Express (Free Tier)
--  Architecture: Multi-Tenant (one DB, tenant isolation by ShopID)
--  Created for: Mukesh — AgroBilling SaaS
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'AgroBillingDB')
    CREATE DATABASE AgroBillingDB;
GO

USE AgroBillingDB;
GO

-- ============================================================
--  SECTION 1: ADMIN & SUBSCRIPTION MANAGEMENT
-- ============================================================

-- Admin users (your login — platform owner)
CREATE TABLE AdminUsers (
    AdminID         INT IDENTITY(1,1) PRIMARY KEY,
    FullName        NVARCHAR(100)   NOT NULL,
    Email           NVARCHAR(150)   NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(256)   NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE()
);

-- Subscription plans
CREATE TABLE SubscriptionPlans (
    PlanID          INT IDENTITY(1,1) PRIMARY KEY,
    PlanName        NVARCHAR(100)   NOT NULL,   -- e.g. Monthly, Yearly, Trial
    DurationDays    INT             NOT NULL,   -- 30, 365, 30 (trial)
    Price           DECIMAL(10,2)   NOT NULL,
    IsTrial         BIT             NOT NULL DEFAULT 0,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE()
);

-- Insert default plans
INSERT INTO SubscriptionPlans (PlanName, DurationDays, Price, IsTrial) VALUES
('1 Month Free Trial', 30,  0.00, 1),
('Monthly',            30,  299.00, 0),
('Yearly',             365, 2999.00, 0);

-- Shops (shopkeeper tenants)
CREATE TABLE Shops (
    ShopID              INT IDENTITY(1,1) PRIMARY KEY,
    OwnerName           NVARCHAR(100)   NOT NULL,
    ShopName            NVARCHAR(150)   NOT NULL,
    MobileNumber        NVARCHAR(15)    NOT NULL,
    AlternateMobile     NVARCHAR(15)    NULL,
    Email               NVARCHAR(150)   NULL,
    Address             NVARCHAR(300)   NOT NULL,
    City                NVARCHAR(100)   NOT NULL,
    State               NVARCHAR(100)   NOT NULL DEFAULT 'Haryana',
    PinCode             NVARCHAR(10)    NOT NULL,
    GSTNumber           NVARCHAR(20)    NULL,       -- optional, some shops not registered
    GSTPercent          DECIMAL(5,2)    NOT NULL DEFAULT 18.00,  -- shopkeeper sets this
    BillStartNumber     INT             NOT NULL DEFAULT 1,      -- physical bill se aage start
    CurrentBillSequence INT             NOT NULL DEFAULT 0,      -- auto increments
    PasswordHash        NVARCHAR(256)   NOT NULL,
    LogoPath            NVARCHAR(300)   NULL,
    IsActive            BIT             NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
    CreatedByAdminID    INT             NOT NULL,
    FOREIGN KEY (CreatedByAdminID) REFERENCES AdminUsers(AdminID)
);

-- Subscription history per shop
CREATE TABLE ShopSubscriptions (
    SubscriptionID      INT IDENTITY(1,1) PRIMARY KEY,
    ShopID              INT             NOT NULL,
    PlanID              INT             NOT NULL,
    StartDate           DATE            NOT NULL,
    EndDate             DATE            NOT NULL,
    AmountPaid          DECIMAL(10,2)   NOT NULL DEFAULT 0.00,
    PaymentMode         NVARCHAR(50)    NULL,       -- Cash, UPI, Bank Transfer
    PaymentReference    NVARCHAR(100)   NULL,
    IsActive            BIT             NOT NULL DEFAULT 1,  -- current active subscription
    ExtendedByAdminID   INT             NULL,
    Notes               NVARCHAR(300)   NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (ShopID)             REFERENCES Shops(ShopID),
    FOREIGN KEY (PlanID)             REFERENCES SubscriptionPlans(PlanID),
    FOREIGN KEY (ExtendedByAdminID)  REFERENCES AdminUsers(AdminID)
);

-- Admin notifications (subscription expiry alerts)
CREATE TABLE AdminNotifications (
    NotificationID      INT IDENTITY(1,1) PRIMARY KEY,
    ShopID              INT             NOT NULL,
    NotificationType    NVARCHAR(50)    NOT NULL,   -- 'EXPIRY_WARNING', 'EXPIRED', 'NEW_SIGNUP'
    Message             NVARCHAR(500)   NOT NULL,
    IsRead              BIT             NOT NULL DEFAULT 0,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (ShopID) REFERENCES Shops(ShopID)
);

-- ============================================================
--  SECTION 2: PRODUCT & INVENTORY
-- ============================================================

-- Product categories
CREATE TABLE ProductCategories (
    CategoryID      INT IDENTITY(1,1) PRIMARY KEY,
    ShopID          INT             NOT NULL,
    CategoryName    NVARCHAR(100)   NOT NULL,   -- Pesticide, Seeds, Fertilizer, Tools, etc.
    IsActive        BIT             NOT NULL DEFAULT 1,
    FOREIGN KEY (ShopID) REFERENCES Shops(ShopID)
);

-- Units of measurement
CREATE TABLE Units (
    UnitID      INT IDENTITY(1,1) PRIMARY KEY,
    UnitName    NVARCHAR(50) NOT NULL,   -- KG, Liter, Packet, Bag, Bottle, Piece
    ShortName   NVARCHAR(10) NOT NULL    -- kg, L, pkt, bag, btl, pc
);

INSERT INTO Units (UnitName, ShortName) VALUES
('Kilogram','kg'), ('Gram','gm'), ('Liter','L'),
('Milliliter','mL'), ('Packet','pkt'), ('Bag','bag'),
('Bottle','btl'), ('Piece','pc'), ('Box','box'), ('Quintal','qtl');

-- Companies / Suppliers (jahan se maal aata hai)
CREATE TABLE Suppliers (
    SupplierID          INT IDENTITY(1,1) PRIMARY KEY,
    ShopID              INT             NOT NULL,
    CompanyName         NVARCHAR(150)   NOT NULL,
    ContactPersonName   NVARCHAR(100)   NULL,
    MobileNumber        NVARCHAR(15)    NULL,
    Email               NVARCHAR(150)   NULL,
    Address             NVARCHAR(300)   NULL,
    GSTNumber           NVARCHAR(20)    NULL,
    OpeningBalance      DECIMAL(12,2)   NOT NULL DEFAULT 0.00,  -- agar pehle se kuch dena h
    IsActive            BIT             NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (ShopID) REFERENCES Shops(ShopID)
);

-- Products master (per shop, multi-tenant)
CREATE TABLE Products (
    ProductID           INT IDENTITY(1,1) PRIMARY KEY,
    ShopID              INT             NOT NULL,
    CategoryID          INT             NOT NULL,
    SupplierID          INT             NULL,       -- main supplier
    ProductName         NVARCHAR(200)   NOT NULL,
    CompanyName         NVARCHAR(150)   NULL,       -- brand/company name on packet
    HSNCode             NVARCHAR(20)    NULL,       -- GST HSN code
    UnitID              INT             NOT NULL,
    PurchasePrice       DECIMAL(10,2)   NOT NULL DEFAULT 0.00,
    SellingPrice        DECIMAL(10,2)   NOT NULL,
    GSTPercent          DECIMAL(5,2)    NOT NULL DEFAULT 0.00,  -- product-level GST override
    UseShopGST          BIT             NOT NULL DEFAULT 1,     -- 1 = use shop GST, 0 = use product GST
    CurrentStock        DECIMAL(12,3)   NOT NULL DEFAULT 0.000,
    MinStockAlert       DECIMAL(12,3)   NOT NULL DEFAULT 5.000,
    IsActive            BIT             NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (ShopID)     REFERENCES Shops(ShopID),
    FOREIGN KEY (CategoryID) REFERENCES ProductCategories(CategoryID),
    FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID),
    FOREIGN KEY (UnitID)     REFERENCES Units(UnitID)
);

-- Stock purchase (maal aana — inventory update)
CREATE TABLE PurchaseOrders (
    PurchaseID          INT IDENTITY(1,1) PRIMARY KEY,
    ShopID              INT             NOT NULL,
    SupplierID          INT             NOT NULL,
    PurchaseDate        DATE            NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    InvoiceNumber       NVARCHAR(50)    NULL,       -- supplier ka bill number
    TotalAmount         DECIMAL(12,2)   NOT NULL,
    DiscountAmount      DECIMAL(10,2)   NOT NULL DEFAULT 0.00,
    GSTAmount           DECIMAL(10,2)   NOT NULL DEFAULT 0.00,
    NetPayable          DECIMAL(12,2)   NOT NULL,
    AmountPaid          DECIMAL(12,2)   NOT NULL DEFAULT 0.00,
    PaymentStatus       NVARCHAR(20)    NOT NULL DEFAULT 'PENDING',  -- PENDING, PARTIAL, PAID
    Notes               NVARCHAR(300)   NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (ShopID)      REFERENCES Shops(ShopID),
    FOREIGN KEY (SupplierID)  REFERENCES Suppliers(SupplierID)
);

-- Purchase order items (konsa product kitna aaya)
CREATE TABLE PurchaseOrderItems (
    PurchaseItemID  INT IDENTITY(1,1) PRIMARY KEY,
    PurchaseID      INT             NOT NULL,
    ProductID       INT             NOT NULL,
    Quantity        DECIMAL(12,3)   NOT NULL,
    UnitPrice       DECIMAL(10,2)   NOT NULL,
    GSTPercent      DECIMAL(5,2)    NOT NULL DEFAULT 0.00,
    GSTAmount       DECIMAL(10,2)   NOT NULL DEFAULT 0.00,
    TotalAmount     DECIMAL(12,2)   NOT NULL,
    FOREIGN KEY (PurchaseID) REFERENCES PurchaseOrders(PurchaseID),
    FOREIGN KEY (ProductID)  REFERENCES Products(ProductID)
);

-- Supplier payments (unko kab kab paise diye)
CREATE TABLE SupplierPayments (
    PaymentID       INT IDENTITY(1,1) PRIMARY KEY,
    ShopID          INT             NOT NULL,
    SupplierID      INT             NOT NULL,
    PurchaseID      INT             NULL,   -- specific purchase ke against, ya general payment
    PaymentDate     DATE            NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    Amount          DECIMAL(12,2)   NOT NULL,
    PaymentMode     NVARCHAR(50)    NOT NULL DEFAULT 'Cash',  -- Cash, UPI, Bank
    Reference       NVARCHAR(100)   NULL,
    Notes           NVARCHAR(200)   NULL,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (ShopID)      REFERENCES Shops(ShopID),
    FOREIGN KEY (SupplierID)  REFERENCES Suppliers(SupplierID),
    FOREIGN KEY (PurchaseID)  REFERENCES PurchaseOrders(PurchaseID)
);

-- ============================================================
--  SECTION 3: CUSTOMERS
-- ============================================================

CREATE TABLE Customers (
    CustomerID      INT IDENTITY(1,1) PRIMARY KEY,
    ShopID          INT             NOT NULL,
    FullName        NVARCHAR(100)   NOT NULL,
    FatherName      NVARCHAR(100)   NULL,
    MobileNumber    NVARCHAR(15)    NOT NULL,
    AlternateMobile NVARCHAR(15)    NULL,
    Village         NVARCHAR(100)   NULL,
    Tehsil          NVARCHAR(100)   NULL,
    District        NVARCHAR(100)   NULL,
    State           NVARCHAR(100)   NOT NULL DEFAULT 'Haryana',
    LandAcres       DECIMAL(8,2)    NULL,   -- optional, useful for agro shops
    AadhaarLast4    NVARCHAR(4)     NULL,   -- partial only, for identification
    OpeningBalance  DECIMAL(12,2)   NOT NULL DEFAULT 0.00,  -- pehle se kuch baki tha
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (ShopID) REFERENCES Shops(ShopID)
);

-- ============================================================
--  SECTION 4: BILLING
-- ============================================================

-- Bills / Sales invoices
CREATE TABLE Bills (
    BillID          INT IDENTITY(1,1) PRIMARY KEY,
    ShopID          INT             NOT NULL,
    CustomerID      INT             NOT NULL,
    BillNumber      NVARCHAR(50)    NOT NULL,   -- shopkeeper prefix + auto sequence
    BillDate        DATE            NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    SubTotal        DECIMAL(12,2)   NOT NULL,   -- before GST
    DiscountAmount  DECIMAL(10,2)   NOT NULL DEFAULT 0.00,
    GSTPercent      DECIMAL(5,2)    NOT NULL DEFAULT 0.00,
    GSTAmount       DECIMAL(10,2)   NOT NULL DEFAULT 0.00,
    TotalAmount     DECIMAL(12,2)   NOT NULL,   -- final bill amount
    AmountPaid      DECIMAL(12,2)   NOT NULL DEFAULT 0.00,
    AmountPending   AS (TotalAmount - AmountPaid) PERSISTED,  -- computed column
    PaymentStatus   NVARCHAR(20)    NOT NULL DEFAULT 'PENDING',  -- PENDING, PARTIAL, PAID
    Notes           NVARCHAR(300)   NULL,
    IsReturn        BIT             NOT NULL DEFAULT 0,  -- return bill flag
    OriginalBillID  INT             NULL,        -- if return, points to original
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE(),
    UNIQUE (ShopID, BillNumber),
    FOREIGN KEY (ShopID)        REFERENCES Shops(ShopID),
    FOREIGN KEY (CustomerID)    REFERENCES Customers(CustomerID),
    FOREIGN KEY (OriginalBillID) REFERENCES Bills(BillID)
);

-- Bill items (konsa product kita liya)
CREATE TABLE BillItems (
    BillItemID      INT IDENTITY(1,1) PRIMARY KEY,
    BillID          INT             NOT NULL,
    ProductID       INT             NOT NULL,
    ProductName     NVARCHAR(200)   NOT NULL,   -- snapshot at time of sale
    Quantity        DECIMAL(12,3)   NOT NULL,
    UnitPrice       DECIMAL(10,2)   NOT NULL,   -- selling price at time of sale
    DiscountAmount  DECIMAL(10,2)   NOT NULL DEFAULT 0.00,
    GSTPercent      DECIMAL(5,2)    NOT NULL DEFAULT 0.00,
    GSTAmount       DECIMAL(10,2)   NOT NULL DEFAULT 0.00,
    TotalAmount     DECIMAL(12,2)   NOT NULL,
    FOREIGN KEY (BillID)     REFERENCES Bills(BillID),
    FOREIGN KEY (ProductID)  REFERENCES Products(ProductID)
);

-- Payments against bills (ek bill pe multiple payments aate hain)
CREATE TABLE BillPayments (
    PaymentID       INT IDENTITY(1,1) PRIMARY KEY,
    BillID          INT             NOT NULL,
    ShopID          INT             NOT NULL,
    CustomerID      INT             NOT NULL,
    PaymentDate     DATE            NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    Amount          DECIMAL(12,2)   NOT NULL,
    PaymentMode     NVARCHAR(50)    NOT NULL DEFAULT 'Cash',  -- Cash, UPI, Cheque
    Reference       NVARCHAR(100)   NULL,   -- UPI transaction ID ya cheque no.
    Notes           NVARCHAR(200)   NULL,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (BillID)      REFERENCES Bills(BillID),
    FOREIGN KEY (ShopID)      REFERENCES Shops(ShopID),
    FOREIGN KEY (CustomerID)  REFERENCES Customers(CustomerID)
);

-- Credit notes (return ke baad adjustment)
CREATE TABLE CreditNotes (
    CreditNoteID    INT IDENTITY(1,1) PRIMARY KEY,
    ShopID          INT             NOT NULL,
    CustomerID      INT             NOT NULL,
    OriginalBillID  INT             NOT NULL,
    CreditNoteDate  DATE            NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    CreditAmount    DECIMAL(12,2)   NOT NULL,
    AdjustedAmount  DECIMAL(12,2)   NOT NULL DEFAULT 0.00,  -- kitna adjust ho gaya
    RemainingCredit AS (CreditAmount - AdjustedAmount) PERSISTED,
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'OPEN',  -- OPEN, ADJUSTED, CLOSED
    Notes           NVARCHAR(300)   NULL,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (ShopID)          REFERENCES Shops(ShopID),
    FOREIGN KEY (CustomerID)      REFERENCES Customers(CustomerID),
    FOREIGN KEY (OriginalBillID)  REFERENCES Bills(BillID)
);

-- Credit note items (konsa product return hua)
CREATE TABLE CreditNoteItems (
    CreditNoteItemID    INT IDENTITY(1,1) PRIMARY KEY,
    CreditNoteID        INT             NOT NULL,
    ProductID           INT             NOT NULL,
    ProductName         NVARCHAR(200)   NOT NULL,
    Quantity            DECIMAL(12,3)   NOT NULL,
    UnitPrice           DECIMAL(10,2)   NOT NULL,
    TotalAmount         DECIMAL(12,2)   NOT NULL,
    FOREIGN KEY (CreditNoteID)  REFERENCES CreditNotes(CreditNoteID),
    FOREIGN KEY (ProductID)     REFERENCES Products(ProductID)
);

-- ============================================================
--  SECTION 5: EXPENSES
-- ============================================================

-- Expense categories (fixed + custom)
CREATE TABLE ExpenseCategories (
    CategoryID      INT IDENTITY(1,1) PRIMARY KEY,
    ShopID          INT             NULL,   -- NULL = system default (fixed), ShopID = custom
    CategoryName    NVARCHAR(100)   NOT NULL,
    IsSystem        BIT             NOT NULL DEFAULT 0,
    IsActive        BIT             NOT NULL DEFAULT 1
);

-- System default expense categories
INSERT INTO ExpenseCategories (ShopID, CategoryName, IsSystem) VALUES
(NULL, 'Electricity Bill', 1),
(NULL, 'Shop Rent',        1),
(NULL, 'Employee Salary',  1),
(NULL, 'Petrol / Diesel',  1),
(NULL, 'Vehicle Repair',   1),
(NULL, 'Packaging Material', 1),
(NULL, 'Phone / Internet', 1),
(NULL, 'Shop Maintenance', 1),
(NULL, 'Miscellaneous',    1);

-- Expenses ledger
CREATE TABLE Expenses (
    ExpenseID       INT IDENTITY(1,1) PRIMARY KEY,
    ShopID          INT             NOT NULL,
    CategoryID      INT             NOT NULL,
    ExpenseDate     DATE            NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    Amount          DECIMAL(12,2)   NOT NULL,
    Description     NVARCHAR(300)   NULL,
    PaymentMode     NVARCHAR(50)    NOT NULL DEFAULT 'Cash',
    Reference       NVARCHAR(100)   NULL,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (ShopID)      REFERENCES Shops(ShopID),
    FOREIGN KEY (CategoryID)  REFERENCES ExpenseCategories(CategoryID)
);

-- ============================================================
--  SECTION 6: STOCK MOVEMENT LOG (audit trail)
-- ============================================================

CREATE TABLE StockMovements (
    MovementID      INT IDENTITY(1,1) PRIMARY KEY,
    ShopID          INT             NOT NULL,
    ProductID       INT             NOT NULL,
    MovementType    NVARCHAR(30)    NOT NULL,  -- PURCHASE, SALE, RETURN_IN, RETURN_OUT, ADJUSTMENT
    ReferenceType   NVARCHAR(30)    NULL,      -- 'BILL', 'PURCHASE_ORDER', 'CREDIT_NOTE'
    ReferenceID     INT             NULL,      -- BillID or PurchaseID or CreditNoteID
    QuantityChange  DECIMAL(12,3)   NOT NULL,  -- positive = stock IN, negative = stock OUT
    StockBefore     DECIMAL(12,3)   NOT NULL,
    StockAfter      DECIMAL(12,3)   NOT NULL,
    Notes           NVARCHAR(200)   NULL,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (ShopID)    REFERENCES Shops(ShopID),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

-- ============================================================
--  SECTION 7: INDEXES (performance)
-- ============================================================

CREATE INDEX IX_Bills_ShopID_CustomerID     ON Bills(ShopID, CustomerID);
CREATE INDEX IX_Bills_ShopID_BillDate       ON Bills(ShopID, BillDate);
CREATE INDEX IX_Bills_PaymentStatus         ON Bills(ShopID, PaymentStatus);
CREATE INDEX IX_BillItems_BillID            ON BillItems(BillID);
CREATE INDEX IX_BillPayments_BillID         ON BillPayments(BillID);
CREATE INDEX IX_BillPayments_CustomerID     ON BillPayments(CustomerID);
CREATE INDEX IX_PurchaseOrders_SupplierID   ON PurchaseOrders(ShopID, SupplierID);
CREATE INDEX IX_Customers_ShopID_Mobile     ON Customers(ShopID, MobileNumber);
CREATE INDEX IX_Products_ShopID_Category    ON Products(ShopID, CategoryID);
CREATE INDEX IX_StockMovements_ProductID    ON StockMovements(ShopID, ProductID);
CREATE INDEX IX_Expenses_ShopID_Date        ON Expenses(ShopID, ExpenseDate);
CREATE INDEX IX_ShopSubscriptions_ShopID    ON ShopSubscriptions(ShopID, EndDate);

-- ============================================================
--  SECTION 8: STORED PROCEDURES
-- ============================================================

GO

-- SP 1: Generate next bill number for a shop
CREATE OR ALTER PROCEDURE sp_GetNextBillNumber
    @ShopID     INT,
    @BillNumber NVARCHAR(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    DECLARE @StartNum   INT;
    DECLARE @SeqNum     INT;

    UPDATE Shops 
    SET CurrentBillSequence = CurrentBillSequence + 1
    WHERE ShopID = @ShopID;

    SELECT @StartNum = BillStartNumber, @SeqNum = CurrentBillSequence
    FROM Shops WHERE ShopID = @ShopID;

    SET @BillNumber = CAST((@StartNum + @SeqNum - 1) AS NVARCHAR(50));

    COMMIT TRANSACTION;
END;
GO

-- SP 2: Create Bill (with items, update stock, update payment status)
CREATE OR ALTER PROCEDURE sp_CreateBill
    @ShopID         INT,
    @CustomerID     INT,
    @BillDate       DATE,
    @GSTPercent     DECIMAL(5,2),
    @DiscountAmount DECIMAL(10,2),
    @AmountPaid     DECIMAL(12,2),
    @Notes          NVARCHAR(300),
    @BillItemsXML   XML,           -- bill items passed as XML
    @BillID         INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @BillNumber NVARCHAR(50);
        EXEC sp_GetNextBillNumber @ShopID, @BillNumber OUTPUT;

        -- Parse bill items from XML
        DECLARE @Items TABLE (
            ProductID   INT,
            ProductName NVARCHAR(200),
            Quantity    DECIMAL(12,3),
            UnitPrice   DECIMAL(10,2),
            GSTPercent  DECIMAL(5,2),
            GSTAmount   DECIMAL(10,2),
            ItemDiscount DECIMAL(10,2),
            TotalAmount DECIMAL(12,2)
        );

        INSERT INTO @Items
        SELECT
            i.value('(ProductID)[1]',   'INT'),
            i.value('(ProductName)[1]', 'NVARCHAR(200)'),
            i.value('(Quantity)[1]',    'DECIMAL(12,3)'),
            i.value('(UnitPrice)[1]',   'DECIMAL(10,2)'),
            i.value('(GSTPercent)[1]',  'DECIMAL(5,2)'),
            i.value('(GSTAmount)[1]',   'DECIMAL(10,2)'),
            i.value('(ItemDiscount)[1]','DECIMAL(10,2)'),
            i.value('(TotalAmount)[1]', 'DECIMAL(12,2)')
        FROM @BillItemsXML.nodes('/Items/Item') AS T(i);

        DECLARE @SubTotal   DECIMAL(12,2) = (SELECT SUM(TotalAmount) FROM @Items);
        DECLARE @GSTAmount  DECIMAL(10,2) = ROUND(@SubTotal * @GSTPercent / 100, 2);
        DECLARE @Total      DECIMAL(12,2) = @SubTotal - @DiscountAmount + @GSTAmount;
        DECLARE @Status     NVARCHAR(20)  = 
            CASE 
                WHEN @AmountPaid >= @Total THEN 'PAID'
                WHEN @AmountPaid > 0       THEN 'PARTIAL'
                ELSE 'PENDING'
            END;

        -- Insert bill header
        INSERT INTO Bills (ShopID, CustomerID, BillNumber, BillDate, SubTotal, 
                           DiscountAmount, GSTPercent, GSTAmount, TotalAmount, 
                           AmountPaid, PaymentStatus, Notes)
        VALUES (@ShopID, @CustomerID, @BillNumber, @BillDate, @SubTotal,
                @DiscountAmount, @GSTPercent, @GSTAmount, @Total,
                @AmountPaid, @Status, @Notes);

        SET @BillID = SCOPE_IDENTITY();

        -- Insert bill items
        INSERT INTO BillItems (BillID, ProductID, ProductName, Quantity, UnitPrice, 
                               DiscountAmount, GSTPercent, GSTAmount, TotalAmount)
        SELECT @BillID, ProductID, ProductName, Quantity, UnitPrice,
               ItemDiscount, GSTPercent, GSTAmount, TotalAmount
        FROM @Items;

        -- Update stock for each item
        UPDATE P
        SET P.CurrentStock = P.CurrentStock - I.Quantity
        FROM Products P
        INNER JOIN @Items I ON I.ProductID = P.ProductID;

        -- Insert stock movement logs
        INSERT INTO StockMovements (ShopID, ProductID, MovementType, ReferenceType, 
                                    ReferenceID, QuantityChange, StockBefore, StockAfter)
        SELECT @ShopID, I.ProductID, 'SALE', 'BILL', @BillID,
               -I.Quantity,
               P.CurrentStock + I.Quantity,
               P.CurrentStock
        FROM @Items I
        INNER JOIN Products P ON P.ProductID = I.ProductID;

        -- Record initial payment if any
        IF @AmountPaid > 0
        BEGIN
            INSERT INTO BillPayments (BillID, ShopID, CustomerID, PaymentDate, Amount, PaymentMode)
            VALUES (@BillID, @ShopID, @CustomerID, @BillDate, @AmountPaid, 'Cash');
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- SP 3: Add payment against a bill
CREATE OR ALTER PROCEDURE sp_AddBillPayment
    @BillID         INT,
    @ShopID         INT,
    @CustomerID     INT,
    @Amount         DECIMAL(12,2),
    @PaymentMode    NVARCHAR(50),
    @Reference      NVARCHAR(100),
    @Notes          NVARCHAR(200),
    @PaymentDate    DATE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO BillPayments (BillID, ShopID, CustomerID, PaymentDate, Amount, PaymentMode, Reference, Notes)
        VALUES (@BillID, @ShopID, @CustomerID, @PaymentDate, @Amount, @PaymentMode, @Reference, @Notes);

        -- Update bill AmountPaid and PaymentStatus
        DECLARE @TotalPaid  DECIMAL(12,2);
        DECLARE @BillTotal  DECIMAL(12,2);

        SELECT @TotalPaid = SUM(Amount) FROM BillPayments WHERE BillID = @BillID;
        SELECT @BillTotal = TotalAmount FROM Bills WHERE BillID = @BillID;

        UPDATE Bills
        SET AmountPaid    = @TotalPaid,
            PaymentStatus = CASE 
                              WHEN @TotalPaid >= @BillTotal THEN 'PAID'
                              WHEN @TotalPaid > 0           THEN 'PARTIAL'
                              ELSE 'PENDING' 
                            END
        WHERE BillID = @BillID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- SP 4: Process product return (create credit note, restore stock)
CREATE OR ALTER PROCEDURE sp_ProcessReturn
    @ShopID             INT,
    @CustomerID         INT,
    @OriginalBillID     INT,
    @ReturnDate         DATE,
    @Notes              NVARCHAR(300),
    @ReturnItemsXML     XML,          -- items being returned
    @CreditNoteID       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @ReturnItems TABLE (
            ProductID   INT,
            ProductName NVARCHAR(200),
            Quantity    DECIMAL(12,3),
            UnitPrice   DECIMAL(10,2),
            TotalAmount DECIMAL(12,2)
        );

        INSERT INTO @ReturnItems
        SELECT
            i.value('(ProductID)[1]',   'INT'),
            i.value('(ProductName)[1]', 'NVARCHAR(200)'),
            i.value('(Quantity)[1]',    'DECIMAL(12,3)'),
            i.value('(UnitPrice)[1]',   'DECIMAL(10,2)'),
            i.value('(TotalAmount)[1]', 'DECIMAL(12,2)')
        FROM @ReturnItemsXML.nodes('/Items/Item') AS T(i);

        DECLARE @CreditAmount DECIMAL(12,2) = (SELECT SUM(TotalAmount) FROM @ReturnItems);

        -- Create credit note
        INSERT INTO CreditNotes (ShopID, CustomerID, OriginalBillID, CreditNoteDate, CreditAmount, Notes)
        VALUES (@ShopID, @CustomerID, @OriginalBillID, @ReturnDate, @CreditAmount, @Notes);

        SET @CreditNoteID = SCOPE_IDENTITY();

        -- Insert credit note items
        INSERT INTO CreditNoteItems (CreditNoteID, ProductID, ProductName, Quantity, UnitPrice, TotalAmount)
        SELECT @CreditNoteID, ProductID, ProductName, Quantity, UnitPrice, TotalAmount
        FROM @ReturnItems;

        -- Restore stock
        UPDATE P
        SET P.CurrentStock = P.CurrentStock + R.Quantity
        FROM Products P
        INNER JOIN @ReturnItems R ON R.ProductID = P.ProductID;

        -- Stock movement log
        INSERT INTO StockMovements (ShopID, ProductID, MovementType, ReferenceType,
                                    ReferenceID, QuantityChange, StockBefore, StockAfter)
        SELECT @ShopID, R.ProductID, 'RETURN_IN', 'CREDIT_NOTE', @CreditNoteID,
               R.Quantity,
               P.CurrentStock - R.Quantity,
               P.CurrentStock
        FROM @ReturnItems R
        INNER JOIN Products P ON P.ProductID = R.ProductID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- SP 5: Record purchase from supplier (maal aana + inventory update)
CREATE OR ALTER PROCEDURE sp_CreatePurchaseOrder
    @ShopID         INT,
    @SupplierID     INT,
    @PurchaseDate   DATE,
    @InvoiceNumber  NVARCHAR(50),
    @AmountPaid     DECIMAL(12,2),
    @PaymentMode    NVARCHAR(50),
    @Notes          NVARCHAR(300),
    @ItemsXML       XML,
    @PurchaseID     INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @PurchaseItems TABLE (
            ProductID   INT,
            Quantity    DECIMAL(12,3),
            UnitPrice   DECIMAL(10,2),
            GSTPercent  DECIMAL(5,2),
            GSTAmount   DECIMAL(10,2),
            TotalAmount DECIMAL(12,2)
        );

        INSERT INTO @PurchaseItems
        SELECT
            i.value('(ProductID)[1]',  'INT'),
            i.value('(Quantity)[1]',   'DECIMAL(12,3)'),
            i.value('(UnitPrice)[1]',  'DECIMAL(10,2)'),
            i.value('(GSTPercent)[1]', 'DECIMAL(5,2)'),
            i.value('(GSTAmount)[1]',  'DECIMAL(10,2)'),
            i.value('(TotalAmount)[1]','DECIMAL(12,2)')
        FROM @ItemsXML.nodes('/Items/Item') AS T(i);

        DECLARE @Total      DECIMAL(12,2) = (SELECT SUM(TotalAmount) FROM @PurchaseItems);
        DECLARE @GSTTotal   DECIMAL(10,2) = (SELECT SUM(GSTAmount)   FROM @PurchaseItems);
        DECLARE @Status     NVARCHAR(20)  = 
            CASE
                WHEN @AmountPaid >= @Total THEN 'PAID'
                WHEN @AmountPaid > 0       THEN 'PARTIAL'
                ELSE 'PENDING'
            END;

        INSERT INTO PurchaseOrders (ShopID, SupplierID, PurchaseDate, InvoiceNumber,
                                    TotalAmount, GSTAmount, NetPayable, AmountPaid, 
                                    PaymentStatus, Notes)
        VALUES (@ShopID, @SupplierID, @PurchaseDate, @InvoiceNumber,
                @Total, @GSTTotal, @Total, @AmountPaid, @Status, @Notes);

        SET @PurchaseID = SCOPE_IDENTITY();

        -- Insert items
        INSERT INTO PurchaseOrderItems (PurchaseID, ProductID, Quantity, UnitPrice, 
                                        GSTPercent, GSTAmount, TotalAmount)
        SELECT @PurchaseID, ProductID, Quantity, UnitPrice, GSTPercent, GSTAmount, TotalAmount
        FROM @PurchaseItems;

        -- Update stock
        UPDATE P
        SET P.CurrentStock = P.CurrentStock + I.Quantity
        FROM Products P
        INNER JOIN @PurchaseItems I ON I.ProductID = P.ProductID;

        -- Stock movement log
        INSERT INTO StockMovements (ShopID, ProductID, MovementType, ReferenceType,
                                    ReferenceID, QuantityChange, StockBefore, StockAfter)
        SELECT @ShopID, I.ProductID, 'PURCHASE', 'PURCHASE_ORDER', @PurchaseID,
               I.Quantity,
               P.CurrentStock - I.Quantity,
               P.CurrentStock
        FROM @PurchaseItems I
        INNER JOIN Products P ON P.ProductID = I.ProductID;

        -- Record payment to supplier
        IF @AmountPaid > 0
        BEGIN
            INSERT INTO SupplierPayments (ShopID, SupplierID, PurchaseID, PaymentDate, 
                                          Amount, PaymentMode)
            VALUES (@ShopID, @SupplierID, @PurchaseID, @PurchaseDate, @AmountPaid, @PaymentMode);
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- SP 6: Customer ledger — full payment history
CREATE OR ALTER PROCEDURE sp_GetCustomerLedger
    @ShopID     INT,
    @CustomerID INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Customer info
    SELECT C.CustomerID, C.FullName, C.FatherName, C.MobileNumber,
           C.Village, C.District,
           ISNULL(C.OpeningBalance, 0) AS OpeningBalance,
           (SELECT ISNULL(SUM(AmountPending),0) FROM Bills 
            WHERE CustomerID = @CustomerID AND ShopID = @ShopID 
            AND PaymentStatus != 'PAID') AS TotalPending,
           (SELECT COUNT(*) FROM Bills 
            WHERE CustomerID = @CustomerID AND ShopID = @ShopID) AS TotalBills
    FROM Customers C
    WHERE C.CustomerID = @CustomerID AND C.ShopID = @ShopID;

    -- All bills
    SELECT B.BillID, B.BillNumber, B.BillDate, B.TotalAmount,
           B.AmountPaid, B.AmountPending, B.PaymentStatus,
           (SELECT COUNT(*) FROM BillItems WHERE BillID = B.BillID) AS ItemCount
    FROM Bills B
    WHERE B.CustomerID = @CustomerID AND B.ShopID = @ShopID
    ORDER BY B.BillDate DESC;

    -- Payment history
    SELECT BP.PaymentID, BP.PaymentDate, BP.Amount, BP.PaymentMode,
           BP.Reference, B.BillNumber
    FROM BillPayments BP
    INNER JOIN Bills B ON B.BillID = BP.BillID
    WHERE BP.CustomerID = @CustomerID AND BP.ShopID = @ShopID
    ORDER BY BP.PaymentDate DESC;

    -- Credit notes
    SELECT CN.CreditNoteID, CN.CreditNoteDate, CN.CreditAmount,
           CN.AdjustedAmount, CN.RemainingCredit, CN.Status,
           B.BillNumber AS OriginalBill
    FROM CreditNotes CN
    INNER JOIN Bills B ON B.BillID = CN.OriginalBillID
    WHERE CN.CustomerID = @CustomerID AND CN.ShopID = @ShopID
    ORDER BY CN.CreditNoteDate DESC;
END;
GO

-- SP 7: Supplier ledger
CREATE OR ALTER PROCEDURE sp_GetSupplierLedger
    @ShopID     INT,
    @SupplierID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT S.SupplierID, S.CompanyName, S.ContactPersonName, S.MobileNumber,
           S.OpeningBalance,
           ISNULL((SELECT SUM(NetPayable) FROM PurchaseOrders 
                   WHERE SupplierID = @SupplierID AND ShopID = @ShopID), 0) AS TotalPurchased,
           ISNULL((SELECT SUM(Amount) FROM SupplierPayments 
                   WHERE SupplierID = @SupplierID AND ShopID = @ShopID), 0) AS TotalPaid,
           S.OpeningBalance + 
           ISNULL((SELECT SUM(NetPayable) FROM PurchaseOrders 
                   WHERE SupplierID = @SupplierID AND ShopID = @ShopID), 0) -
           ISNULL((SELECT SUM(Amount) FROM SupplierPayments 
                   WHERE SupplierID = @SupplierID AND ShopID = @ShopID), 0) AS OutstandingDue
    FROM Suppliers S
    WHERE S.SupplierID = @SupplierID AND S.ShopID = @ShopID;

    -- Purchase orders
    SELECT PO.PurchaseID, PO.PurchaseDate, PO.InvoiceNumber,
           PO.NetPayable, PO.AmountPaid, PO.PaymentStatus
    FROM PurchaseOrders PO
    WHERE PO.SupplierID = @SupplierID AND PO.ShopID = @ShopID
    ORDER BY PO.PurchaseDate DESC;

    -- Payment history
    SELECT SP.PaymentID, SP.PaymentDate, SP.Amount, SP.PaymentMode,
           SP.Reference, PO.InvoiceNumber
    FROM SupplierPayments SP
    LEFT JOIN PurchaseOrders PO ON PO.PurchaseID = SP.PurchaseID
    WHERE SP.SupplierID = @SupplierID AND SP.ShopID = @ShopID
    ORDER BY SP.PaymentDate DESC;
END;
GO

-- SP 8: Dashboard — monthly summary for shopkeeper
CREATE OR ALTER PROCEDURE sp_GetMonthlyDashboard
    @ShopID INT,
    @Year   INT,
    @Month  INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartDate DATE = DATEFROMPARTS(@Year, @Month, 1);
    DECLARE @EndDate   DATE = EOMONTH(@StartDate);

    -- Sales summary
    SELECT 
        COUNT(*)                            AS TotalBills,
        ISNULL(SUM(TotalAmount), 0)         AS TotalSales,
        ISNULL(SUM(AmountPaid), 0)          AS TotalCollected,
        ISNULL(SUM(AmountPending), 0)       AS TotalPending,
        COUNT(CASE WHEN PaymentStatus = 'PAID'    THEN 1 END) AS PaidBills,
        COUNT(CASE WHEN PaymentStatus = 'PENDING' THEN 1 END) AS PendingBills,
        COUNT(CASE WHEN PaymentStatus = 'PARTIAL' THEN 1 END) AS PartialBills
    FROM Bills
    WHERE ShopID = @ShopID 
      AND BillDate BETWEEN @StartDate AND @EndDate
      AND IsReturn = 0;

    -- Expense summary
    SELECT 
        EC.CategoryName,
        ISNULL(SUM(E.Amount), 0) AS TotalExpense
    FROM Expenses E
    INNER JOIN ExpenseCategories EC ON EC.CategoryID = E.CategoryID
    WHERE E.ShopID = @ShopID
      AND E.ExpenseDate BETWEEN @StartDate AND @EndDate
    GROUP BY EC.CategoryName
    ORDER BY TotalExpense DESC;

    -- Total expenses
    SELECT ISNULL(SUM(Amount), 0) AS TotalExpenses
    FROM Expenses
    WHERE ShopID = @ShopID
      AND ExpenseDate BETWEEN @StartDate AND @EndDate;

    -- Purchases from suppliers this month
    SELECT ISNULL(SUM(NetPayable), 0) AS TotalPurchased,
           ISNULL(SUM(AmountPaid), 0) AS PaidToSuppliers
    FROM PurchaseOrders
    WHERE ShopID = @ShopID
      AND PurchaseDate BETWEEN @StartDate AND @EndDate;

    -- Top selling products
    SELECT TOP 5 
        BI.ProductName,
        SUM(BI.Quantity)    AS TotalQty,
        SUM(BI.TotalAmount) AS TotalAmount
    FROM BillItems BI
    INNER JOIN Bills B ON B.BillID = BI.BillID
    WHERE B.ShopID = @ShopID
      AND B.BillDate BETWEEN @StartDate AND @EndDate
      AND B.IsReturn = 0
    GROUP BY BI.ProductName
    ORDER BY TotalAmount DESC;
END;
GO

-- SP 9: Admin dashboard — all shops overview
CREATE OR ALTER PROCEDURE sp_AdminDashboard
AS
BEGIN
    SET NOCOUNT ON;

    -- Subscription expiring in next 7 days
    SELECT S.ShopID, S.ShopName, S.OwnerName, S.MobileNumber,
           SS.EndDate,
           DATEDIFF(DAY, CAST(GETDATE() AS DATE), SS.EndDate) AS DaysLeft,
           'EXPIRING_SOON' AS AlertType
    FROM Shops S
    INNER JOIN ShopSubscriptions SS ON SS.ShopID = S.ShopID AND SS.IsActive = 1
    WHERE DATEDIFF(DAY, CAST(GETDATE() AS DATE), SS.EndDate) BETWEEN 0 AND 7
      AND S.IsActive = 1
    ORDER BY SS.EndDate ASC;

    -- Already expired
    SELECT S.ShopID, S.ShopName, S.OwnerName, S.MobileNumber,
           SS.EndDate,
           DATEDIFF(DAY, SS.EndDate, CAST(GETDATE() AS DATE)) AS DaysOverdue
    FROM Shops S
    INNER JOIN ShopSubscriptions SS ON SS.ShopID = S.ShopID AND SS.IsActive = 1
    WHERE SS.EndDate < CAST(GETDATE() AS DATE)
      AND S.IsActive = 1
    ORDER BY SS.EndDate ASC;

    -- All shops summary
    SELECT S.ShopID, S.ShopName, S.OwnerName, S.MobileNumber, S.City,
           SS.StartDate, SS.EndDate,
           DATEDIFF(DAY, CAST(GETDATE() AS DATE), SS.EndDate) AS DaysLeft,
           SP.PlanName,
           S.IsActive
    FROM Shops S
    LEFT JOIN ShopSubscriptions SS ON SS.ShopID = S.ShopID AND SS.IsActive = 1
    LEFT JOIN SubscriptionPlans SP ON SP.PlanID = SS.PlanID
    ORDER BY SS.EndDate ASC;
END;
GO

-- SP 10: Extend subscription
CREATE OR ALTER PROCEDURE sp_ExtendSubscription
    @ShopID         INT,
    @PlanID         INT,
    @AdminID        INT,
    @AmountPaid     DECIMAL(10,2),
    @PaymentMode    NVARCHAR(50),
    @PaymentRef     NVARCHAR(100),
    @Notes          NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    -- Deactivate current subscription
    UPDATE ShopSubscriptions SET IsActive = 0
    WHERE ShopID = @ShopID AND IsActive = 1;

    DECLARE @DurationDays   INT;
    DECLARE @StartDate      DATE = CAST(GETDATE() AS DATE);
    DECLARE @EndDate        DATE;

    SELECT @DurationDays = DurationDays FROM SubscriptionPlans WHERE PlanID = @PlanID;
    SET @EndDate = DATEADD(DAY, @DurationDays, @StartDate);

    INSERT INTO ShopSubscriptions (ShopID, PlanID, StartDate, EndDate, AmountPaid,
                                   PaymentMode, PaymentReference, IsActive, 
                                   ExtendedByAdminID, Notes)
    VALUES (@ShopID, @PlanID, @StartDate, @EndDate, @AmountPaid,
            @PaymentMode, @PaymentRef, 1, @AdminID, @Notes);

    -- Mark old notification as read
    UPDATE AdminNotifications SET IsRead = 1
    WHERE ShopID = @ShopID AND IsRead = 0;

    COMMIT TRANSACTION;
END;
GO

-- SP 11: Low stock alert for shopkeeper
CREATE OR ALTER PROCEDURE sp_GetLowStockProducts
    @ShopID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT P.ProductID, P.ProductName, PC.CategoryName,
           P.CurrentStock, P.MinStockAlert, U.ShortName AS Unit,
           S.CompanyName AS PrimarySupplier
    FROM Products P
    INNER JOIN ProductCategories PC ON PC.CategoryID = P.CategoryID
    INNER JOIN Units U ON U.UnitID = P.UnitID
    LEFT JOIN Suppliers S ON S.SupplierID = P.SupplierID
    WHERE P.ShopID = @ShopID
      AND P.IsActive = 1
      AND P.CurrentStock <= P.MinStockAlert
    ORDER BY P.CurrentStock ASC;
END;
GO

-- ============================================================
--  SECTION 9: VIEWS
-- ============================================================

-- View: Customer pending summary (per shop)
CREATE OR ALTER VIEW vw_CustomerPendingSummary AS
SELECT 
    B.ShopID,
    C.CustomerID,
    C.FullName,
    C.MobileNumber,
    C.Village,
    COUNT(B.BillID)                         AS TotalBills,
    ISNULL(SUM(B.TotalAmount), 0)           AS TotalBilled,
    ISNULL(SUM(B.AmountPaid), 0)            AS TotalPaid,
    ISNULL(SUM(B.AmountPending), 0)         AS TotalPending,
    MAX(B.BillDate)                         AS LastBillDate
FROM Bills B
INNER JOIN Customers C ON C.CustomerID = B.CustomerID
WHERE B.IsReturn = 0
GROUP BY B.ShopID, C.CustomerID, C.FullName, C.MobileNumber, C.Village;
GO

-- View: Supplier outstanding summary
CREATE OR ALTER VIEW vw_SupplierOutstanding AS
SELECT 
    S.ShopID,
    S.SupplierID,
    S.CompanyName,
    S.OpeningBalance,
    ISNULL(SUM(PO.NetPayable), 0)           AS TotalPurchased,
    ISNULL(SUM(SP.Amount), 0)               AS TotalPaid,
    S.OpeningBalance + 
    ISNULL(SUM(PO.NetPayable), 0) - 
    ISNULL(SUM(SP.Amount), 0)               AS OutstandingDue
FROM Suppliers S
LEFT JOIN PurchaseOrders PO ON PO.SupplierID = S.SupplierID AND PO.ShopID = S.ShopID
LEFT JOIN SupplierPayments SP ON SP.SupplierID = S.SupplierID AND SP.ShopID = S.ShopID
GROUP BY S.ShopID, S.SupplierID, S.CompanyName, S.OpeningBalance;
GO

-- ============================================================
-- DONE — AgroBilling DB Schema Complete
-- Tables: 23 | Stored Procedures: 11 | Views: 2 | Indexes: 11
-- ============================================================