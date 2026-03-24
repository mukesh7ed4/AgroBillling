-- ================================================
--  AGROBILLING — TEST SEED DATA
--  Run this in SSMS on AgroBillingDB
--  Test Shop Login: shop@test.com / shop123
-- ================================================

USE AgroBillingDB;
GO

-- ─── 1. ADMIN USER (agar pehle se nahi banaya) ───
IF NOT EXISTS (SELECT 1 FROM AdminUsers WHERE Email = 'admin@agrobilling.com')
BEGIN
    INSERT INTO AdminUsers (FullName, Email, PasswordHash, IsActive, CreatedAt)
    VALUES (
        'Mukesh Admin',
        'admin@agrobilling.com',
        LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'admin123'), 2)),
        1,
        GETDATE()
    );
END
GO

-- ─── 2. SUBSCRIPTION PLANS ───
IF NOT EXISTS (SELECT 1 FROM SubscriptionPlans WHERE PlanName = '1 Month Free Trial')
BEGIN
    INSERT INTO SubscriptionPlans (PlanName, DurationDays, Price, IsTrial, IsActive) VALUES
    ('1 Month Free Trial', 30,   0.00,   1, 1),
    ('Monthly',            30,   299.00, 0, 1),
    ('Yearly',             365,  2999.00,0, 1);
END
GO

-- ─── 3. TEST SHOP ───
DECLARE @ShopID INT;
DECLARE @AdminID INT = (SELECT TOP 1 AdminID FROM AdminUsers WHERE Email = 'admin@agrobilling.com');

IF NOT EXISTS (SELECT 1 FROM Shops WHERE Email = 'shop@test.com')
BEGIN
    INSERT INTO Shops (
        OwnerName, ShopName, MobileNumber, AlternateMobile,
        Email, Address, City, State, PinCode,
        GSTNumber, GSTPercent, BillStartNumber, CurrentBillSequence,
        PasswordHash, IsActive, CreatedAt, CreatedByAdminID
    ) VALUES (
        'Ramesh Kumar',
        'Kisan Agro Store',
        '9812345678',
        '9876543210',
        'shop@test.com',
        'Main Market, Near Bus Stand',
        'Rohtak',
        'Haryana',
        '124001',
        '06AABCU9603R1ZX',
        18.00,
        1001,
        0,
        LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'shop123'), 2)),
        1,
        GETDATE(),
        @AdminID
    );
END

SET @ShopID = (SELECT ShopID FROM Shops WHERE Email = 'shop@test.com');

-- ─── 4. SUBSCRIPTION FOR SHOP ───
IF NOT EXISTS (SELECT 1 FROM ShopSubscriptions WHERE ShopID = @ShopID AND IsActive = 1)
BEGIN
    DECLARE @PlanID INT = (SELECT TOP 1 PlanID FROM SubscriptionPlans WHERE IsTrial = 1);
    INSERT INTO ShopSubscriptions (
        ShopID, PlanID, StartDate, EndDate,
        AmountPaid, PaymentMode, IsActive, CreatedAt
    ) VALUES (
        @ShopID, @PlanID,
        CAST(GETDATE() AS DATE),
        CAST(DATEADD(DAY, 30, GETDATE()) AS DATE),
        0.00, 'Free Trial', 1, GETDATE()
    );
END
GO

-- ─── 5. PRODUCT CATEGORIES ───
DECLARE @ShopID INT = (SELECT ShopID FROM Shops WHERE Email = 'shop@test.com');

IF NOT EXISTS (SELECT 1 FROM ProductCategories WHERE ShopID = @ShopID)
BEGIN
    INSERT INTO ProductCategories (ShopID, CategoryName, IsActive) VALUES
    (@ShopID, 'Pesticide',   1),
    (@ShopID, 'Fertilizer',  1),
    (@ShopID, 'Seeds',       1),
    (@ShopID, 'Tools',       1),
    (@ShopID, 'Irrigation',  1);
END
GO

-- ─── 6. SUPPLIERS ───
DECLARE @ShopID INT = (SELECT ShopID FROM Shops WHERE Email = 'shop@test.com');

