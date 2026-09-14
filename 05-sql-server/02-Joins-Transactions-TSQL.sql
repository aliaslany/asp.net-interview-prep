-- ================================================================
--  JOIN، تجمیع، تراکنش و Stored Procedure
-- ================================================================

-- ----------------------------------------------------------------
-- انواع JOIN
-- ----------------------------------------------------------------

-- INNER JOIN — فقط سطرهایی که در هر دو طرف تطابق دارند
SELECT c.Name, o.Total
FROM Customers c
INNER JOIN Orders o ON c.Id = o.CustomerId;


-- LEFT JOIN — همه مشتری‌ها، حتی آن‌هایی که سفارش ندارند
SELECT c.Name, o.Total
FROM Customers c
LEFT JOIN Orders o ON c.Id = o.CustomerId;
-- برای مشتری بدون سفارش، ستون‌های o برابر NULL می‌شوند


-- پیدا کردن مشتریانی که هیچ سفارشی ندارند
SELECT c.Name
FROM Customers c
LEFT JOIN Orders o ON c.Id = o.CustomerId
WHERE o.Id IS NULL;          -- ← ترفند رایج و پرکاربرد


-- ❌ تله: گذاشتن شرط جدول راست در WHERE به‌جای ON
SELECT c.Name, o.Total
FROM Customers c
LEFT JOIN Orders o ON c.Id = o.CustomerId
WHERE o.Status = 'Paid';     -- LEFT JOIN را عملاً به INNER JOIN تبدیل می‌کند!

-- ✅ درست: شرط را در ON بگذار
SELECT c.Name, o.Total
FROM Customers c
LEFT JOIN Orders o ON c.Id = o.CustomerId AND o.Status = 'Paid';


-- CROSS JOIN — ضرب دکارتی (هر سطر با هر سطر)
SELECT c.Name, p.Name
FROM Customers c
CROSS JOIN Products p;


-- SELF JOIN — جدول به خودش وصل می‌شود
SELECT e.Name AS Employee, m.Name AS Manager
FROM Employees e
LEFT JOIN Employees m ON e.ManagerId = m.Id;


-- ----------------------------------------------------------------
-- GROUP BY ، WHERE و HAVING
-- ----------------------------------------------------------------

SELECT
    c.Name,
    COUNT(o.Id)   AS OrderCount,
    SUM(o.Total)  AS TotalSpent,
    AVG(o.Total)  AS AvgOrder
FROM Customers c
INNER JOIN Orders o ON c.Id = o.CustomerId
WHERE o.OrderDate >= '2024-01-01'    -- فیلتر قبل از گروه‌بندی
GROUP BY c.Id, c.Name
HAVING SUM(o.Total) > 10000000       -- فیلتر بعد از گروه‌بندی
ORDER BY TotalSpent DESC;

-- ترتیب اجرای منطقی دستورات SQL:
--   FROM → JOIN → WHERE → GROUP BY → HAVING → SELECT → ORDER BY
-- به همین دلیل نمی‌توانی در WHERE از نام مستعار SELECT استفاده کنی


-- ----------------------------------------------------------------
-- Window Functions — بدون از دست دادن جزئیات، تجمیع کن
-- ----------------------------------------------------------------

SELECT
    o.Id,
    o.CustomerId,
    o.Total,
    SUM(o.Total)   OVER (PARTITION BY o.CustomerId)            AS CustomerTotal,
    ROW_NUMBER()   OVER (PARTITION BY o.CustomerId
                         ORDER BY o.OrderDate DESC)            AS RowNum,
    RANK()         OVER (ORDER BY o.Total DESC)                AS PriceRank
FROM Orders o;

-- کاربرد رایج: آخرین سفارش هر مشتری
WITH Ranked AS (
    SELECT *,
           ROW_NUMBER() OVER (PARTITION BY CustomerId
                              ORDER BY OrderDate DESC) AS rn
    FROM Orders
)
SELECT * FROM Ranked WHERE rn = 1;


-- ----------------------------------------------------------------
-- CTE — کوئری موقت نام‌دار برای خوانایی
-- ----------------------------------------------------------------

