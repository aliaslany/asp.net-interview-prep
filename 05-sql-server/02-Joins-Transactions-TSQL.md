# JOIN، تجمیع، تراکنش و T-SQL

---

## انواع JOIN

| نوع | چه چیزی برمی‌گرداند |
|---|---|
| `INNER JOIN` | فقط سطرهای دارای تطابق در هر دو طرف |
| `LEFT JOIN` | همه سطرهای جدول چپ + تطابق‌ها (بقیه NULL) |
| `RIGHT JOIN` | همه سطرهای جدول راست |
| `FULL OUTER JOIN` | همه سطرهای هر دو طرف |
| `CROSS JOIN` | ضرب دکارتی — هر سطر با هر سطر |
| `SELF JOIN` | جدول به خودش (مثل کارمند و مدیرش) |

---

## ترفند پرکاربرد: پیدا کردن سطرهای بدون تطابق

```sql
SELECT c.Name
FROM Customers c
LEFT JOIN Orders o ON c.Id = o.CustomerId
WHERE o.Id IS NULL;
```

«مشتریانی که هیچ سفارشی ندارند». این الگو را حتماً بلد باش — خیلی پرسیده می‌شود.

---

## تله مهم: شرط در `WHERE` به‌جای `ON`

```sql
-- ❌ این LEFT JOIN را عملاً به INNER JOIN تبدیل می‌کند
LEFT JOIN Orders o ON c.Id = o.CustomerId
WHERE o.Status = 'Paid'

-- ✅ درست
LEFT JOIN Orders o ON c.Id = o.CustomerId AND o.Status = 'Paid'
```

**چرا؟** در `LEFT JOIN`، مشتری بدون سفارش سطری با `o.Status = NULL` می‌گیرد. شرط `WHERE o.Status = 'Paid'` آن سطر را حذف می‌کند — یعنی همان کاری که `INNER JOIN` می‌کرد.

این سوال را عمداً می‌پرسند چون باگ رایجی است که خیلی‌ها متوجهش نمی‌شوند.

---

## `WHERE` در برابر `HAVING`

```sql
WHERE o.OrderDate >= '2024-01-01'      -- قبل از گروه‌بندی، روی تک‌تک سطرها
GROUP BY c.Id, c.Name
HAVING SUM(o.Total) > 10000000         -- بعد از گروه‌بندی، روی نتیجه تجمیع
```

### ترتیب اجرای منطقی SQL

```
FROM → JOIN → WHERE → GROUP BY → HAVING → SELECT → ORDER BY
```

**این توضیح می‌دهد چرا:**
- در `WHERE` نمی‌توانی از نام مستعار `SELECT` استفاده کنی (چون `SELECT` بعداً اجرا می‌شود)
- در `ORDER BY` می‌توانی (چون بعد از `SELECT` است)
- `HAVING` می‌تواند از توابع تجمیعی استفاده کند ولی `WHERE` نمی‌تواند

گفتن این ترتیب سر مصاحبه خیلی خوب به گوش می‌رسد.

---

## Window Functions

تجمیع **بدون** از دست دادن جزئیات سطرها:

```sql
SELECT
    o.Id,
    o.Total,
    SUM(o.Total) OVER (PARTITION BY o.CustomerId) AS CustomerTotal,
    ROW_NUMBER() OVER (PARTITION BY o.CustomerId
                       ORDER BY o.OrderDate DESC) AS rn
FROM Orders o;
```

با `GROUP BY` سطرهای جزئی را از دست می‌دهی. با window function هم جزئیات را داری هم تجمیع را.

### کاربرد رایج: آخرین رکورد هر گروه

```sql
WITH Ranked AS (
    SELECT *, ROW_NUMBER() OVER (PARTITION BY CustomerId
                                 ORDER BY OrderDate DESC) AS rn
    FROM Orders
)
SELECT * FROM Ranked WHERE rn = 1;
```

«آخرین سفارش هر مشتری». این الگو را حتماً یاد بگیر — بارها به کارت می‌آید.

### تفاوت `ROW_NUMBER` و `RANK` و `DENSE_RANK`

با مقادیر 10, 10, 9:

| تابع | خروجی |
|---|---|
| `ROW_NUMBER` | 1, 2, 3 |
| `RANK` | 1, 1, 3 |
| `DENSE_RANK` | 1, 1, 2 |

---

## CTE

```sql
WITH HighValueCustomers AS (
    SELECT CustomerId, SUM(Total) AS TotalSpent
    FROM Orders GROUP BY CustomerId
    HAVING SUM(Total) > 50000000
)
SELECT c.Name, h.TotalSpent
FROM HighValueCustomers h
JOIN Customers c ON c.Id = h.CustomerId;
```

**فایده‌اش:** خوانایی. به‌جای زیرکوئری‌های تودرتو، مراحل را نام‌دار و پشت سر هم می‌نویسی.