IF NOT EXISTS (SELECT 1 FROM Suppliers WHERE ShopID = @ShopID)
BEGIN
    INSERT INTO Suppliers (
        ShopID, CompanyName, ContactPersonName, MobileNumber,
        Email, Address, GSTNumber, OpeningBalance, IsActive, CreatedAt
    ) VALUES
    (@ShopID, 'Bayer CropScience',   'Suresh Sharma',  '9811111111', 'bayer@test.com',   'Delhi',   '07AAACB2894K1ZE', 5000.00, 1, GETDATE()),
    (@ShopID, 'IFFCO Fertilizers',   'Mahesh Verma',   '9822222222', 'iffco@test.com',   'Gurgaon', '06AAABI0041L1Z6', 2000.00, 1, GETDATE()),
    (@ShopID, 'Syngenta India',      'Rakesh Singh',   '9833333333', 'syngenta@test.com','Panipat',  '06AATCS7428J1ZT', 0.00,    1, GETDATE());
END
GO

-- ─── 7. PRODUCTS ───
DECLARE @ShopID INT     = (SELECT ShopID FROM Shops WHERE Email = 'shop@test.com');
DECLARE @CatPest INT    = (SELECT TOP 1 CategoryID FROM ProductCategories WHERE ShopID = @ShopID AND CategoryName = 'Pesticide');
DECLARE @CatFert INT    = (SELECT TOP 1 CategoryID FROM ProductCategories WHERE ShopID = @ShopID AND CategoryName = 'Fertilizer');
DECLARE @CatSeed INT    = (SELECT TOP 1 CategoryID FROM ProductCategories WHERE ShopID = @ShopID AND CategoryName = 'Seeds');
DECLARE @Sup1 INT       = (SELECT TOP 1 SupplierID FROM Suppliers WHERE ShopID = @ShopID AND CompanyName = 'Bayer CropScience');
DECLARE @Sup2 INT       = (SELECT TOP 1 SupplierID FROM Suppliers WHERE ShopID = @ShopID AND CompanyName = 'IFFCO Fertilizers');
DECLARE @UnitKG INT     = (SELECT TOP 1 UnitID FROM Units WHERE ShortName = 'kg');
DECLARE @UnitLtr INT    = (SELECT TOP 1 UnitID FROM Units WHERE ShortName = 'L');
DECLARE @UnitPkt INT    = (SELECT TOP 1 UnitID FROM Units WHERE ShortName = 'pkt');
DECLARE @UnitBag INT    = (SELECT TOP 1 UnitID FROM Units WHERE ShortName = 'bag');

IF NOT EXISTS (SELECT 1 FROM Products WHERE ShopID = @ShopID)
BEGIN
    INSERT INTO Products (
        ShopID, CategoryID, SupplierID, ProductName, CompanyName,
        UnitID, PurchasePrice, SellingPrice, GSTPercent, UseShopGST,
        CurrentStock, MinStockAlert, IsActive, CreatedAt
    ) VALUES
    -- Pesticides
    (@ShopID, @CatPest, @Sup1, 'Confidor 200SL 100ml',  'Bayer',  @UnitLtr, 380.00,  450.00,  18, 0, 50,  10, 1, GETDATE()),
    (@ShopID, @CatPest, @Sup1, 'Antracol 70WP 500gm',   'Bayer',  @UnitKG,  520.00,  620.00,  18, 0, 30,  5,  1, GETDATE()),
    (@ShopID, @CatPest, @Sup3, 'Ridomil Gold 1kg',      'Syngenta',@UnitKG, 750.00,  890.00,  18, 0, 20,  5,  1, GETDATE()),
    (@ShopID, @CatPest, @Sup3, 'Amistar Top 200ml',     'Syngenta',@UnitLtr,680.00,  800.00,  18, 0, 25,  5,  1, GETDATE()),
    -- Fertilizers
    (@ShopID, @CatFert, @Sup2, 'DAP 50kg Bag',          'IFFCO',  @UnitBag, 1250.00, 1380.00, 5,  0, 100, 20, 1, GETDATE()),
    (@ShopID, @CatFert, @Sup2, 'Urea 45kg Bag',         'IFFCO',  @UnitBag, 266.00,  290.00,  5,  0, 150, 30, 1, GETDATE()),
    (@ShopID, @CatFert, @Sup2, 'NPK 20-20-0 50kg',      'IFFCO',  @UnitBag, 1100.00, 1250.00, 5,  0, 80,  15, 1, GETDATE()),
    (@ShopID, @CatFert, @Sup2, 'Zinc Sulphate 1kg',     'IFFCO',  @UnitKG,  85.00,   110.00,  18, 0, 60,  10, 1, GETDATE()),
    -- Seeds
    (@ShopID, @CatSeed, @Sup3, 'Wheat Seeds 40kg',      'Syngenta',@UnitBag, 900.00,  1050.00, 0,  1, 40,  10, 1, GETDATE()),
    (@ShopID, @CatSeed, @Sup3, 'Mustard Seeds 1kg',     'Syngenta',@UnitKG,  180.00,  220.00,  0,  1, 35,  8,  1, GETDATE()),
    (@ShopID, @CatSeed, @Sup3, 'Paddy Seeds 5kg Pkt',   'Syngenta',@UnitPkt, 320.00,  390.00,  0,  1, 45,  10, 1, GETDATE());
