# آماده‌سازی مصاحبه Full-Stack .NET

مخزنی از مثال‌های کد + توضیح فارسی، بر اساس دقیقاً همان چیزهایی که در آگهی شغلی خواسته شده.

**ساختار:** هر موضوع دو فایل دارد — یک فایل کد با کامنت، و یک فایل `.md` هم‌نام که مفهوم را توضیح می‌دهد و می‌گوید سر مصاحبه چه بگویی.

---

## فهرست

### ۱. C#
| فایل | موضوع |
|---|---|
| `01-IEnumerable-vs-IQueryable` | تفاوت، درخت عبارت، اجرای تأخیری |
| `02-Value-vs-Reference-Type` | stack و heap، boxing، nullable، برابری |
| `03-Abstract-vs-Interface` | کِی کدام، virtual/override، تله `new` |
| `04-Async-Await` | deadlock با `.Result`، `Task.WhenAll`، I/O در برابر CPU |
| `05-String-Disposable-Linq` | StringBuilder، الگوی Dispose، عملیات LINQ |

### ۲. OOP و SOLID
| فایل | موضوع |
|---|---|
| `01-OOP-Four-Pillars` | کپسوله‌سازی، وراثت، چندریختی، انتزاع |
| `02-SOLID` | هر پنج اصل با مثال نقض و اصلاح |

### ۳. Design Patterns
| فایل | موضوع |
|---|---|
| `01-Repository-UnitOfWork` | پیاده‌سازی کامل + بحث «آیا EF خودش این نیست؟» |
| `02-Singleton-Factory-Strategy` | سه الگوی پرتکرار + تفاوت Factory و Strategy |

### ۴. Entity Framework
| فایل | موضوع |
|---|---|
| `01-NPlusOne-And-Loading` | مشکل N+1، Include، Projection، AsNoTracking |
| `02-Transactions-Concurrency` | تراکنش، RowVersion، Lost Update، درج انبوه |

### ۵. SQL Server
| فایل | موضوع |
|---|---|
| `01-Indexes-And-Performance` | clustered/non-clustered، Key Lookup، SARGability |
| `02-Joins-Transactions-TSQL` | انواع JOIN، window function، CTE، Stored Procedure |

### ۶. ASP.NET و Web API
| فایل | موضوع |
|---|---|
| `01-MVC-And-RestApi` | چرخه عمر، Model Binding، فیلترها، CSRF، status code |

### ۷. Frontend
| فایل | موضوع |
|---|---|
| `01-jQuery-And-Ajax` | Event Delegation، AJAX، debounce، closure |

### ۸. Concurrency
| فایل | موضوع |
|---|---|
| `01-Multithreading-SignalR` | Thread/Task، race condition، lock، SignalR |

---

## اگر وقت کم داری

به ترتیب احتمال پرسیده شدن:

1. **`04-entity-framework/01`** — مشکل N+1
2. **`01-csharp/01`** — IEnumerable در برابر IQueryable
3. **`05-sql-server/01`** — Index و Key Lookup
4. **`02-oop-solid/02`** — SOLID
5. **`01-csharp/04`** — deadlock در async
6. **`07-frontend/01`** — Event Delegation

---

## الگوی جواب دادن

برای هر سوال فنی، این سه بخش را رعایت کن:

1. **تعریف کوتاه** — چیست
2. **چرا / کجا** — چه مشکلی را حل می‌کند
3. **Trade-off** — چه هزینه‌ای دارد

مثال:
> «Index کوئری خواندن را سریع می‌کند چون به‌جای اسکن کل جدول مستقیم سراغ داده می‌رود، **ولی** نوشتن را کند می‌کند و فضا می‌گیرد، پس فقط روی ستون‌های پرکاربرد در WHERE می‌گذارمش.»

بخش سوم همان چیزی است که مهندس باتجربه را از تازه‌کار جدا می‌کند.

---

## اگر جواب را بلد نیستی

**نگو:** «نمی‌دانم» و ساکت شو
**نگو:** جواب اشتباه با اعتماد به نفس

**بگو:**
> «مدتی است مستقیم با این کار نکرده‌ام، ولی منطقش این است که... درست فکر می‌کنم؟»

صداقت + منطق + آمادگی یادگیری — دقیقاً همان چیزی که کارفرما دنبالش است.