CTE بازگشتی (`WITH ... UNION ALL`) هم برای ساختارهای درختی مثل چارت سازمانی استفاده می‌شود.

---

## صفحه‌بندی

```sql
SELECT Id, Total FROM Orders
ORDER BY OrderDate DESC
OFFSET 20 ROWS FETCH NEXT 20 ROWS ONLY;
```

`ORDER BY` برای `OFFSET` **اجباری** است.

**نکته کارایی:** با شماره صفحه بزرگ (مثلاً صفحه ۱۰۰۰)، `OFFSET` کند می‌شود چون باید همه سطرهای قبلی را بخواند و دور بریزد. برای داده خیلی بزرگ، روش **keyset pagination** بهتر است:

```sql
WHERE OrderDate < @lastSeenDate ORDER BY OrderDate DESC
```

---

## تراکنش در T-SQL

```sql
BEGIN TRY
    BEGIN TRANSACTION;
        -- عملیات
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
```

**چرا `@@TRANCOUNT` را چک می‌کنیم؟** چون اگر تراکنش قبلاً rollback شده باشد، `ROLLBACK` دوباره خودش خطا می‌دهد.

**`THROW` در برابر `RAISERROR`:** `THROW` جدیدتر و ساده‌تر است و خطای اصلی را دست‌نخورده به لایه بالا می‌فرستد.

---

## Stored Procedure

```sql
CREATE PROCEDURE usp_GetCustomerOrders
    @CustomerId INT,
    @FromDate   DATETIME = NULL,     -- اختیاری
    @TotalCount INT OUTPUT           -- خروجی
AS
BEGIN
    SET NOCOUNT ON;
    ...
END
```

**`SET NOCOUNT ON` چه می‌کند؟** جلوی ارسال پیام «N rows affected» را می‌گیرد. در حلقه‌ها و حجم بالا، ترافیک شبکه کمتری تولید می‌شود.

### مزایا و معایب Stored Procedure

| مزیت | عیب |
|---|---|
| plan اجرا کش می‌شود | نسخه‌بندی با گیت سخت‌تر است |
| ترافیک شبکه کمتر | منطق کسب‌وکار در دیتابیس پخش می‌شود |
| مجوزدهی دقیق‌تر | تست کردنش سخت‌تر است |
| بدون ریسک SQL Injection | مهاجرت به دیتابیس دیگر سخت می‌شود |

**جواب متعادل اگر پرسیدند:**
> «برای کوئری‌های پیچیده و پرتکرار یا عملیات انبوه از Stored Procedure استفاده می‌کنم. برای CRUD معمولی، EF کافی است و نگهداری کدش راحت‌تر است.»

---

## SQL Injection

```sql
-- ❌ الحاق رشته
SET @sql = N'SELECT * FROM Users WHERE Name = ''' + @input + '''';
EXEC(@sql);
```

اگر ورودی این باشد: `'; DROP TABLE Users; --` کل جدول حذف می‌شود.

```sql
-- ✅ پارامتری
EXEC sp_executesql
    N'SELECT * FROM Users WHERE Name = @name',
    N'@name NVARCHAR(100)', @name = @input;
```

**در سمت C# هم همین قانون:** همیشه `SqlParameter` استفاده کن، هرگز رشته‌ها را به هم نچسبان. EF به‌طور پیش‌فرض پارامتری کار می‌کند — مگر اینکه از `SqlQuery` با الحاق رشته استفاده کنی.

---

## DELETE / TRUNCATE / DROP

| | کار | rollback | IDENTITY | تریگر |
|---|---|---|---|---|
| `DELETE` | سطرهای شرط‌دار | ✓ | دست‌نخورده | فعال می‌شود |
| `TRUNCATE` | کل جدول | معمولاً ✗ | ریست می‌شود | فعال نمی‌شود |
| `DROP` | خود جدول | ✗ | — | — |

`TRUNCATE` خیلی سریع‌تر است چون سطر به سطر لاگ نمی‌کند، فقط صفحات داده را آزاد می‌کند.

---

## Isolation Levels

| سطح | مشکل جلوگیری‌شده |
|---|---|
| `READ UNCOMMITTED` | هیچ — dirty read می‌دهد |
| `READ COMMITTED` | dirty read (پیش‌فرض SQL Server) |
| `REPEATABLE READ` | + non-repeatable read |
| `SERIALIZABLE` | + phantom read (کندترین) |
| `SNAPSHOT` | همه، بدون قفل‌گذاری (با نسخه‌سازی) |

**`WITH (NOLOCK)`** معادل `READ UNCOMMITTED` است. سریع است ولی ممکن است داده‌ای را بخوانی که بعداً rollback می‌شود. در گزارش‌های تقریبی قابل قبول است، در عملیات مالی هرگز.