END
GO

-- ─── 8. CUSTOMERS ───
DECLARE @ShopID INT = (SELECT ShopID FROM Shops WHERE Email = 'shop@test.com');

IF NOT EXISTS (SELECT 1 FROM Customers WHERE ShopID = @ShopID)
BEGIN
    INSERT INTO Customers (
        ShopID, FullName, FatherName, MobileNumber, AlternateMobile,
        Village, Tehsil, District, State, LandAcres,
        OpeningBalance, IsActive, CreatedAt
    ) VALUES
    (@ShopID, 'Rajesh Kumar',    'Suresh Kumar',   '9801111111', NULL,         'Asthal Bohar', 'Rohtak',  'Rohtak',  'Haryana', 8.5,  0.00,    1, GETDATE()),
    (@ShopID, 'Mahesh Singh',    'Ratan Singh',    '9802222222', '9812222222', 'Mokhra',       'Rohtak',  'Rohtak',  'Haryana', 12.0, 2500.00, 1, GETDATE()),
    (@ShopID, 'Suresh Yadav',    'Bhagwan Yadav',  '9803333333', NULL,         'Sunaria',      'Rohtak',  'Rohtak',  'Haryana', 5.0,  0.00,    1, GETDATE()),
    (@ShopID, 'Ramesh Hooda',    'Satbir Hooda',   '9804444444', NULL,         'Kiloi',        'Rohtak',  'Rohtak',  'Haryana', 15.0, 1200.00, 1, GETDATE()),
    (@ShopID, 'Dinesh Malik',    'Hawa Singh',     '9805555555', '9815555555', 'Titoli',       'Rohtak',  'Rohtak',  'Haryana', 7.5,  0.00,    1, GETDATE()),
    (@ShopID, 'Prem Kumar',      'Jeet Ram',       '9806666666', NULL,         'Lakhan Majra', 'Rohtak',  'Rohtak',  'Haryana', 3.0,  500.00,  1, GETDATE()),
    (@ShopID, 'Vikram Sangwan',  'Om Prakash',     '9807777777', NULL,         'Ismaila',      'Jhajjar', 'Jhajjar', 'Haryana', 20.0, 0.00,    1, GETDATE()),
    (@ShopID, 'Naresh Antil',    'Dalbir Antil',   '9808888888', NULL,         'Maham',        'Rohtak',  'Rohtak',  'Haryana', 6.0,  3000.00, 1, GETDATE());
END
GO

-- ─── 9. PURCHASE ORDERS (maal aaya supplier se) ───
DECLARE @ShopID INT  = (SELECT ShopID FROM Shops WHERE Email = 'shop@test.com');
DECLARE @Sup1 INT    = (SELECT TOP 1 SupplierID FROM Suppliers WHERE ShopID = @ShopID AND CompanyName = 'Bayer CropScience');
DECLARE @Sup2 INT    = (SELECT TOP 1 SupplierID FROM Suppliers WHERE ShopID = @ShopID AND CompanyName = 'IFFCO Fertilizers');
DECLARE @Prod1 INT   = (SELECT TOP 1 ProductID FROM Products WHERE ShopID = @ShopID AND ProductName = 'Confidor 200SL 100ml');
DECLARE @Prod2 INT   = (SELECT TOP 1 ProductID FROM Products WHERE ShopID = @ShopID AND ProductName = 'DAP 50kg Bag');
DECLARE @Prod3 INT   = (SELECT TOP 1 ProductID FROM Products WHERE ShopID = @ShopID AND ProductName = 'Urea 45kg Bag');

