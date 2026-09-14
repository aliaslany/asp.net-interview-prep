// ================================================================
//  jQuery و AJAX — آنچه در آگهی "تسلط کامل" خواسته شده
// ================================================================

// ----------------------------------------------------------------
// ۱. آماده شدن DOM
// ----------------------------------------------------------------

$(document).ready(function () {
    // کد اینجا بعد از ساخته شدن DOM اجرا می‌شود
    // (نه لزوماً بعد از لود شدن تصاویر)
});

// شکل کوتاه و رایج‌تر
$(function () {
    // همان بالا
});

// تفاوت با window.onload:
//   ready → بعد از آماده شدن ساختار HTML
//   load  → بعد از لود شدن همه منابع (تصویر، CSS، iframe)
//   ready زودتر اجرا می‌شود، پس معمولاً همان را می‌خواهیم


// ----------------------------------------------------------------
// ۲. Event Delegation — مهم‌ترین مفهوم jQuery در مصاحبه
// ----------------------------------------------------------------

// ❌ فقط روی المان‌هایی کار می‌کند که همین الان در DOM هستند
$('.delete-btn').on('click', function () {
    console.log('حذف');
});
// اگر بعداً با AJAX دکمه جدیدی به صفحه اضافه شود،
// این رویداد روی آن کار نمی‌کند

// ✅ Event Delegation — روی والد ثابت گوش می‌دهیم
$('#product-table').on('click', '.delete-btn', function () {
    var id = $(this).data('id');
    console.log('حذف محصول ' + id);
});
// روی دکمه‌هایی که در آینده اضافه شوند هم کار می‌کند

// چرا کار می‌کند؟
//   رویداد کلیک از دکمه به بالا "حباب" می‌کند تا به #product-table برسد.
//   jQuery آنجا چک می‌کند که آیا منبع رویداد با سلکتور '.delete-btn' جور است.
//   چون به والد گوش می‌دهیم، مهم نیست فرزند کِی ساخته شده.

// نکته: به المان ثابت‌ترین والد ممکن گوش بده، نه document
// چون هرچه مسیر حباب کوتاه‌تر باشد، سریع‌تر است


// ----------------------------------------------------------------
// ۳. انتخابگرها و پیمایش DOM
// ----------------------------------------------------------------

$('#userId');                 // بر اساس id — سریع‌ترین
$('.product-card');           // بر اساس class
$('input[type="text"]');      // بر اساس صفت
$('#form input:visible');     // ترکیبی

// کش کردن نتیجه انتخاب — نکته کارایی مهم
var $rows = $('#table tr');   // یک بار جستجو
$rows.addClass('a');
$rows.addClass('b');
// به‌جای اینکه هر بار $('#table tr') بنویسی و DOM را دوباره بگردی

// قرارداد نام‌گذاری: متغیرهای jQuery را با $ شروع کن
// تا از المان DOM خام قابل تشخیص باشند


// ----------------------------------------------------------------
// ۴. AJAX — فراخوانی Web API
// ----------------------------------------------------------------

// GET
$.ajax({
    url: '/api/products',
    type: 'GET',
    data: { page: 1, pageSize: 20 },
    dataType: 'json',

    success: function (response) {
        renderProducts(response.items);
    },

    error: function (xhr, status, error) {
        if (xhr.status === 401) {
            window.location.href = '/Account/Login';
        } else if (xhr.status === 400) {
            showValidationErrors(xhr.responseJSON);
        } else {
            showMessage('خطایی رخ داد. لطفاً دوباره تلاش کنید.');
        }
    }
});


// POST با JSON
function createProduct(product) {
    return $.ajax({
        url: '/api/products',
        type: 'POST',
        contentType: 'application/json',      // چه چیزی می‌فرستیم
        data: JSON.stringify(product),        // باید رشته باشد
        dataType: 'json'                      // چه چیزی انتظار داریم
    });
}

// استفاده با Promise — تمیزتر از callback تودرتو
createProduct({ name: 'کیبورد', price: 850000 })
    .done(function (result) {
        console.log('ساخته شد با شناسه ' + result.id);
    })
    .fail(function (xhr) {
        console.error('خطا: ' + xhr.status);
    })
    .always(function () {
        $('#loading').hide();
    });