WITH HighValueCustomers AS (
    SELECT CustomerId, SUM(Total) AS TotalSpent
    FROM Orders
    GROUP BY CustomerId
    HAVING SUM(Total) > 50000000
)
SELECT c.Name, h.TotalSpent
FROM HighValueCustomers h
INNER JOIN Customers c ON c.Id = h.CustomerId;


-- ----------------------------------------------------------------
-- صفحه‌بندی
-- ----------------------------------------------------------------

SELECT Id, Total, OrderDate
FROM Orders
ORDER BY OrderDate DESC
OFFSET 20 ROWS FETCH NEXT 20 ROWS ONLY;   -- صفحه دوم، ۲۰ تایی
-- نکته: ORDER BY برای OFFSET اجباری است


-- ----------------------------------------------------------------
-- تراکنش
-- ----------------------------------------------------------------

BEGIN TRY
    BEGIN TRANSACTION;

        UPDATE Accounts SET Balance = Balance - 1000000 WHERE Id = 1;

        IF (SELECT Balance FROM Accounts WHERE Id = 1) < 0
            THROW 50001, N'موجودی کافی نیست', 1;

        UPDATE Accounts SET Balance = Balance + 1000000 WHERE Id = 2;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;      -- خطا را به لایه بالاتر پاس بده
END CATCH;


-- ----------------------------------------------------------------
-- Stored Procedure با پارامتر
-- ----------------------------------------------------------------

CREATE PROCEDURE usp_GetCustomerOrders
    @CustomerId  INT,
    @FromDate    DATETIME = NULL,       -- پارامتر اختیاری
    @TotalCount  INT OUTPUT             -- پارامتر خروجی
AS
BEGIN
    SET NOCOUNT ON;    -- از ارسال پیام "N rows affected" جلوگیری می‌کند
                       -- در حجم بالا کارایی را بهتر می‌کند

    SELECT @TotalCount = COUNT(*)
    FROM Orders
    WHERE CustomerId = @CustomerId
      AND (@FromDate IS NULL OR OrderDate >= @FromDate);

    SELECT Id, OrderDate, Total, Status
    FROM Orders
    WHERE CustomerId = @CustomerId
      AND (@FromDate IS NULL OR OrderDate >= @FromDate)
    ORDER BY OrderDate DESC;
END;
GO

-- فراخوانی
DECLARE @count INT;
EXEC usp_GetCustomerOrders @CustomerId = 1, @TotalCount = @count OUTPUT;
SELECT @count AS TotalOrders;


-- ----------------------------------------------------------------
-- SQL Injection — اشتباه و راه درست
-- ----------------------------------------------------------------

-- ❌ فاجعه امنیتی: الحاق رشته
-- DECLARE @sql NVARCHAR(MAX);
-- SET @sql = N'SELECT * FROM Users WHERE Name = ''' + @userInput + '''';
-- EXEC(@sql);
--
-- اگر ورودی این باشد:  '; DROP TABLE Users; --
-- کل جدول حذف می‌شود

-- ✅ استفاده از پارامتر
EXEC sp_executesql
    N'SELECT * FROM Users WHERE Name = @name',
    N'@name NVARCHAR(100)',
    @name = N'ali';

-- در سمت C# هم همیشه SqlParameter استفاده کن، نه string concatenation


-- ----------------------------------------------------------------
-- MERGE — درج یا به‌روزرسانی در یک دستور
-- ----------------------------------------------------------------

MERGE INTO Products AS target
USING (SELECT 1 AS Id, N'لپ‌تاپ' AS Name, 50000000 AS Price) AS source
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET Name = source.Name, Price = source.Price
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Price) VALUES (source.Id, source.Name, source.Price);


-- ----------------------------------------------------------------
-- DELETE / TRUNCATE / DROP
-- ----------------------------------------------------------------

DELETE FROM Orders WHERE OrderDate < '2020-01-01';   -- شرط‌دار، لاگ می‌شود، قابل rollback
TRUNCATE TABLE TempOrders;                            -- کل جدول، سریع، IDENTITY ریست می‌شود
DROP TABLE TempOrders;                                -- خود جدول حذف می‌شود