IF NOT EXISTS (SELECT 1 FROM PurchaseOrders WHERE ShopID = @ShopID)
BEGIN
    -- Purchase 1 from Bayer
    INSERT INTO PurchaseOrders (
        ShopID, SupplierID, PurchaseDate, InvoiceNumber,
        TotalAmount, DiscountAmount, GSTAmount, NetPayable,
        AmountPaid, PaymentStatus, Notes, CreatedAt
    ) VALUES (
        @ShopID, @Sup1,
        CAST(DATEADD(DAY, -20, GETDATE()) AS DATE),
        'BAY-2025-001',
        19000.00, 0, 3420.00, 19000.00,
        10000.00, 'PARTIAL', 'First batch of pesticides', GETDATE()
    );

    DECLARE @PO1 INT = SCOPE_IDENTITY();
    INSERT INTO PurchaseOrderItems (PurchaseID, ProductID, Quantity, UnitPrice, GSTPercent, GSTAmount, TotalAmount)
    VALUES (@PO1, @Prod1, 50, 380.00, 18, 3420.00, 19000.00);

    -- Supplier payment for PO1
    INSERT INTO SupplierPayments (ShopID, SupplierID, PurchaseID, PaymentDate, Amount, PaymentMode, CreatedAt)
    VALUES (@ShopID, @Sup1, @PO1, CAST(DATEADD(DAY, -20, GETDATE()) AS DATE), 10000.00, 'Cash', GETDATE());

    -- Purchase 2 from IFFCO
    INSERT INTO PurchaseOrders (
        ShopID, SupplierID, PurchaseDate, InvoiceNumber,
        TotalAmount, DiscountAmount, GSTAmount, NetPayable,
        AmountPaid, PaymentStatus, Notes, CreatedAt
    ) VALUES (
        @ShopID, @Sup2,
        CAST(DATEADD(DAY, -10, GETDATE()) AS DATE),
        'IFFCO-2025-042',
        82400.00, 0, 4120.00, 82400.00,
        82400.00, 'PAID', 'Fertilizer stock', GETDATE()
    );

    DECLARE @PO2 INT = SCOPE_IDENTITY();
    INSERT INTO PurchaseOrderItems (PurchaseID, ProductID, Quantity, UnitPrice, GSTPercent, GSTAmount, TotalAmount)
    VALUES
    (@PO2, @Prod2, 40, 1250.00, 5, 2500.00, 50000.00),
    (@PO2, @Prod3, 50, 266.00,  5, 665.00,  13300.00);

    INSERT INTO SupplierPayments (ShopID, SupplierID, PurchaseID, PaymentDate, Amount, PaymentMode, Reference, CreatedAt)
    VALUES (@ShopID, @Sup2, @PO2, CAST(DATEADD(DAY, -10, GETDATE()) AS DATE), 82400.00, 'UPI', 'UPI-9823456781', GETDATE());
END
GO

-- ─── 10. BILLS ───
DECLARE @ShopID INT  = (SELECT ShopID FROM Shops WHERE Email = 'shop@test.com');
DECLARE @Cust1 INT   = (SELECT TOP 1 CustomerID FROM Customers WHERE ShopID = @ShopID AND MobileNumber = '9801111111');
DECLARE @Cust2 INT   = (SELECT TOP 1 CustomerID FROM Customers WHERE ShopID = @ShopID AND MobileNumber = '9802222222');
DECLARE @Cust3 INT   = (SELECT TOP 1 CustomerID FROM Customers WHERE ShopID = @ShopID AND MobileNumber = '9803333333');
DECLARE @Cust4 INT   = (SELECT TOP 1 CustomerID FROM Customers WHERE ShopID = @ShopID AND MobileNumber = '9804444444');
DECLARE @Prod1 INT   = (SELECT TOP 1 ProductID FROM Products WHERE ShopID = @ShopID AND ProductName = 'Confidor 200SL 100ml');
DECLARE @Prod2 INT   = (SELECT TOP 1 ProductID FROM Products WHERE ShopID = @ShopID AND ProductName = 'DAP 50kg Bag');
DECLARE @Prod3 INT   = (SELECT TOP 1 ProductID FROM Products WHERE ShopID = @ShopID AND ProductName = 'Urea 45kg Bag');
DECLARE @Prod5 INT   = (SELECT TOP 1 ProductID FROM Products WHERE ShopID = @ShopID AND ProductName = 'Ridomil Gold 1kg');

