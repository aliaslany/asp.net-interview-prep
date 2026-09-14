# ASP.NET MVC و Web API

---

## چرخه عمر درخواست در MVC

```
Request → Routing → Controller Factory → Action Filter
        → Model Binding → Action → Result Filter → View → Response
```

**Routing** — URL را به کنترلر و اکشن نگاشت می‌کند
**Model Binding** — داده درخواست (query string، فرم، JSON) را به پارامترهای اکشن تبدیل می‌کند
**Action Filter** — کد قبل و بعد از اکشن
**View Engine** — Razor که HTML نهایی را می‌سازد

---

## Model Binding

ASP.NET خودکار داده را به آبجکت تبدیل می‌کند:

```csharp
public ActionResult Create(ProductViewModel model)
```

منابع داده به این ترتیب بررسی می‌شوند: Form Data، Route Values، Query String.

### نکته امنیتی: Over-Posting

اگر مدل تو فیلد `IsAdmin` داشته باشد، کاربر می‌تواند در فرم آن را بفرستد و خودش را ادمین کند.

**راه‌حل‌ها:**
- ViewModel جدا از موجودیت دیتابیس بساز (بهترین راه)
- از `[Bind(Include = "Name,Price")]` استفاده کن

این نکته امنیتی را حتماً بلد باش — گاهی عمداً می‌پرسند.

---

## اعتبارسنجی

```csharp
[Required(ErrorMessage = "نام الزامی است")]
[StringLength(100)]
public string Name { get; set; }

[Range(1, 1000000000)]
public decimal Price { get; set; }
```

```csharp
if (!ModelState.IsValid)
    return View(model);
```

**قانون طلایی:** اعتبارسنجی سمت کلاینت فقط برای **تجربه کاربری** است. کاربر می‌تواند با ابزارهایی مثل Postman آن را کامل دور بزند. اعتبارسنجی سمت سرور **همیشه اجباری** است.

---

## الگوی PRG — Post-Redirect-Get

```csharp
[HttpPost]
public ActionResult Create(ProductViewModel model)
{
    var id = _service.Create(model);
    return RedirectToAction("Details", new { id });   // ← redirect، نه View
}
```

**چرا؟** اگر بعد از `POST` مستقیم `View` برگردانی، کاربر با زدن F5 دوباره فرم را ارسال می‌کند و رکورد تکراری ساخته می‌شود.

---

## Action Filter ها

| فیلتر | کِی اجرا می‌شود |
|---|---|
| `IAuthorizationFilter` | اول از همه — بررسی دسترسی |
| `IActionFilter` | قبل و بعد از اکشن |
| `IResultFilter` | قبل و بعد از رندر نتیجه |
| `IExceptionFilter` | وقتی خطای مدیریت‌نشده رخ دهد |

**کاربردهای رایج:** لاگ، احراز هویت، کش، مدیریت خطا، اندازه‌گیری زمان اجرا.

**نکته:** فیلترها جای خوبی برای **دغدغه‌های عرضی** (cross-cutting concerns) هستند — چیزهایی که در همه اکشن‌ها تکرار می‌شوند.

---

## امنیت: CSRF و XSS

### CSRF (جعل درخواست بین‌سایتی)

سایت مهاجم فرمی می‌سازد که به سایت تو `POST` می‌کند. چون کوکی کاربر خودکار ارسال می‌شود، عملیات با هویت او انجام می‌شود.

```csharp
[HttpPost]
[ValidateAntiForgeryToken]     // ← در کنترلر
public ActionResult Create(...) { }
```
```html
@Html.AntiForgeryToken()       <!-- در View -->
```

توکنی در فرم و کوکی گذاشته می‌شود که فقط سایت خودت می‌تواند بخواند.

### XSS (تزریق اسکریپت)

اگر ورودی کاربر را بدون encode در HTML بگذاری، می‌تواند `<script>` تزریق کند.

Razor به‌طور **پیش‌فرض encode می‌کند**:
```csharp
@Model.Name          // ✅ امن
@Html.Raw(Model.Name) // ❌ خطرناک — فقط برای محتوای مطمئن
```

---

# Web API و REST

## نگاشت متد HTTP به عملیات

