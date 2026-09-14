# jQuery و AJAX

آگهی این شرکت «تسلط کامل» به jQuery خواسته و صراحتاً «توانایی Debug و کدنویسی با JavaScript و jQuery» را ذکر کرده — یعنی احتمالاً سوال عملی می‌پرسند.

---

## ۱. `$(document).ready`

```javascript
$(function () { ... });    // شکل کوتاه و رایج
```

| | کِی اجرا می‌شود |
|---|---|
| `ready` | بعد از آماده شدن ساختار DOM |
| `window.onload` | بعد از لود شدن **همه** منابع (تصویر، CSS، iframe) |

`ready` زودتر اجرا می‌شود، پس تجربه کاربری بهتری می‌دهد. معمولاً همین را می‌خواهی.

---

## ۲. Event Delegation — مهم‌ترین مفهوم

این پرتکرارترین سوال jQuery در مصاحبه است.

### مشکل

```javascript
$('.delete-btn').on('click', function () { ... });
```

این فقط روی دکمه‌هایی کار می‌کند که **در همان لحظه** در DOM هستند. اگر بعداً با AJAX سطر جدیدی به جدول اضافه شود، دکمه‌اش کار نمی‌کند.

### راه‌حل

```javascript
$('#product-table').on('click', '.delete-btn', function () {
    var id = $(this).data('id');
});
```

### چرا کار می‌کند؟

به‌خاطر **Event Bubbling**. وقتی روی دکمه کلیک می‌شود، رویداد از دکمه به والدینش «حباب» می‌کند تا به بالا برسد.

ما به والد **ثابت** (`#product-table`) گوش می‌دهیم. jQuery در آنجا چک می‌کند که آیا منبع رویداد با سلکتور `.delete-btn` جور است یا نه.

چون به والد گوش می‌دهیم، **مهم نیست فرزند کِی ساخته شده**.

### نکات

- به **نزدیک‌ترین والد ثابت** گوش بده، نه `document` — هرچه مسیر حباب کوتاه‌تر، سریع‌تر
- یک فایده جانبی: به‌جای ۱۰۰۰ event listener برای ۱۰۰۰ سطر، فقط یکی داری

---

## ۳. کارایی در کار با DOM

### کش کردن انتخابگر

```javascript
// ❌ هر بار DOM را می‌گردد
$('#table tr').addClass('a');
$('#table tr').addClass('b');

// ✅ یک بار جستجو
var $rows = $('#table tr');
$rows.addClass('a');
$rows.addClass('b');
```

**قرارداد:** متغیرهای jQuery را با `$` شروع کن (`$rows`) تا از المان DOM خام قابل تشخیص باشند.

### ساخت HTML در حافظه

```javascript
// ❌ N بار reflow مرورگر
products.forEach(p => $tbody.append('<tr>...</tr>'));

// ✅ یک بار
var html = '';
products.forEach(p => html += '<tr>...</tr>');
$tbody.html(html);
```

هر بار که به DOM دست می‌زنی، مرورگر باید layout را دوباره محاسبه کند (reflow). با ۱۰۰۰ سطر این تفاوت کاملاً محسوس است.

---

## ۴. AJAX

```javascript
$.ajax({
    url: '/api/products',
    type: 'POST',
    contentType: 'application/json',   // چه می‌فرستیم
    data: JSON.stringify(product),     // باید رشته باشد
    dataType: 'json',                  // چه انتظار داریم
    success: function (res) { },
    error: function (xhr) { }
});
```

### نکته مهم: `contentType` و `dataType`

- `contentType` = فرمت چیزی که **می‌فرستی**
- `dataType` = فرمت چیزی که **انتظار داری بگیری**

اشتباه گرفتن این دو، یکی از رایج‌ترین دلایل کار نکردن فراخوانی AJAX است.

اگر `contentType: 'application/json'` بگذاری، **باید** داده را `JSON.stringify` کنی. اگر نگذاری، jQuery داده را به شکل form-urlencoded می‌فرستد.

### مدیریت خطا بر اساس status code

```javascript
error: function (xhr) {
    if (xhr.status === 401) window.location.href = '/Account/Login';
    else if (xhr.status === 400) showValidationErrors(xhr.responseJSON);
    else showMessage('خطایی رخ داد');
}
```

این ارتباط مستقیم دارد با status code هایی که سمت Web API برمی‌گردانی.

### ارسال توکن CSRF

```javascript
var token = $('input[name="__RequestVerificationToken"]').val();
$.ajax({ headers: { 'RequestVerificationToken': token } });
```