-- Update shop bill sequence
UPDATE Shops SET CurrentBillSequence = 0 WHERE ShopID = @ShopID;

IF NOT EXISTS (SELECT 1 FROM Bills WHERE ShopID = @ShopID)
BEGIN
    -- Bill 1 — PAID
    UPDATE Shops SET CurrentBillSequence = CurrentBillSequence + 1 WHERE ShopID = @ShopID;
    INSERT INTO Bills (
        ShopID, CustomerID, BillNumber, BillDate,
        SubTotal, DiscountAmount, GSTPercent, GSTAmount,
        TotalAmount, AmountPaid, PaymentStatus, Notes, IsReturn, CreatedAt
    ) VALUES (
        @ShopID, @Cust1, '1001',
        CAST(DATEADD(DAY, -15, GETDATE()) AS DATE),
        3800.00, 0, 18, 684.00,
        4484.00, 4484.00, 'PAID', NULL, 0, GETDATE()
    );
    DECLARE @Bill1 INT = SCOPE_IDENTITY();
    INSERT INTO BillItems (BillID, ProductID, ProductName, Quantity, UnitPrice, DiscountAmount, GSTPercent, GSTAmount, TotalAmount)
    VALUES (@Bill1, @Prod1, 'Confidor 200SL 100ml', 8, 450.00, 0, 18, 648.00, 3600.00 + 648.00);
    INSERT INTO BillPayments (BillID, ShopID, CustomerID, PaymentDate, Amount, PaymentMode, CreatedAt)
    VALUES (@Bill1, @ShopID, @Cust1, CAST(DATEADD(DAY,-15,GETDATE()) AS DATE), 4484.00, 'Cash', GETDATE());

    -- Bill 2 — PARTIAL (Mahesh Singh has opening balance too)
    UPDATE Shops SET CurrentBillSequence = CurrentBillSequence + 1 WHERE ShopID = @ShopID;
    INSERT INTO Bills (
        ShopID, CustomerID, BillNumber, BillDate,
        SubTotal, DiscountAmount, GSTPercent, GSTAmount,
        TotalAmount, AmountPaid, PaymentStatus, Notes, IsReturn, CreatedAt
    ) VALUES (
        @ShopID, @Cust2, '1002',
        CAST(DATEADD(DAY, -12, GETDATE()) AS DATE),
        3760.00, 200.00, 5, 178.00,
        3738.00, 2000.00, 'PARTIAL', 'Baad mein dega', 0, GETDATE()
    );
    DECLARE @Bill2 INT = SCOPE_IDENTITY();
    INSERT INTO BillItems (BillID, ProductID, ProductName, Quantity, UnitPrice, DiscountAmount, GSTPercent, GSTAmount, TotalAmount)
    VALUES
    (@Bill2, @Prod2, 'DAP 50kg Bag',  2, 1380.00, 100.00, 5, 132.90, 2760.00 + 132.90 - 100.00),
    (@Bill2, @Prod3, 'Urea 45kg Bag', 3, 290.00,  100.00, 5, 41.25,  870.00  + 41.25  - 100.00);
    INSERT INTO BillPayments (BillID, ShopID, CustomerID, PaymentDate, Amount, PaymentMode, CreatedAt)
    VALUES (@Bill2, @ShopID, @Cust2, CAST(DATEADD(DAY,-12,GETDATE()) AS DATE), 2000.00, 'Cash', GETDATE());

    -- Bill 3 — PENDING
    UPDATE Shops SET CurrentBillSequence = CurrentBillSequence + 1 WHERE ShopID = @ShopID;
    INSERT INTO Bills (
        ShopID, CustomerID, BillNumber, BillDate,
        SubTotal, DiscountAmount, GSTPercent, GSTAmount,
        TotalAmount, AmountPaid, PaymentStatus, Notes, IsReturn, CreatedAt
    ) VALUES (
        @ShopID, @Cust3, '1003',
        CAST(DATEADD(DAY, -8, GETDATE()) AS DATE),
        2780.00, 0, 18, 500.40,
        3280.40, 0.00, 'PENDING', NULL, 0, GETDATE()
    );
    DECLARE @Bill3 INT = SCOPE_IDENTITY();
    INSERT INTO BillItems (BillID, ProductID, ProductName, Quantity, UnitPrice, DiscountAmount, GSTPercent, GSTAmount, TotalAmount)
    VALUES
    (@Bill3, @Prod1, 'Confidor 200SL 100ml', 3, 450.00, 0, 18, 243.00, 1350.00 + 243.00),
    (@Bill3, @Prod5, 'Ridomil Gold 1kg',     2, 890.00, 0, 18, 320.40, 1780.00 + 320.40 - 1593.00);

    -- Bill 4 — PAID
    UPDATE Shops SET CurrentBillSequence = CurrentBillSequence + 1 WHERE ShopID = @ShopID;
    INSERT INTO Bills (
        ShopID, CustomerID, BillNumber, BillDate,
        SubTotal, DiscountAmount, GSTPercent, GSTAmount,
        TotalAmount, AmountPaid, PaymentStatus, Notes, IsReturn, CreatedAt
    ) VALUES (
        @ShopID, @Cust4, '1004',
        CAST(DATEADD(DAY, -5, GETDATE()) AS DATE),
        5520.00, 500.00, 5, 251.00,
        5271.00, 5271.00, 'PAID', NULL, 0, GETDATE()
    );
    DECLARE @Bill4 INT = SCOPE_IDENTITY();
    INSERT INTO BillItems (BillID, ProductID, ProductName, Quantity, UnitPrice, DiscountAmount, GSTPercent, GSTAmount, TotalAmount)
    VALUES (@Bill4, @Prod2, 'DAP 50kg Bag', 4, 1380.00, 500.00, 5, 251.00, 5520.00 + 251.00 - 500.00);
    INSERT INTO BillPayments (BillID, ShopID, CustomerID, PaymentDate, Amount, PaymentMode, CreatedAt)
    VALUES (@Bill4, @ShopID, @Cust4, CAST(DATEADD(DAY,-5,GETDATE()) AS DATE), 5271.00, 'UPI', GETDATE());

    -- Bill 5 — PARTIAL (aaj ka bill)
    UPDATE Shops SET CurrentBillSequence = CurrentBillSequence + 1 WHERE ShopID = @ShopID;
    INSERT INTO Bills (
        ShopID, CustomerID, BillNumber, BillDate,
        SubTotal, DiscountAmount, GSTPercent, GSTAmount,
        TotalAmount, AmountPaid, PaymentStatus, Notes, IsReturn, CreatedAt
    ) VALUES (
        @ShopID, @Cust1, '1005',
        CAST(GETDATE() AS DATE),
        2320.00, 0, 5, 116.00,
        2436.00, 1000.00, 'PARTIAL', NULL, 0, GETDATE()
    );
    DECLARE @Bill5 INT = SCOPE_IDENTITY();
    INSERT INTO BillItems (BillID, ProductID, ProductName, Quantity, UnitPrice, DiscountAmount, GSTPercent, GSTAmount, TotalAmount)
    VALUES
    (@Bill5, @Prod2, 'DAP 50kg Bag',  1, 1380.00, 0, 5, 69.00, 1449.00),
    (@Bill5, @Prod3, 'Urea 45kg Bag', 3, 290.00,  0, 5, 43.50, 913.50);
    INSERT INTO BillPayments (BillID, ShopID, CustomerID, PaymentDate, Amount, PaymentMode, CreatedAt)
    VALUES (@Bill5, @ShopID, @Cust1, CAST(GETDATE() AS DATE), 1000.00, 'Cash', GETDATE());

    -- Update shop bill sequence to 5
    UPDATE Shops SET CurrentBillSequence = 5 WHERE ShopID = @ShopID;
