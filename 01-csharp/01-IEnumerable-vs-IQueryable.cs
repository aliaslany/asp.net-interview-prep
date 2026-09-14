using System;
using System.Collections.Generic;
using System.Linq;

namespace InterviewPrep.CSharp
{
    // ================================================================
    //  IEnumerable در برابر IQueryable
    //  مهم‌ترین سوال EF/LINQ در مصاحبه‌های .NET
    // ================================================================

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    public class EnumerableVsQueryable
    {
        private readonly AppDbContext _db;

        public EnumerableVsQueryable(AppDbContext db) => _db = db;

        // ------------------------------------------------------------
        // اشتباه: کل جدول از دیتابیس میاد، بعد در حافظه فیلتر می‌شود
        // ------------------------------------------------------------
        public List<User> Wrong()
        {
            IEnumerable<User> users = _db.Users;   // ← نوع را IEnumerable کردیم

            // این Where دیگر به SQL ترجمه نمی‌شود
            // چون IEnumerable یعنی «دنباله‌ای در حافظه»
            return users.Where(u => u.Age > 30).ToList();

            // SQL تولیدشده:
            //   SELECT * FROM Users
            // یعنی اگر جدول ۵ میلیون رکورد داشته باشد، همه می‌آیند در RAM
        }

        // ------------------------------------------------------------
        // درست: فیلتر به SQL ترجمه و در دیتابیس اجرا می‌شود
        // ------------------------------------------------------------
        public List<User> Right()
        {
            IQueryable<User> users = _db.Users;    // ← IQueryable

            return users.Where(u => u.Age > 30).ToList();

            // SQL تولیدشده:
            //   SELECT * FROM Users WHERE Age > 30
        }

        // ------------------------------------------------------------
        // ساختن کوئری به‌صورت تدریجی (composition)
        // تا وقتی ToList صدا زده نشود، هیچ درخواستی به دیتابیس نمی‌رود
        // ------------------------------------------------------------
        public List<User> BuildQueryStepByStep(int? minAge, bool onlyActive)
        {
            IQueryable<User> query = _db.Users;

            if (minAge.HasValue)
                query = query.Where(u => u.Age >= minAge.Value);

            if (onlyActive)
                query = query.Where(u => u.IsActive);

            query = query.OrderBy(u => u.Name);

            // فقط همین‌جا کوئری اجرا می‌شود — به آن می‌گویند Deferred Execution
            return query.ToList();
        }

        // ------------------------------------------------------------
        // تله‌ای که در کد واقعی خیلی دیده می‌شود
        // ------------------------------------------------------------
        public List<User> HiddenTrap()
        {
            // ToList() اینجا کوئری را اجرا می‌کند و همه‌چیز می‌آید در حافظه
            var all = _db.Users.ToList();

            // این Where روی List است، نه روی دیتابیس
            return all.Where(u => u.Age > 30).ToList();

            // نتیجه درست است ولی از نظر کارایی فاجعه است
        }

        // ------------------------------------------------------------
        // نکته: بعضی توابع C# قابل ترجمه به SQL نیستند
        // ------------------------------------------------------------
        public List<User> NotTranslatable()
        {
            // این خطا می‌دهد چون EF نمی‌داند MyCustomCheck را
            // چطور به SQL تبدیل کند
            // return _db.Users.Where(u => MyCustomCheck(u)).ToList();

            // راه درست: اول با SQL تا جایی که می‌شود فیلتر کن،
            // بعد با AsEnumerable بقیه را در حافظه انجام بده
            return _db.Users
                .Where(u => u.IsActive)      // این در SQL اجرا می‌شود
                .AsEnumerable()              // از اینجا به بعد در حافظه
                .Where(u => MyCustomCheck(u))
                .ToList();
        }

        private bool MyCustomCheck(User u) => u.Name.Length % 2 == 0;
    }

    // کلاس ساختگی برای اینکه مثال کامپایل‌پذیر باشد
    public class AppDbContext
    {
        public IQueryable<User> Users { get; set; }
    }
}
