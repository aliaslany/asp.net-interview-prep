using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace InterviewPrep.CSharp
{
    // ================================================================
    //  string / StringBuilder ، IDisposable ، LINQ
    //  سه موضوع کوتاه ولی پرتکرار
    // ================================================================

    public class StringPerformance
    {
        // ------------------------------------------------------------
        // string در C# غیرقابل تغییر (immutable) است
        // ------------------------------------------------------------
        public void WhyImmutableMatters()
        {
            string s = "Hello";
            s += " World";
            // اینجا "Hello" عوض نشد!
            // یک رشته جدید "Hello World" ساخته شد و s به آن اشاره می‌کند
            // رشته قدیمی زباله می‌شود و GC باید جمعش کند
        }

        // ❌ در حلقه فاجعه است
        public string BadConcat(List<string> items)
        {
            string result = "";
            foreach (var item in items)
                result += item + ", ";   // هر بار یک آبجکت جدید

            // با ۱۰ هزار آیتم → ۱۰ هزار رشته موقت ساخته می‌شود
            return result;
        }

        // ✅ StringBuilder یک بافر قابل تغییر دارد
        public string GoodConcat(List<string> items)
        {
            var sb = new StringBuilder();
            foreach (var item in items)
                sb.Append(item).Append(", ");

            return sb.ToString();   // فقط یک بار رشته ساخته می‌شود
        }

        // برای تعداد کم و ثابت، خود کامپایلر بهینه می‌کند
        public string FewItems(string a, string b, string c)
        {
            return a + b + c;       // کامپایلر به string.Concat تبدیل می‌کند — مشکلی ندارد
        }

        // مقایسه رشته‌ها — نکته امنیتی و فرهنگی
        public void StringComparison()
        {
            string a = "İstanbul";
            string b = "istanbul";

            // ❌ در زبان ترکی نتیجه غیرمنتظره می‌دهد
            bool wrong = a.ToLower() == b;

            // ✅ برای مقایسه فنی (نام فایل، کلید، توکن)
            bool right = string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ================================================================
    //  IDisposable و using
    // ================================================================

    public class ResourceHolder : IDisposable
    {
        private SqlConnection _connection;
        private bool _disposed;

        public ResourceHolder(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
            _connection.Open();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);   // به GC بگو نیازی به finalizer نیست
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // آزادسازی منابع مدیریت‌شده
                _connection?.Dispose();
                _connection = null;
            }

            // اینجا منابع غیرمدیریت‌شده (handle فایل، pointer) آزاد می‌شوند

            _disposed = true;
        }
    }

    public class DisposableUsage
    {
        // ❌ اگر خطا رخ دهد، اتصال بسته نمی‌شود
        public void Bad(string cs)
        {
            var conn = new SqlConnection(cs);
            conn.Open();
            DoWork(conn);        // اگر اینجا exception بدهد...
            conn.Close();        // ...این خط هرگز اجرا نمی‌شود
        }

        // ✅ using تضمین می‌کند Dispose صدا زده شود، حتی با خطا
        public void Good(string cs)
        {
            using (var conn = new SqlConnection(cs))
            {
                conn.Open();
                DoWork(conn);
            }   // Dispose اینجا خودکار صدا زده می‌شود

            // using معادل این است:
            //   try { ... } finally { conn?.Dispose(); }
        }

        private void DoWork(SqlConnection c) { }
    }

    // ================================================================
    //  LINQ
    // ================================================================

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }

    public class LinqExamples
    {
        private readonly List<Product> _products = new List<Product>();

        public void CommonOperations()
        {
            // فیلتر
            var cheap = _products.Where(p => p.Price < 100_000);

            // تبدیل (projection)
            var names = _products.Select(p => p.Name);

            // مرتب‌سازی چندسطحی
            var sorted = _products
                .OrderBy(p => p.Category)
                .ThenByDescending(p => p.Price);

            // گروه‌بندی
            var byCategory = _products.GroupBy(p => p.Category);
            foreach (var group in byCategory)
                Console.WriteLine($"{group.Key}: {group.Count()} کالا");

            // توابع تجمیعی
            decimal total = _products.Sum(p => p.Price);
            decimal avg = _products.Average(p => p.Price);
            var mostExpensive = _products.Max(p => p.Price);

            // صفحه‌بندی
            var page2 = _products.Skip(20).Take(20);

            // بررسی وجود
            bool anyOutOfStock = _products.Any(p => p.Stock == 0);
            bool allInStock = _products.All(p => p.Stock > 0);
        }

        // ------------------------------------------------------------
        // First / FirstOrDefault / Single / SingleOrDefault
        // تفاوتشان سوال پرتکراری است
        // ------------------------------------------------------------
        public void FindingElements()
        {
            // First: اولی را بده — اگر هیچی نبود خطا
            var a = _products.First(p => p.Id == 5);

            // FirstOrDefault: اولی را بده — اگر نبود null
            var b = _products.FirstOrDefault(p => p.Id == 5);

            // Single: دقیقاً یکی باید باشد — اگر صفر یا بیشتر از یکی بود خطا
            var c = _products.Single(p => p.Id == 5);

            // SingleOrDefault: صفر یا یکی — اگر بیشتر بود خطا
            var d = _products.SingleOrDefault(p => p.Id == 5);

            // کِی Single؟ وقتی منطق برنامه‌ات می‌گوید حتماً باید یکتا باشد
            // و اگر نبود، یعنی یک باگ جدی داری و بهتر است زود بفهمی
        }

        // ------------------------------------------------------------
        // Deferred Execution — تله رایج
        // ------------------------------------------------------------
        public void DeferredExecutionTrap()
        {
            var list = new List<int> { 1, 2, 3 };

            var query = list.Where(x => x > 1);   // هنوز اجرا نشده

            list.Add(4);                           // لیست را عوض کردیم

            Console.WriteLine(query.Count());      // 3 ← عدد ۴ هم شمرده شد!

            // اگر می‌خواهی همان لحظه اجرا شود، ToList بزن
            var snapshot = list.Where(x => x > 1).ToList();
        }

        // ------------------------------------------------------------
        // join در LINQ
        // ------------------------------------------------------------
        public void JoinExample(List<Product> products, List<Order> orders)
        {
            var result = from p in products
                         join o in orders on p.Id equals o.ProductId
                         select new { p.Name, o.Quantity };

            // معادل با syntax متدی
            var same = products.Join(
                orders,
                p => p.Id,
                o => o.ProductId,
                (p, o) => new { p.Name, o.Quantity });
        }
    }

    public class Order
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