END
GO

-- ─── 11. EXPENSES ───
DECLARE @ShopID INT = (SELECT ShopID FROM Shops WHERE Email = 'shop@test.com');
DECLARE @CatElec INT  = (SELECT TOP 1 CategoryID FROM ExpenseCategories WHERE CategoryName = 'Electricity Bill' AND ShopID IS NULL);
DECLARE @CatRent INT  = (SELECT TOP 1 CategoryID FROM ExpenseCategories WHERE CategoryName = 'Shop Rent'        AND ShopID IS NULL);
DECLARE @CatPetrol INT= (SELECT TOP 1 CategoryID FROM ExpenseCategories WHERE CategoryName = 'Petrol / Diesel'  AND ShopID IS NULL);
DECLARE @CatSalary INT= (SELECT TOP 1 CategoryID FROM ExpenseCategories WHERE CategoryName = 'Employee Salary'  AND ShopID IS NULL);
DECLARE @CatMisc INT  = (SELECT TOP 1 CategoryID FROM ExpenseCategories WHERE CategoryName = 'Miscellaneous'    AND ShopID IS NULL);

IF NOT EXISTS (SELECT 1 FROM Expenses WHERE ShopID = @ShopID)
BEGIN
    INSERT INTO Expenses (ShopID, CategoryID, ExpenseDate, Amount, Description, PaymentMode, CreatedAt)
    VALUES
    (@ShopID, @CatRent,   CAST(DATEADD(DAY, -28, GETDATE()) AS DATE), 8000.00,  'Monthly shop rent',           'Cash',  GETDATE()),
    (@ShopID, @CatElec,   CAST(DATEADD(DAY, -20, GETDATE()) AS DATE), 1250.00,  'Electricity bill - March',    'UPI',   GETDATE()),
    (@ShopID, @CatSalary, CAST(DATEADD(DAY, -28, GETDATE()) AS DATE), 7000.00,  'Helper salary - Ramkishan',   'Cash',  GETDATE()),
    (@ShopID, @CatPetrol, CAST(DATEADD(DAY, -15, GETDATE()) AS DATE), 500.00,   'Petrol for delivery bike',    'Cash',  GETDATE()),
    (@ShopID, @CatMisc,   CAST(DATEADD(DAY, -10, GETDATE()) AS DATE), 350.00,   'Stationery and packaging',    'Cash',  GETDATE()),
    (@ShopID, @CatPetrol, CAST(DATEADD(DAY, -3,  GETDATE()) AS DATE), 400.00,   'Petrol - market visit',       'Cash',  GETDATE());