اگر اکشن سمت سرور `[ValidateAntiForgeryToken]` دارد، بدون این هدر درخواست AJAX رد می‌شود.

---

## ۵. کار با فرم

```javascript
$('#product-form').on('submit', function (e) {
    e.preventDefault();                    // جلوی ارسال معمولی
    $.ajax({
        url: $(this).attr('action'),
        data: $(this).serialize()          // همه فیلدها → query string
    });
});
```

`serialize()` همه فیلدهای فرم را به رشته `name=value&name2=value2` تبدیل می‌کند — دقیقاً فرمتی که Model Binding در ASP.NET می‌فهمد.

---

## ۶. Debounce

```javascript
$('#search').on('keyup', debounce(function () {
    $.get('/api/search', { q: $(this).val() }).done(render);
}, 300));
```

**بدون debounce:** با تایپ «لپ‌تاپ» شش درخواست فرستاده می‌شود.
**با debounce:** فقط بعد از اینکه کاربر ۳۰۰ میلی‌ثانیه دست کشید، یکی.

### Debounce در برابر Throttle

| | رفتار | کاربرد |
|---|---|---|
| **Debounce** | صبر می‌کند تا کاربر دست بکشد | جستجوی زنده، اعتبارسنجی |
| **Throttle** | حداکثر هر X میلی‌ثانیه یک بار | scroll، resize، حرکت ماوس |

---

## ۷. XSS — نکته امنیتی

```javascript
// ❌ خطرناک
$tbody.html('<td>' + product.name + '</td>');

// ✅ امن
function escapeHtml(text) {
    return $('<div>').text(text).html();
}
```

اگر نام محصول `<script>alert(1)</script>` باشد، در حالت اول اسکریپت اجرا می‌شود.

**یا ساده‌تر:** از `.text()` به‌جای `.html()` استفاده کن — jQuery خودش encode می‌کند.

---

## ۸. جاوااسکریپت پایه که پرسیده می‌شود

### `==` در برابر `===`

```javascript
1 == '1'     // true  — نوع را تبدیل می‌کند
1 === '1'    // false — نوع را هم مقایسه می‌کند
```

**همیشه `===` استفاده کن.** تبدیل خودکار نوع در JS منبع باگ‌های عجیب است.

### `var` / `let` / `const`

| | محدوده | انتساب مجدد |
|---|---|---|
| `var` | تابع | ✓ |
| `let` | بلاک | ✓ |
| `const` | بلاک | ✗ |

**`const` روی آبجکت:** خود متغیر قابل انتساب مجدد نیست، ولی محتوای آبجکت قابل تغییر است.

### Closure

تابعی که به متغیرهای محیط تعریفش دسترسی دارد — حتی بعد از اینکه آن تابع بیرونی تمام شده.

```javascript
function makeCounter() {
    var count = 0;                // در closure زنده می‌ماند
    return function () { return ++count; };
}
```

### تله کلاسیک closure در حلقه

```javascript
for (var i = 0; i < 3; i++)
    setTimeout(() => console.log(i), 100);
// خروجی: 3, 3, 3

for (let j = 0; j < 3; j++)
    setTimeout(() => console.log(j), 100);
// خروجی: 0, 1, 2
```

**چرا؟** `var` محدوده تابعی دارد، پس همه callback ها به **یک** متغیر اشاره می‌کنند که تا زمان اجرای آن‌ها مقدارش ۳ شده. `let` برای **هر تکرار** یک binding جدید می‌سازد.

این سوال را خیلی می‌پرسند.

---

## Bootstrap

آگهی Bootstrap را هم ذکر کرده. چیزهایی که ممکن است بپرسند:

**سیستم گرید ۱۲ ستونی:**
```html
<div class="container">
  <div class="row">
    <div class="col-md-8">محتوا</div>
    <div class="col-md-4">کناره</div>
  </div>
</div>
```

**نقاط شکست:** `col-` (موبایل)، `col-sm-`، `col-md-`، `col-lg-`، `col-xl-`

**رویکرد Mobile-first:** کلاس بدون پیشوند برای کوچک‌ترین صفحه است و بقیه از آن اندازه به **بالا** اعمال می‌شوند.

**پشتیبانی RTL:** بوت‌استرپ ۵ به‌صورت بومی RTL دارد (`dir="rtl"` و فایل `bootstrap.rtl.css`). در نسخه‌های قدیمی‌تر باید از نسخه‌های شخص ثالث استفاده می‌شد.
