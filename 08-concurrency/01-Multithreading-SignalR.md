# Multithreading و SignalR

هر دو در آگهی به‌عنوان **مزیت** ذکر شده‌اند — یعنی احتمالاً سوال عمیقی نمی‌پرسند، ولی اگر بتوانی درست جواب بدهی امتیاز اضافه می‌گیری.

---

# Multithreading

## Thread در برابر Task

| | Thread | Task |
|---|---|---|
| چیست | رشته واقعی سیستم‌عامل | انتزاع روی Thread Pool |
| حافظه | حدود ۱ مگابایت stack | بسیار کمتر |
| ساخت | پرهزینه | رشته بازاستفاده می‌شود |
| نتیجه | برنمی‌گرداند | `Task<T>` برمی‌گرداند |
| ترکیب | سخت | `await`، `ContinueWith`، `WhenAll` |

**جواب کوتاه:** در کد مدرن .NET تقریباً همیشه `Task` استفاده می‌کنی، نه `Thread`.

---

## Race Condition

```csharp
Parallel.For(0, 100_000, i => _counter++);
Console.WriteLine(_counter);    // معمولاً کمتر از ۱۰۰۰۰۰!
```

**چرا؟** چون `++` در واقع **سه عمل** است:
1. خواندن مقدار فعلی
2. اضافه کردن یک
3. نوشتن مقدار جدید

اگر دو رشته همزمان مرحله ۱ را انجام دهند، هر دو مقدار یکسان می‌خوانند و یکی از افزایش‌ها **گم می‌شود**.

این همان **Lost Update** است که در بحث همزمانی EF هم دیدیم — فقط در سطح حافظه به‌جای دیتابیس.

---

## سه راه‌حل

### ۱. `lock`

```csharp
private readonly object _lockObject = new object();

lock (_lockObject)
{
    _counter++;
}
```

**نکات مهم `lock`:**

- هرگز روی `this`، `typeof(X)`، یا یک **رشته** قفل نزن. چون کد بیرونی هم می‌تواند روی همان آبجکت قفل بزند و deadlock بسازد.
- همیشه یک `private readonly object` اختصاصی بساز.
- بخش قفل‌شده را تا حد ممکن **کوتاه** نگه دار — هرچه طولانی‌تر، رقابت بیشتر و کندی بیشتر.
- داخل `lock` نمی‌توانی `await` بزنی (کامپایلر اجازه نمی‌دهد). برای async از `SemaphoreSlim` استفاده کن.

### ۲. `Interlocked` — سریع‌تر برای عملیات ساده

```csharp
Interlocked.Increment(ref _counter);
```

عملیات اتمیک در سطح دستورات CPU. **خیلی سریع‌تر از lock** چون هیچ رشته‌ای بلاک نمی‌شود.

متدهای موجود: `Increment`, `Decrement`, `Add`, `Exchange`, `CompareExchange`.

**کِی؟** برای شمارنده‌ها و تعویض مقادیر ساده.

### ۳. مجموعه‌های Concurrent

```csharp
var dict = new ConcurrentDictionary<string, int>();
dict.AddOrUpdate("a", 1, (key, old) => old + 1);
```

**هشدار:** `Dictionary` معمولی thread-safe **نیست**. دسترسی همزمان می‌تواند ساختار داخلی‌اش را خراب کند و حتی حلقه بی‌نهایت بسازد.

موجود: `ConcurrentDictionary`, `ConcurrentQueue`, `ConcurrentStack`, `ConcurrentBag`, `BlockingCollection`.

---

## Deadlock

```csharp
// رشته ۱: A را می‌گیرد، منتظر B
lock (_lockA) { lock (_lockB) { } }

// رشته ۲: B را می‌گیرد، منتظر A
lock (_lockB) { lock (_lockA) { } }
```

هر دو برای همیشه منتظر می‌مانند.

**راه‌حل اصلی:** همیشه قفل‌ها را به **یک ترتیب ثابت** بگیر. اگر همه‌جا اول A بعد B بگیری، این حالت هرگز پیش نمی‌آید.