END
GO

-- ─── VERIFY DATA ───
SELECT 'AdminUsers'       AS TableName, COUNT(*) AS Rows FROM AdminUsers
UNION ALL
SELECT 'Shops',            COUNT(*) FROM Shops
UNION ALL
SELECT 'ShopSubscriptions',COUNT(*) FROM ShopSubscriptions
UNION ALL
SELECT 'ProductCategories',COUNT(*) FROM ProductCategories
UNION ALL
SELECT 'Suppliers',        COUNT(*) FROM Suppliers
UNION ALL
SELECT 'Products',         COUNT(*) FROM Products
UNION ALL
SELECT 'Customers',        COUNT(*) FROM Customers
UNION ALL
SELECT 'PurchaseOrders',   COUNT(*) FROM PurchaseOrders
UNION ALL
SELECT 'Bills',            COUNT(*) FROM Bills
UNION ALL
SELECT 'BillItems',        COUNT(*) FROM BillItems
UNION ALL
SELECT 'BillPayments',     COUNT(*) FROM BillPayments
UNION ALL
SELECT 'Expenses',         COUNT(*) FROM Expenses;

-- ─── TEST LOGINS ───
PRINT '====================================';
PRINT 'TEST LOGIN CREDENTIALS:';
PRINT 'Admin  → admin@agrobilling.com / admin123';
PRINT 'Shop   → shop@test.com         / shop123';
PRINT '====================================';
GO