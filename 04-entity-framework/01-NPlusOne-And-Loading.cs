using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace InterviewPrep.EntityFramework
{
    // ================================================================
    //  مشکل N+1 و انواع بارگذاری در EF
    //  رایج‌ترین مشکل عملکردی در پروژه‌های EF
    // ================================================================

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        //     ^^^^^^^ virtual یعنی Lazy Loading فعال است
    }

    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
        public virtual Customer Customer { get; set; }
    }

    public class ShopContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
    }

    public class LoadingStrategies
    {
        private readonly ShopContext _db = new ShopContext();

        // ------------------------------------------------------------
        // مشکل N+1
        // ------------------------------------------------------------
        public void TheNPlusOneProblem()
        {
            var orders = _db.Orders.Take(1000).ToList();
            // SQL شماره ۱:
            //   SELECT TOP 1000 * FROM Orders

            foreach (var order in orders)
            {
                // هر بار که به order.Customer دست می‌زنیم،
                // EF یک کوئری جداگانه می‌زند
                Console.WriteLine(order.Customer.Name);

                // SQL شماره ۲ تا ۱۰۰۱:
                //   SELECT * FROM Customers WHERE Id = @p0
            }

            // مجموع: ۱۰۰۱ رفت‌وبرگشت به دیتابیس
            // هر رفت‌وبرگشت حتی اگر ۱ میلی‌ثانیه باشد → ۱ ثانیه فقط تأخیر شبکه
        }

        // ------------------------------------------------------------
        // راه‌حل ۱: Eager Loading با Include
        // ------------------------------------------------------------
        public void EagerLoading()
        {
            var orders = _db.Orders
                .Include(o => o.Customer)     // ← مشتری را همزمان بیاور
                .Take(1000)
                .ToList();

            // فقط یک SQL با JOIN:
            //   SELECT o.*, c.*
            //   FROM Orders o
            //   INNER JOIN Customers c ON o.CustomerId = c.Id

            foreach (var order in orders)
                Console.WriteLine(order.Customer.Name);   // بدون کوئری اضافه
        }

        // Include تودرتو
        public void NestedInclude()
        {
            var customers = _db.Customers
                .Include(c => c.Orders)
                .ToList();

            // هشدار: Include های زیاد و تودرتو، JOIN های بزرگ می‌سازند
            // و داده تکراری زیادی برمی‌گردانند (Cartesian Explosion)
            // مثلاً اگر مشتری ۱۰ سفارش داشته باشد، اطلاعات مشتری
            // ۱۰ بار در نتیجه تکرار می‌شود
        }

        // ------------------------------------------------------------
        // راه‌حل ۲: Explicit Loading — بارگذاری دستی و کنترل‌شده
        // ------------------------------------------------------------
        public void ExplicitLoading()
        {
            var customer = _db.Customers.First();

            // فقط وقتی واقعاً لازم شد، بارگذاری کن
            _db.Entry(customer)
               .Collection(c => c.Orders)
               .Load();

            // برای رابطه یک‌به‌یک یا چند‌به‌یک:
            var order = _db.Orders.First();
            _db.Entry(order).Reference(o => o.Customer).Load();
        }

        // ------------------------------------------------------------
        // راه‌حل ۳: Projection — بهترین گزینه برای نمایش
        // ------------------------------------------------------------
        public void Projection()
        {
            var data = _db.Orders
                .Select(o => new OrderListItem
                {
                    OrderId      = o.Id,
                    Total        = o.Total,
                    CustomerName = o.Customer.Name    // EF خودش JOIN می‌زند
                })
                .Take(1000)
                .ToList();

            // SQL:
            //   SELECT TOP 1000 o.Id, o.Total, c.Name
            //   FROM Orders o INNER JOIN Customers c ON ...

            // چرا بهترین است؟
            //   - فقط ۳ ستون از دیتابیس می‌آید، نه کل موجودیت
            //   - Change Tracker درگیر نمی‌شود (خروجی موجودیت EF نیست)
            //   - ترافیک شبکه و حافظه کمتر
        }

        // ------------------------------------------------------------
        // AsNoTracking — برای کوئری‌های فقط‌خواندنی
        // ------------------------------------------------------------
        public List<Order> ReadOnlyQuery()
        {
            return _db.Orders
                .AsNoTracking()          // ← Change Tracker غیرفعال
                .Where(o => o.Total > 100_000)
                .ToList();

            // Change Tracker چه می‌کند؟
            //   از هر موجودیت یک کپی اولیه نگه می‌دارد تا موقع
            //   SaveChanges بفهمد چه چیزی عوض شده.
            //
            // اگر قرار نیست چیزی را آپدیت کنی، این کار
            //   - حافظه می‌خورد
            //   - CPU می‌خورد (مقایسه اسنپ‌شات)
            //   - و هیچ فایده‌ای ندارد
            //
            // در صفحات گزارش و لیست، همیشه AsNoTracking بگذار
        }

        // ------------------------------------------------------------
        // غیرفعال کردن کامل Lazy Loading
        // ------------------------------------------------------------
        public void DisableLazyLoading()
        {
            _db.Configuration.LazyLoadingEnabled = false;

            // حالا اگر یادت برود Include بزنی، Customer برابر null می‌شود
            // و خطا می‌گیری — که بهتر از این است که بی‌صدا
            // ۱۰۰۰ کوئری اضافه بزند و متوجه نشوی
        }

        // ------------------------------------------------------------
        // تله رایج: Lazy Loading بعد از بسته شدن Context
        // ------------------------------------------------------------
        public List<Order> DisposedContextTrap()
        {
            List<Order> orders;

            using (var db = new ShopContext())
            {
                orders = db.Orders.ToList();
            }   // ← context اینجا بسته شد

            // این خط ObjectDisposedException می‌دهد
            // چون Lazy Loading می‌خواهد کوئری بزند ولی context مرده است
            // Console.WriteLine(orders[0].Customer.Name);

            return orders;

            // راه‌حل: قبل از بسته شدن، Include یا Projection بزن
        }
    }

    public class OrderListItem
    {
        public int OrderId { get; set; }
        public decimal Total { get; set; }
        public string CustomerName { get; set; }
    }
}
