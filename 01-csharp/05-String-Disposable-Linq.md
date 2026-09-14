# string، IDisposable و LINQ

سه موضوع کوتاه که تقریباً همیشه پرسیده می‌شوند.

---

# ۱. string در برابر StringBuilder

## چرا string غیرقابل تغییر است؟

در C# رشته **immutable** است. یعنی وقتی می‌نویسی:

```csharp
string s = "Hello";
s += " World";
```

رشته `"Hello"` تغییر نکرد. یک رشته کاملاً جدید ساخته شد و `s` به آن اشاره می‌کند. رشته قدیمی زباله می‌شود.

**چرا این‌طور طراحی شده؟**
- امنیت رشته‌ها (رمز، مسیر فایل) — کسی نمی‌تواند بعد از اعتبارسنجی آن را عوض کند
- thread-safe بودن ذاتی
- امکان String Interning (کش کردن رشته‌های یکسان)

## تأثیرش در حلقه

```csharp
string result = "";
foreach (var item in items)
    result += item;        // ❌ هر بار یک آبجکت جدید
```

با ۱۰ هزار آیتم، ۱۰ هزار رشته موقت ساخته می‌شود که همه باید توسط GC جمع شوند.

```csharp
var sb = new StringBuilder();
foreach (var item in items)
    sb.Append(item);       // ✅ یک بافر قابل تغییر
return sb.ToString();
```

**کِی StringBuilder؟** وقتی تعداد الحاق‌ها **نامشخص یا زیاد** است. برای سه‌چهار تا رشته ثابت، `a + b + c` کاملاً خوب است — کامپایلر خودش آن را به `string.Concat` تبدیل می‌کند.

## نکته مقایسه رشته

```csharp
// ❌ برای مقایسه فنی
a.ToLower() == b.ToLower()

// ✅
string.Equals(a, b, StringComparison.OrdinalIgnoreCase)
```

`ToLower()` هم یک رشته جدید می‌سازد (هزینه)، هم به فرهنگ سیستم وابسته است. مثال معروفش حرف `I` در زبان ترکی است که رفتار متفاوتی دارد و باعث باگ‌های عجیب می‌شود.

---

# ۲. IDisposable و using

## مشکلی که حل می‌کند

Garbage Collector فقط **حافظه مدیریت‌شده** را آزاد می‌کند. چیزهایی مثل اتصال دیتابیس، handle فایل، یا socket را **نمی‌شناسد**.

اگر آزادشان نکنی، نشتی منابع (resource leak) داری. در مورد اتصال دیتابیس، Connection Pool پر می‌شود و سرور از کار می‌افتد.

## چرا `using` نه `Close()` دستی؟

```csharp
// ❌
var conn = new SqlConnection(cs);
conn.Open();
DoWork(conn);      // اگر اینجا خطا بدهد...
conn.Close();      // ...این هرگز اجرا نمی‌شود
```

```csharp
// ✅
using (var conn = new SqlConnection(cs))
{
    conn.Open();
    DoWork(conn);
}   // Dispose تضمین‌شده، حتی با خطا
```

`using` در واقع همان `try/finally` است که کامپایلر برایت می‌نویسد.

## الگوی استاندارد Dispose

اگر خودت کلاسی می‌نویسی که منبع نگه می‌دارد:

```csharp
public void Dispose()
{
    Dispose(true);
    GC.SuppressFinalize(this);
}

protected virtual void Dispose(bool disposing)
{
    if (_disposed) return;
    if (disposing) { /* منابع مدیریت‌شده */ }
    /* منابع غیرمدیریت‌شده */
    _disposed = true;
}
```

`GC.SuppressFinalize` به GC می‌گوید «منابع را دستی آزاد کردم، نیازی به finalizer نیست» — این باعث می‌شود آبجکت زودتر از حافظه آزاد شود.

---

# ۳. LINQ

## Deferred Execution — مهم‌ترین نکته

کوئری LINQ تا وقتی نتیجه‌اش خواسته نشود **اجرا نمی‌شود**.

```csharp
var list = new List<int> { 1, 2, 3 };
var query = list.Where(x => x > 1);   // هنوز اجرا نشده

list.Add(4);

query.Count();    // 3 ← عدد ۴ هم شمرده شد!
```

**این می‌تواند باگ بسازد.** اگر می‌خواهی عکس فوری از داده بگیری، `ToList()` بزن.

**توابعی که کوئری را اجرا می‌کنند:** `ToList`, `ToArray`, `Count`, `First`, `Any`, `Sum`, `foreach`.

## First / Single / و نسخه‌های OrDefault

| متد | اگر هیچی نبود | اگر بیشتر از یکی بود |
|---|---|---|
| `First` | خطا | اولی را می‌دهد |
| `FirstOrDefault` | `null` یا مقدار پیش‌فرض | اولی را می‌دهد |
| `Single` | خطا | **خطا** |
| `SingleOrDefault` | `null` | **خطا** |

**کِی `Single`؟** وقتی منطق برنامه می‌گوید نتیجه حتماً باید یکتا باشد — مثلاً جستجو بر اساس کلید اصلی. اگر بیشتر از یکی برگشت، یعنی باگ جدی داری و بهتر است همان‌جا سر و صدا کند تا بعداً در جای عجیبی خودش را نشان دهد.

**در عمل** `FirstOrDefault` بیشتر استفاده می‌شود چون کمی سریع‌تر است (به محض پیدا کردن اولی متوقف می‌شود، در حالی که `Single` باید تا آخر بگردد تا مطمئن شود دومی وجود ندارد).

## عملیات پرکاربرد

| عملیات | کار |
|---|---|
| `Where` | فیلتر |
| `Select` | تبدیل / انتخاب ستون |
| `OrderBy` / `ThenBy` | مرتب‌سازی چندسطحی |
| `GroupBy` | گروه‌بندی |
| `Skip` / `Take` | صفحه‌بندی |
| `Any` / `All` | بررسی وجود |
| `Sum` / `Average` / `Max` | تجمیع |
| `Distinct` | حذف تکراری |
| `SelectMany` | تخت کردن مجموعه تودرتو |

## نکته‌ای که با EF ترکیب می‌شود

یادت باشد وقتی LINQ روی `IQueryable` (یعنی روی EF) کار می‌کند، به SQL ترجمه می‌شود. وقتی روی `List` کار می‌کند، در حافظه اجرا می‌شود.

`Where` یکسان نوشته می‌شود ولی رفتارش کاملاً فرق دارد — این همان موضوع فایل `01-IEnumerable-vs-IQueryable` است.
