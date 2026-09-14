# Singleton، Factory و Strategy

سه الگوی پرتکراری که تقریباً همیشه در مصاحبه‌های .NET پرسیده می‌شوند.

---

# Singleton

**تعریف:** تضمین اینکه از یک کلاس فقط **یک نمونه** در کل برنامه وجود داشته باشد.

## کجا واقعاً لازم است؟

- کش در حافظه
- Connection Pool
- تنظیمات برنامه
- Logger

## نسخه اشتباه و چرا می‌شکند

```csharp
if (_instance == null)
    _instance = new BadSingleton();
```

اگر **دو رشته همزمان** به خط `if` برسند و هر دو `null` ببینند، هر دو یک نمونه می‌سازند. دیگر singleton نیست.

## سه راه درست

### ۱. قفل دوگانه (Double-Check Locking)

```csharp
if (_instance == null)          // سریع، بدون قفل
{
    lock (_lock)
    {
        if (_instance == null)  // امن، داخل قفل
            _instance = new LockedSingleton();
    }
}
```

**چرا دو بار چک می‌کنیم؟** بررسی اول برای کارایی است — بعد از ساخته شدن نمونه، دیگر هیچ رشته‌ای وارد قفل نمی‌شود. بررسی دوم برای درستی است.

### ۲. `Lazy<T>` — تمیزترین راه در .NET

```csharp
private static readonly Lazy<LazySingleton> _lazy =
    new Lazy<LazySingleton>(() => new LazySingleton());

public static LazySingleton Instance => _lazy.Value;
```

`Lazy<T>` به‌طور پیش‌فرض thread-safe است و تا اولین دسترسی چیزی ساخته نمی‌شود.

**این جوابی است که سر مصاحبه باید بدهی** — نشان می‌دهد ابزارهای مدرن .NET را می‌شناسی.

### ۳. `static readonly`

```csharp
public static readonly EagerSingleton Instance = new EagerSingleton();
```

ساده‌ترین راه. CLR تضمین می‌کند فیلد static فقط یک بار مقداردهی شود.

**عیب:** حتی اگر هیچ‌وقت استفاده نشود، ساخته می‌شود.

---

## انتقاد مهم به Singleton

این نکته را **خودت** مطرح کن — نشان می‌دهد فقط الگو را حفظ نکرده‌ای:

> «Singleton را معمولاً یک anti-pattern می‌دانند چون:
> - وضعیت سراسری (global state) می‌سازد که تست را سخت می‌کند
> - وابستگی پنهان ایجاد می‌کند — با دیدن سازنده کلاس نمی‌فهمی به چه چیزهایی وابسته است
> - نقض Dependency Inversion است
>
> در .NET مدرن، معمولاً به‌جای الگوی Singleton، کلاس را با طول عمر Singleton در **IoC Container** ثبت می‌کنم. اینطوری هم یک نمونه دارم، هم قابل تزریق و تست است.»

---

# Factory

**تعریف:** ساخت آبجکت بدون اینکه کد مصرف‌کننده بداند چطور ساخته می‌شود.

## مشکلی که حل می‌کند

```csharp
if (gateway == "saman") g = new SamanGateway();
else if (gateway == "mellat") g = new MellatGateway();
```

این `if` در هر جایی که پرداخت لازم است تکرار می‌شود. اگر درگاه جدیدی اضافه شود، باید همه‌جا را پیدا کنی و عوض کنی.

## راه‌حل

منطق ساخت را در یک جا متمرکز کن:

```csharp
public class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly Dictionary<string, Func<IPaymentGateway>> _map = ...
    public IPaymentGateway Create(string name) => _map[name]();
}
```

حالا `CheckoutService` اصلاً نمی‌داند چند درگاه وجود دارد.

**نکته پیاده‌سازی:** استفاده از `Dictionary<string, Func<T>>` به‌جای `switch`، خود Factory را هم مطابق اصل Open/Closed می‌کند — برای افزودن درگاه جدید فقط یک سطر به دیکشنری اضافه می‌شود.

## انواع Factory

| نوع | کار |
|---|---|
| **Simple Factory** | یک متد که بر اساس ورودی آبجکت می‌سازد (همان چیزی که در کد است) |
| **Factory Method** | کلاس پایه متد ساخت را abstract می‌گذارد، فرزندان تصمیم می‌گیرند |
| **Abstract Factory** | ساخت یک **خانواده** از آبجکت‌های مرتبط |

در مصاحبه معمولاً همان Simple Factory کافی است، ولی دانستن اینکه سه نوع دارد امتیاز می‌گیرد.

---

# Strategy

**تعریف:** تعریف خانواده‌ای از الگوریتم‌ها، کپسوله کردن هرکدام، و قابل تعویض کردنشان در **زمان اجرا**.

## مثال

```csharp
var calculator = new ShippingCalculator(new StandardShipping());

if (orderTotal > 500_000)
    calculator.SetStrategy(new FreeShipping());    // ← عوض شد
```

بدون Strategy، این کد پر از `if` می‌شد که با هر قانون جدید بزرگ‌تر می‌شود.

## ارتباط با SOLID

Strategy مستقیماً **Open/Closed** را پیاده می‌کند: برای افزودن روش ارسال جدید، فقط یک کلاس اضافه می‌کنی. `ShippingCalculator` دست نمی‌خورد.

---

## تفاوت Factory و Strategy — سوال پرتکرار

این دو شبیه به‌نظر می‌رسند چون هر دو با interface کار می‌کنند، ولی هدفشان فرق دارد:

| | Factory | Strategy |
|---|---|---|
| سوال اصلی | «کدام آبجکت را **بسازم**؟» | «کدام الگوریتم را **اجرا کنم**؟» |
| موضوع | ساخت (creation) | رفتار (behavior) |
| زمان | معمولاً یک بار در ابتدا | می‌تواند وسط کار عوض شود |
| دسته | Creational Pattern | Behavioral Pattern |

**در عمل زیاد با هم استفاده می‌شوند:** یک Factory که Strategy مناسب را بر اساس شرایط می‌سازد.

---

## سایر الگوهایی که خوب است بشناسی

| الگو | یک‌خطی |
|---|---|
| **Observer** | وقتی وضعیت تغییر کرد، همه مشترکین خبردار شوند (مبنای event در C#) |
| **Decorator** | افزودن رفتار به آبجکت بدون تغییر کلاسش (مثل `Stream` در .NET) |
| **Adapter** | تطبیق رابط یک کلاس به رابطی که کلاینت انتظار دارد |
| **Facade** | یک رابط ساده جلوی یک زیرسیستم پیچیده |
| **Builder** | ساخت گام‌به‌گام آبجکت پیچیده (مثل `StringBuilder`) |
| **Mediator** | ارتباط بین اجزا از طریق یک واسط، به‌جای ارتباط مستقیم |

**نکته:** لازم نیست همه را با جزئیات بلد باشی. مهم‌تر این است که برای Repository، Factory، Strategy و Singleton **مثال واقعی از تجربه خودت** داشته باشی.