**راه‌حل‌های دیگر:** استفاده از timeout با `Monitor.TryEnter`، یا کلاً کاهش تعداد قفل‌ها.

---

## `Parallel` در برابر `async`

| | `Parallel` / PLINQ | `async` / `await` |
|---|---|---|
| برای | کار CPU-سنگین | کار I/O |
| چه می‌کند | چند رشته را همزمان مشغول می‌کند | رشته را در زمان انتظار آزاد می‌کند |
| مثال | پردازش تصویر، محاسبات | دیتابیس، HTTP، فایل |

**هشدار مهم:** `Parallel.ForEach` در **وب‌سرور** معمولاً اشتباه است. چون سرور همین الان هم درخواست‌های مختلف را موازی پردازش می‌کند. گرفتن رشته‌های بیشتر برای یک درخواست، ظرفیت کلی سرور را کم می‌کند.

این نکته را اگر بگویی، نشان می‌دهد فرق محیط دسکتاپ و وب را می‌فهمی.

---

## `CancellationToken`

```csharp
public async Task<string> Work(CancellationToken ct)
{
    ct.ThrowIfCancellationRequested();
    await Task.Delay(100, ct);
}

using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
{
    await Work(cts.Token);    // بعد از ۵ ثانیه خودکار لغو می‌شود
}
```

**چرا مهم است؟** اگر کاربر صفحه را ببندد یا درخواست timeout شود، ادامه دادن کار فقط منابع سرور را هدر می‌دهد.

داشتن `CancellationToken` در امضای متدهای طولانی، نشانه کد حرفه‌ای است.

---

# SignalR

## چیست؟

کتابخانه‌ای برای **ارتباط بلادرنگ دوطرفه** بین سرور و کلاینت.

**تفاوت با درخواست معمولی:** در HTTP معمولی همیشه کلاینت شروع‌کننده است. با SignalR، **سرور می‌تواند به کلاینت پیام بفرستد** بدون اینکه کلاینت چیزی خواسته باشد.

## کاربردها

- نوتیفیکیشن لحظه‌ای
- چت
- داشبورد زنده
- نمایش پیشرفت عملیات طولانی
- به‌روزرسانی همزمان برای چند کاربر

## Hub

```csharp
public class NotificationHub : Hub
{
    public void SendToAll(string message)
        => Clients.All.receiveMessage(message);

    public void SendToUser(string userId, string message)
        => Clients.User(userId).receiveMessage(message);

    public Task JoinGroup(string name)
        => Groups.Add(Context.ConnectionId, name);
}
```

`Clients.All`, `Clients.User(id)`, `Clients.Group(name)`, `Clients.Caller`, `Clients.Others`.

## فرستادن از بیرون Hub

اگر می‌خواهی از یک سرویس معمولی پیام بفرستی:

```csharp
var hub = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
hub.Clients.User(userId).receiveMessage("سفارش ارسال شد");
```

## پروتکل‌های زیرین

SignalR خودکار بهترین گزینه موجود را انتخاب می‌کند:

| روش | توضیح |
|---|---|
| **WebSocket** | بهترین — اتصال دائمی دوطرفه |
| **Server-Sent Events** | یک‌طرفه از سرور به کلاینت |
| **Long Polling** | جایگزین نهایی — درخواست باز می‌ماند تا داده بیاید |

**این انتخاب خودکار یکی از مزیت‌های اصلی SignalR است** — لازم نیست خودت با تفاوت مرورگرها و پروکسی‌ها سر و کله بزنی.

## نکته مقیاس‌پذیری

اگر چند سرور داشته باشی، کاربر متصل به سرور ۱ پیام سرور ۲ را نمی‌گیرد.

**راه‌حل: Backplane** — یک واسط مشترک (Redis، SQL Server، یا Azure Service Bus) که پیام‌ها را بین سرورها پخش می‌کند.

اگر این را بگویی، نشان می‌دهی به معماری چندسروری فکر کرده‌ای — که با بحث Load Balancer و stateless بودن هم ارتباط مستقیم دارد.