// ارسال توکن ضدجعل (CSRF) در فراخوانی‌های MVC
function postWithToken(url, data) {
    var token = $('input[name="__RequestVerificationToken"]').val();

    return $.ajax({
        url: url,
        type: 'POST',
        data: data,
        headers: { 'RequestVerificationToken': token }
    });
}


// تنظیمات سراسری برای همه فراخوانی‌ها
$.ajaxSetup({
    beforeSend: function () { $('#loading').show(); },
    complete:   function () { $('#loading').hide(); }
});


// ----------------------------------------------------------------
// ۵. دستکاری DOM
// ----------------------------------------------------------------

function renderProducts(products) {
    var $tbody = $('#product-table tbody');
    $tbody.empty();

    // ❌ کند: در هر تکرار به DOM دست می‌زنیم
    // products.forEach(function (p) {
    //     $tbody.append('<tr>...</tr>');
    // });

    // ✅ سریع: همه را در حافظه بساز، یک بار به DOM اضافه کن
    var html = '';
    products.forEach(function (p) {
        html += '<tr data-id="' + p.id + '">'
              + '<td>' + escapeHtml(p.name) + '</td>'
              + '<td>' + p.price.toLocaleString('fa-IR') + '</td>'
              + '<td><button class="delete-btn" data-id="' + p.id + '">حذف</button></td>'
              + '</tr>';
    });

    $tbody.html(html);      // یک بار reflow به‌جای N بار
}

// جلوگیری از XSS — هرگز داده کاربر را خام در HTML نگذار
function escapeHtml(text) {
    return $('<div>').text(text).html();
}


// ----------------------------------------------------------------
// ۶. کار با فرم
// ----------------------------------------------------------------

$('#product-form').on('submit', function (e) {
    e.preventDefault();          // جلوی ارسال معمولی فرم را بگیر

    var $form = $(this);

    if (!$form.valid()) return;  // اگر jquery.validate استفاده می‌کنی

    $.ajax({
        url: $form.attr('action'),
        type: $form.attr('method'),
        data: $form.serialize()   // همه فیلدها را به رشته query تبدیل می‌کند
    }).done(function () {
        showMessage('با موفقیت ذخیره شد');
    });
});


// ----------------------------------------------------------------
// ۷. Debounce — جلوگیری از فراخوانی بیش از حد
// ----------------------------------------------------------------

function debounce(fn, delay) {
    var timer = null;
    return function () {
        var context = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(function () {
            fn.apply(context, args);
        }, delay);
    };
}

// بدون debounce: با تایپ "لپ‌تاپ" شش درخواست فرستاده می‌شود
// با debounce: فقط بعد از اینکه کاربر ۳۰۰ میلی‌ثانیه دست کشید، یکی
$('#search').on('keyup', debounce(function () {
    var term = $(this).val();
    if (term.length < 2) return;

    $.get('/api/products/search', { q: term })
     .done(renderProducts);
}, 300));


// ----------------------------------------------------------------
// ۸. جاوااسکریپت پایه که پرسیده می‌شود
// ----------------------------------------------------------------

// == در برابر ===
console.log(1 == '1');     // true  — نوع را تبدیل می‌کند
console.log(1 === '1');    // false — نوع را هم مقایسه می‌کند
// همیشه === استفاده کن

// var / let / const
function scopeDemo() {
    if (true) {
        var a = 1;         // function scope — بیرون هم قابل دسترس
        let b = 2;         // block scope — فقط داخل if
        const c = 3;       // block scope و غیرقابل انتساب مجدد
    }
    console.log(a);        // 1
    // console.log(b);     // خطا
}

// Closure — تابعی که به متغیرهای محیط تعریفش دسترسی دارد
function makeCounter() {
    var count = 0;                    // در closure زنده می‌ماند
    return function () {
        return ++count;
    };
}
var counter = makeCounter();
counter();  // 1
counter();  // 2

// تله کلاسیک closure در حلقه
for (var i = 0; i < 3; i++) {
    setTimeout(function () { console.log(i); }, 100);
}
// خروجی: 3, 3, 3  ← چون var تابع‌محدوده است و حلقه تمام شده

for (let j = 0; j < 3; j++) {
    setTimeout(function () { console.log(j); }, 100);
}
// خروجی: 0, 1, 2  ← let برای هر تکرار یک binding جدید می‌سازد


function showMessage(msg) { alert(msg); }
function showValidationErrors(errors) { console.log(errors); }