| Method | عملیات | Idempotent؟ | Status موفق |
|---|---|---|---|
| `GET` | خواندن | ✓ | 200 |
| `POST` | ساخت | ✗ | 201 |
| `PUT` | جایگزینی کامل | ✓ | 200 یا 204 |
| `PATCH` | تغییر جزئی | ✗ | 200 یا 204 |
| `DELETE` | حذف | ✓ | 204 |

**Idempotent یعنی چه؟** اجرای چندباره همان نتیجه را می‌دهد. `DELETE` روی یک رکورد، چه یک بار اجرا شود چه ده بار، نتیجه یکسان است. ولی `POST` هر بار یک رکورد جدید می‌سازد.

**چرا مهم است؟** چون کلاینت می‌تواند درخواست idempotent را با خیال راحت retry کند.

---

## Status Code های مهم

| کد | معنی | کِی |
|---|---|---|
| `200` | OK | خواندن موفق |
| `201` | Created | بعد از POST — آدرس منبع جدید در هدر `Location` |
| `204` | No Content | موفق بدون بدنه (DELETE، PUT) |
| `400` | Bad Request | ورودی نامعتبر |
| `401` | Unauthorized | **احراز هویت نشدی** — «تو کی هستی؟» |
| `403` | Forbidden | **مجوز نداری** — «می‌دانم کی هستی، ولی اجازه نداری» |
| `404` | Not Found | منبع وجود ندارد |
| `409` | Conflict | تضاد وضعیت (مثلاً همزمانی) |
| `422` | Unprocessable | فرمت درست ولی محتوا نامعتبر |
| `500` | Internal Error | خطای سرور |

**تفاوت ۴۰۱ و ۴۰۳ سوال کلاسیک است.** حتماً درست جواب بده.

---

## طراحی URL

```
❌ /api/getProducts        ✅ GET    /api/products
❌ /api/createProduct      ✅ POST   /api/products
❌ /api/deleteProduct/5    ✅ DELETE /api/products/5
```

**قانون:** URL اسم منبع است، فعل را HTTP Method مشخص می‌کند.

**منابع تودرتو:**
```
GET /api/customers/5/orders
```

---

## نکاتی که امتیاز می‌گیرند

**۱. صفحه‌بندی را اجباری کن**

```csharp
public IHttpActionResult GetAll(int page = 1, int pageSize = 20)
{
    if (pageSize > 100) pageSize = 100;    // سقف بگذار
```

هرگز API ای ننویس که می‌تواند کل جدول را برگرداند. یک کلاینت بدرفتار می‌تواند سرور را بخواباند.

**۲. ViewModel/DTO جدا از موجودیت دیتابیس**

اگر موجودیت EF را مستقیم برگردانی:
- فیلدهای حساس (هش رمز) لو می‌رود
- Lazy Loading ممکن است کوئری‌های ناخواسته بزند
- تغییر ساختار دیتابیس، قرارداد API را می‌شکند

**۳. نسخه‌بندی**
```
/api/v1/products
```
تا وقتی API را تغییر می‌دهی، کلاینت‌های قدیمی نشکنند.

**۴. مدیریت متمرکز خطا**

با `ExceptionFilterAttribute` همه خطاها یک فرمت یکسان می‌گیرند و جزئیات داخلی (stack trace، نام جدول) به کاربر نشت نمی‌کند.

---

## Session State

| حالت | توضیح | مشکل |
|---|---|---|
| `InProc` | در حافظه همان سرور | با چند سرور یا ری‌استارت از بین می‌رود |
| `StateServer` | سرویس جداگانه | کندتر، نقطه شکست واحد |
| `SQLServer` | در دیتابیس | کندترین، ولی پایدارترین |

**سوال احتمالی:** «چرا `InProc` در محیط چندسروری مشکل‌ساز است؟»

**جواب:** چون Load Balancer ممکن است درخواست بعدی همان کاربر را به سرور دیگری بفرستد که آن session را ندارد. راه‌حل: session را در جای مشترک نگه دار، یا اصلاً stateless طراحی کن (JWT).

**این دقیقاً همان مفهوم stateless است که در بحث Load Balancer مطرح شد.**
