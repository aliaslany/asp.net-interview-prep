# Index و بهینه‌سازی در SQL Server

---

## Clustered در برابر Non-Clustered

این پرتکرارترین سوال SQL Server در مصاحبه است.

### Clustered Index

**ترتیب فیزیکی ذخیره داده روی دیسک.**

مثل **دفترچه تلفن** که خود اطلاعات بر اساس نام خانوادگی مرتب شده. خود داده مرتب است.

- هر جدول فقط **یکی** می‌تواند داشته باشد (چون داده فقط به یک ترتیب قابل ذخیره است)
- `PRIMARY KEY` به‌طور پیش‌فرض clustered ساخته می‌شود
- جدولی که clustered index ندارد به آن **Heap** می‌گویند

### Non-Clustered Index

**ساختار جداگانه‌ای که اشاره‌گر به داده اصلی دارد.**

مثل **فهرست انتهای کتاب**: خودش محتوا نیست، آدرس محتواست.

- می‌توانی چندتا داشته باشی
- فضای اضافه می‌گیرد
- نوشتن را کند می‌کند

---

## Key Lookup — مفهومی که امتیاز می‌گیرد

```sql
CREATE INDEX IX_Orders_CustomerId ON Orders(CustomerId);

SELECT CustomerId, OrderDate, Total
FROM Orders WHERE CustomerId = 123;
```

index فقط `CustomerId` را دارد. برای گرفتن `OrderDate` و `Total` باید به جدول اصلی برگردد — این می‌شود **Key Lookup** که کند است.

### راه‌حل: Covering Index

```sql
CREATE INDEX IX_Orders_CustomerId_Covering
    ON Orders(CustomerId)
    INCLUDE (OrderDate, Total);
```

حالا index خودش همه ستون‌های لازم را دارد و اصلاً به جدول اصلی نمی‌رود.

**تفاوت ستون کلید و ستون INCLUDE:**
- ستون‌های **کلید** قابل جستجو و مرتب‌سازی‌اند
- ستون‌های **INCLUDE** فقط ذخیره می‌شوند تا Key Lookup لازم نشود (فضای کمتری هم می‌گیرند)

**اگر این را سر مصاحبه بگویی، عملاً نشان داده‌ای که Execution Plan خوانده‌ای.**

---

## Composite Index — ترتیب ستون‌ها حیاتی است

```sql
CREATE INDEX IX_Orders_Customer_Date ON Orders(CustomerId, OrderDate);
```

| کوئری | index استفاده می‌شود؟ |
|---|---|
| `WHERE CustomerId = 1` | ✓ |
| `WHERE CustomerId = 1 AND OrderDate > '...'` | ✓ |
| `WHERE OrderDate > '...'` | ✗ |

**قاعده پیشوند چپ (Leftmost Prefix):** index مثل دفترچه تلفنی است که بر اساس (نام خانوادگی، نام) مرتب شده. با نام خانوادگی می‌توانی بگردی، ولی **فقط با نام کوچک نمی‌توانی**.

**پس ستون اول را چه چیزی بگذاریم؟** ستونی که بیشتر در `WHERE` می‌آید و **انتخاب‌پذیری (selectivity)** بالاتری دارد — یعنی مقادیر متنوع‌تری دارد.

---

## SARGability — چه چیزی index را بی‌اثر می‌کند

**SARGable** = Search ARGument able = کوئری‌ای که می‌تواند از index استفاده کند.

| ❌ بد | ✅ خوب |
|---|---|
| `WHERE YEAR(OrderDate) = 2024` | `WHERE OrderDate >= '2024-01-01' AND OrderDate < '2025-01-01'` |
| `WHERE Total * 1.09 > 1000000` | `WHERE Total > 1000000 / 1.09` |
| `WHERE Status LIKE '%Pending'` | `WHERE Status LIKE 'Pend%'` |
| `SELECT *` | فقط ستون‌های لازم |

**قانون کلی:** به محض اینکه **تابع یا محاسبه‌ای روی ستون** اعمال کنی، SQL Server نمی‌تواند از index استفاده کند و مجبور به اسکن کامل می‌شود.

دلیلش ساده است: index بر اساس مقدار **خام** ستون ساخته شده، نه بر اساس نتیجه تابع.

---

## Filtered Index

```sql
CREATE INDEX IX_Orders_Pending
    ON Orders(OrderDate)
    WHERE Status = 'Pending';
```

اگر فقط ۲٪ سفارش‌ها `Pending` هستند، این index:
- خیلی کوچک‌تر است
- سریع‌تر جستجو می‌شود
- هزینه نگهداری کمتری دارد

**کاربرد رایج:** ستون‌های وضعیت، رکوردهای حذف‌نشده (`WHERE IsDeleted = 0`).

---

## هزینه Index — حتماً بگو

اگر بپرسند «پس روی همه ستون‌ها index بذاریم؟» جواب **نه** است:

- فضای دیسک می‌گیرد
- **هر `INSERT`/`UPDATE`/`DELETE` کندتر می‌شود** چون index هم باید به‌روز شود
- فشار بیشتر روی لاگ تراکنش
- Query Optimizer با گزینه‌های زیاد ممکن است انتخاب بدی بکند

**قاعده عملی:** index را بر اساس کوئری‌های واقعی و پرتکرار بساز، نه از روی حدس.

---

## چطور کوئری کند را تشخیص بدهیم؟

**جواب روش‌مند بده، نه حدسی:**

> «اول Execution Plan را نگاه می‌کنم و دنبال Table Scan یا Index Scan می‌گردم که باید Seek باشد. بعد Key Lookup ها را بررسی می‌کنم که ببینم covering index لازم است یا نه. با SET STATISTICS IO هم تعداد خواندن‌های منطقی را مقایسه می‌کنم — این عدد از زمان اجرا قابل اعتمادتر است چون به بار سرور وابسته نیست.»

### چه چیزی را در Plan نگاه کنیم

| نشانه | معنی |
|---|---|
| **Index Seek** | ✓ مطلوب — جستجوی مستقیم |
| **Index Scan** | ✗ کل index پیمایش شده |
| **Table Scan** | ✗✗ بدترین حالت |
| **Key Lookup** | نیاز به covering index |
| **Sort** با هزینه بالا | شاید index مرتب لازم است |
| فلش‌های ضخیم | حجم داده زیاد در آن مرحله |

---

## ابزارهای تشخیص

**index های ازدست‌رفته:** `sys.dm_db_missing_index_details`

**هشدار:** پیشنهادهای این view را کورکورانه اجرا نکن. گاهی سه پیشنهاد مشابه را می‌شود در یک index ترکیب کرد، و گاهی پیشنهاد اصلاً ارزش هزینه نوشتنش را ندارد.

**index های بی‌استفاده:** `sys.dm_db_index_usage_stats` — index هایی که `user_seeks` و `user_scans` صفر دارند ولی `user_updates` بالا دارند، فقط هزینه‌اند.

---

## چند تعریف سریع

**Fragmentation** — با گذشت زمان و تغییرات، ترتیب فیزیکی index به هم می‌ریزد. با `REBUILD` یا `REORGANIZE` مرتب می‌شود.

**Fill Factor** — چقدر از هر صفحه index پر شود. اگر ۱۰۰٪ باشد، هر درج جدید باعث شکستن صفحه می‌شود.

**Statistics** — آماری که Query Optimizer برای تخمین تعداد سطرها استفاده می‌کند. اگر کهنه باشد، plan بدی انتخاب می‌شود. `UPDATE STATISTICS` مشکل را حل می‌کند.
