# Value Type در برابر Reference Type

## خلاصه یک‌خطی

Value Type خودِ **مقدار** را نگه می‌دارد، Reference Type **آدرس** مقدار را.

---

## کدام کدام است؟

| Value Type | Reference Type |
|---|---|
| `int`, `long`, `double`, `decimal` | `class` |
| `bool`, `char` | `string` |
| `struct` | `array` |
| `enum` | `delegate` |
| `DateTime` | `object` |

**نکته گیج‌کننده:** `string` با اینکه شبیه value type رفتار می‌کند، **reference type** است. علتش این است که immutable است و عملگر `==` در آن سربارگذاری شده تا محتوا را مقایسه کند.

---

## حافظه: Stack و Heap

**Stack** — سریع، خودکار آزاد می‌شود، اندازه محدود. متغیرهای محلی value type اینجا هستند.

**Heap** — بزرگ، توسط Garbage Collector مدیریت می‌شود، کندتر. آبجکت‌های reference type اینجا هستند.

**نکته دقیق‌تر که امتیاز دارد:** اگر یک `struct` فیلدِ یک `class` باشد، آن struct هم **در heap** ذخیره می‌شود (داخل آن آبجکت). پس این طور نیست که «value type همیشه روی stack است» — بستگی به جایی دارد که تعریف شده.

---

## رفتار موقع کپی شدن

این مهم‌ترین تفاوت عملی است:

```csharp
var s2 = s1;      // struct → یک کپی مستقل ساخته می‌شود
s2.X = 99;        // s1 دست‌نخورده می‌ماند

var c2 = c1;      // class → فقط آدرس کپی می‌شود
c2.X = 99;        // c1 هم تغییر می‌کند، چون هر دو یک آبجکت‌اند
```

همین رفتار موقع پاس دادن به متد هم صادق است. به متد `PassingToMethods` در فایل کد نگاه کن.

---

## Boxing و Unboxing

**Boxing** = تبدیل value type به `object`.

```csharp
int number = 42;
object boxed = number;    // Boxing
int back = (int)boxed;    // Unboxing
```

**چرا هزینه دارد؟**

موقع boxing، CLR باید در heap حافظه تخصیص دهد و مقدار را کپی کند. یعنی:
- تخصیص حافظه (کند)
- فشار روی Garbage Collector
- کپی شدن داده

در یک حلقه یک‌میلیونی، این تفاوت کاملاً محسوس است.

**کجا اتفاق می‌افتد؟**

```csharp
ArrayList list = new ArrayList();
list.Add(5);                        // ← Boxing

List<int> generic = new List<int>();
generic.Add(5);                     // ← بدون Boxing
```

این دقیقاً دلیل اصلی ساخته شدن **Generics** در C# 2.0 بود: حذف boxing و اضافه کردن type safety.

**جمله‌ای که سر مصاحبه بگو:**
> «برای همین هیچ‌وقت از ArrayList و Hashtable استفاده نمی‌کنم و به‌جایشان List و Dictionary جنریک می‌گذارم — هم boxing ندارند هم type-safe هستند.»

---

## Nullable

Value type ذاتاً نمی‌تواند `null` باشد، چون مقدار است نه آدرس. `Nullable<T>` این محدودیت را برمی‌دارد:

```csharp
int? age = null;

if (age.HasValue) { ... }
int safe = age ?? 0;        // عملگر null-coalescing
```

**کاربرد واقعی:** ستون‌های nullable دیتابیس. اگر ستون `Age` در SQL Server قابل null باشد، خاصیت متناظرش در C# باید `int?` باشد.

---

## `==` در برابر `Equals`

| | چه چیزی مقایسه می‌کند |
|---|---|
| `==` روی value type | مقدار |
| `==` روی reference type | مرجع (آدرس) — مگر override شده باشد |
| `==` روی string | محتوا (چون سربارگذاری شده) |
| `Equals` روی struct | مقادیر تمام فیلدها |
| `Equals` روی class | مرجع — مگر override شده باشد |

اگر می‌خواهی دو آبجکت را بر اساس محتوا مقایسه کنی، باید `Equals` و `GetHashCode` را با هم override کنی. (اگر فقط یکی را override کنی، رفتار `Dictionary` و `HashSet` خراب می‌شود.)

---

## سوالی که ممکن است بپرسند

> «کِی از struct استفاده می‌کنی به‌جای class؟»

**جواب:** وقتی هر سه شرط برقرار باشد:
- داده کوچک است (معمولاً زیر ۱۶ بایت)
- immutable است
- عمر کوتاهی دارد و زیاد ساخته می‌شود

مثال‌های خوب: `Point`, `DateTime`, `decimal`.

اگر struct بزرگ باشد، هر بار کپی شدنش گران‌تر از کار کردن با یک آدرس است — یعنی نتیجه برعکس می‌شود.
