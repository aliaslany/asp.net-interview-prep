-- ================================================================
--  Index و بهینه‌سازی کوئری در SQL Server
-- ================================================================

-- جدول نمونه
CREATE TABLE Orders (
    Id          INT IDENTITY(1,1) PRIMARY KEY,   -- پیش‌فرض Clustered Index می‌سازد
    CustomerId  INT            NOT NULL,
    OrderDate   DATETIME       NOT NULL,
    Status      NVARCHAR(20)   NOT NULL,
    Total       DECIMAL(18,2)  NOT NULL,
    Note        NVARCHAR(MAX)  NULL
);


-- ----------------------------------------------------------------
-- Clustered Index
-- ترتیب فیزیکی ذخیره داده روی دیسک
-- هر جدول فقط یکی می‌تواند داشته باشد
-- ----------------------------------------------------------------

-- PRIMARY KEY به‌طور پیش‌فرض clustered است، ولی می‌شود صریح ساخت:
-- CREATE CLUSTERED INDEX IX_Orders_Id ON Orders(Id);


-- ----------------------------------------------------------------
-- Non-Clustered Index
-- ساختار جداگانه‌ای که اشاره‌گر به داده اصلی دارد
-- ----------------------------------------------------------------

CREATE NONCLUSTERED INDEX IX_Orders_CustomerId
    ON Orders(CustomerId);


-- ----------------------------------------------------------------
-- مشکل Key Lookup
-- ----------------------------------------------------------------

-- این کوئری index بالا را استفاده می‌کند تا سطرها را پیدا کند،
-- ولی برای گرفتن OrderDate و Total مجبور است به جدول اصلی برگردد
SELECT CustomerId, OrderDate, Total
FROM Orders
WHERE CustomerId = 123;
-- در Execution Plan این را به‌صورت "Key Lookup" می‌بینی — کند است


-- ----------------------------------------------------------------
-- راه‌حل: Covering Index
-- ستون‌های اضافه را با INCLUDE داخل خود index می‌گذاریم
-- ----------------------------------------------------------------

CREATE NONCLUSTERED INDEX IX_Orders_CustomerId_Covering
    ON Orders(CustomerId)
    INCLUDE (OrderDate, Total);
-- حالا index خودش همه‌چیز را دارد و اصلاً به جدول اصلی نمی‌رود


-- ----------------------------------------------------------------
-- Composite Index — ترتیب ستون‌ها حیاتی است
-- ----------------------------------------------------------------

CREATE NONCLUSTERED INDEX IX_Orders_Customer_Date
    ON Orders(CustomerId, OrderDate);

-- ✅ استفاده می‌شود (از چپ شروع شده)
SELECT * FROM Orders WHERE CustomerId = 1;
SELECT * FROM Orders WHERE CustomerId = 1 AND OrderDate > '2024-01-01';

-- ❌ استفاده نمی‌شود (ستون اول را رد کرده)
SELECT * FROM Orders WHERE OrderDate > '2024-01-01';

-- قانون: index مثل دفترچه تلفن مرتب‌شده بر اساس (نام خانوادگی، نام) است.
-- با نام خانوادگی می‌توانی بگردی، ولی فقط با نام کوچک نمی‌توانی.


-- ----------------------------------------------------------------
-- چیزهایی که index را بی‌اثر می‌کنند (SARGability)
-- ----------------------------------------------------------------

-- ❌ تابع روی ستون → index استفاده نمی‌شود
SELECT * FROM Orders WHERE YEAR(OrderDate) = 2024;

-- ✅ بازه به‌جای تابع
SELECT * FROM Orders
WHERE OrderDate >= '2024-01-01' AND OrderDate < '2025-01-01';


-- ❌ محاسبه روی ستون
SELECT * FROM Orders WHERE Total * 1.09 > 1000000;

-- ✅ محاسبه را به طرف دیگر ببر
SELECT * FROM Orders WHERE Total > 1000000 / 1.09;


-- ❌ LIKE با % در ابتدا → مجبور به اسکن کامل
SELECT * FROM Orders WHERE Status LIKE '%Pending';

-- ✅ % فقط در انتها
SELECT * FROM Orders WHERE Status LIKE 'Pend%';


-- ❌ SELECT * باعث Key Lookup می‌شود
SELECT * FROM Orders WHERE CustomerId = 1;

-- ✅ فقط ستون‌های لازم
SELECT Id, Total FROM Orders WHERE CustomerId = 1;


-- ----------------------------------------------------------------
-- Filtered Index — فقط روی بخشی از داده
-- ----------------------------------------------------------------

CREATE NONCLUSTERED INDEX IX_Orders_Pending
    ON Orders(OrderDate)
    WHERE Status = 'Pending';
-- اگر فقط ۲٪ سفارش‌ها Pending هستند، این index خیلی کوچک
-- و خیلی سریع خواهد بود


-- ----------------------------------------------------------------
-- بررسی Execution Plan
-- ----------------------------------------------------------------

SET STATISTICS IO ON;      -- تعداد خواندن‌های منطقی را نشان می‌دهد
SET STATISTICS TIME ON;    -- زمان CPU و زمان کل

-- در SSMS با Ctrl+M می‌توانی Execution Plan واقعی را ببینی

-- چه چیزی را در Plan نگاه کنیم:
--   Index Seek     ← مطلوب (جستجوی مستقیم)
--   Index Scan     ← نامطلوب (پیمایش کل index)
--   Table Scan     ← بدترین حالت (پیمایش کل جدول)
--   Key Lookup     ← نشانه نیاز به covering index
--   Sort           ← اگر بزرگ باشد، شاید index مرتب لازم است


-- ----------------------------------------------------------------
-- پیدا کردن index های ازدست‌رفته و بی‌استفاده
-- ----------------------------------------------------------------

-- index هایی که SQL Server فکر می‌کند لازم است
SELECT
    d.statement       AS TableName,
    d.equality_columns,
    d.inequality_columns,
    d.included_columns,
    s.avg_user_impact
FROM sys.dm_db_missing_index_details d
JOIN sys.dm_db_missing_index_groups g  ON d.index_handle = g.index_handle
JOIN sys.dm_db_missing_index_group_stats s ON g.index_group_handle = s.group_handle
ORDER BY s.avg_user_impact DESC;

-- هشدار: پیشنهادهای این view را کورکورانه اجرا نکن.
-- گاهی چند پیشنهاد را می‌شود در یک index ترکیب کرد.


-- index هایی که فقط هزینه دارند و استفاده نمی‌شوند
SELECT
    OBJECT_NAME(s.object_id) AS TableName,
    i.name                   AS IndexName,
    s.user_seeks, s.user_scans, s.user_lookups,
    s.user_updates            -- هزینه نگهداری
FROM sys.dm_db_index_usage_stats s
JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
WHERE s.user_seeks = 0 AND s.user_scans = 0 AND s.user_lookups = 0
  AND i.name IS NOT NULL;
