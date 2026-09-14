using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace InterviewPrep.EntityFramework
{
    // ================================================================
    //  تراکنش، همزمانی و عملیات انبوه در EF
    // ================================================================

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }

        // Timestamp = ستونی که SQL Server خودکار با هر تغییر عوض می‌کند
        // EF از آن برای تشخیص تعارض همزمانی استفاده می‌کند
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }

    public class InventoryContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
    }

    public class TransactionExamples
    {
        // ------------------------------------------------------------
        // SaveChanges خودش یک تراکنش است
        // ------------------------------------------------------------
        public void ImplicitTransaction()
        {
            using (var db = new InventoryContext())
            {
                var p1 = db.Products.Find(1);
                var p2 = db.Products.Find(2);

                p1.Stock -= 1;
                p2.Stock -= 1;

                db.SaveChanges();
                // هر دو UPDATE در یک تراکنش اجرا می‌شوند
                // اگر دومی شکست بخورد، اولی هم برمی‌گردد
            }
        }

        // ------------------------------------------------------------
        // تراکنش صریح — وقتی چند SaveChanges داری
        // ------------------------------------------------------------
        public void ExplicitTransaction()
        {
            using (var db = new InventoryContext())
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var product = db.Products.Find(1);
                    product.Stock -= 5;
                    db.SaveChanges();          // SaveChanges اول

                    var order = new Order { Total = 500_000, CreatedAt = DateTime.Now };
                    db.Orders.Add(order);
                    db.SaveChanges();          // SaveChanges دوم

                    // فقط اینجا واقعاً در دیتابیس ثبت می‌شود
                    transaction.Commit();
                }
                catch
                {
                    // هر دو SaveChanges برمی‌گردند
                    transaction.Rollback();
                    throw;
                }
            }
        }

        // ------------------------------------------------------------
        // مشکل همزمانی: Lost Update
        // ------------------------------------------------------------
        public void TheLostUpdateProblem()
        {
            // سناریو:
            //  زمان ۱: کاربر A محصول را می‌خواند → Stock = 10
            //  زمان ۲: کاربر B همان محصول را می‌خواند → Stock = 10
            //  زمان ۳: کاربر A موجودی را ۳ تا کم می‌کند → Stock = 7 ذخیره می‌شود
            //  زمان ۴: کاربر B موجودی را ۲ تا کم می‌کند → Stock = 8 ذخیره می‌شود
            //
            //  نتیجه: ۵ تا فروخته شد ولی Stock = 8 است!
            //  تغییر کاربر A گم شد.
        }

        // ------------------------------------------------------------
        // راه‌حل ۱: Optimistic Concurrency (خوش‌بینانه)
        // ------------------------------------------------------------
        public bool OptimisticConcurrency(int productId, int quantity)
        {
            using (var db = new InventoryContext())
            {
                var product = db.Products.Find(productId);
                product.Stock -= quantity;

                try
                {
                    db.SaveChanges();
                    // EF این SQL را می‌زند:
                    //   UPDATE Products SET Stock = @s
                    //   WHERE Id = @id AND RowVersion = @originalVersion
                    //
                    // اگر کسی دیگر بین خواندن و نوشتن رکورد را عوض کرده باشد،
                    // RowVersion جور در نمی‌آید و هیچ سطری آپدیت نمی‌شود
                    return true;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // کسی زودتر از ما رکورد را عوض کرده
                    // سه استراتژی برای حل:

                    // الف) برنده کاربر است — مقدار ما را زورکی بنویس
                    // var entry = ex.Entries.Single();
                    // entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                    // db.SaveChanges();

                    // ب) برنده دیتابیس است — تغییر ما را دور بریز
                    // ج) به کاربر بگو و بگذار خودش تصمیم بگیرد

                    return false;
                }
            }
        }

        // ------------------------------------------------------------
        // راه‌حل ۲: کاهش اتمیک در خود دیتابیس
        // ------------------------------------------------------------
        public bool AtomicDecrement(int productId, int quantity)
        {
            using (var db = new InventoryContext())
            {
                // به‌جای خواندن و نوشتن، مستقیم در دیتابیس کم کن
                // شرط Stock >= quantity جلوی منفی شدن را می‌گیرد
                int affected = db.Database.ExecuteSqlCommand(
                    @"UPDATE Products
                      SET Stock = Stock - @qty
                      WHERE Id = @id AND Stock >= @qty",
                    new System.Data.SqlClient.SqlParameter("@qty", quantity),
                    new System.Data.SqlClient.SqlParameter("@id", productId));

                return affected > 0;

                // این روش کاملاً اتمیک است — هیچ فاصله‌ای بین خواندن
                // و نوشتن وجود ندارد که کسی بتواند وسطش بپرد
            }
        }

        // ------------------------------------------------------------
        // عملیات انبوه — چرا EF کند می‌شود
        // ------------------------------------------------------------
        public void SlowBulkInsert(List<Product> products)
        {
            using (var db = new InventoryContext())
            {
                foreach (var p in products)
                    db.Products.Add(p);
                    // ❌ EF بعد از هر Add، DetectChanges را صدا می‌زند
                    // که تمام موجودیت‌های ردیابی‌شده را بررسی می‌کند
                    // با ۱۰ هزار رکورد، این می‌شود O(n²)

                db.SaveChanges();
            }
        }

        public void FasterBulkInsert(List<Product> products)
        {
            using (var db = new InventoryContext())
            {
                db.Configuration.AutoDetectChangesEnabled = false;

                foreach (var p in products)
                    db.Products.Add(p);

                db.Configuration.AutoDetectChangesEnabled = true;
                db.SaveChanges();

                // هنوز هم SaveChanges به‌ازای هر رکورد یک INSERT می‌زند
                // برای حجم واقعاً بالا باید سراغ SqlBulkCopy یا
                // کتابخانه EntityFramework.BulkExtensions رفت
            }
        }

        // ------------------------------------------------------------
        // چرا DbContext باید عمر کوتاهی داشته باشد
        // ------------------------------------------------------------
        public void WhyShortLivedContext()
        {
            // ❌ context طولانی‌عمر
            //   - Change Tracker مدام بزرگ‌تر می‌شود → مصرف حافظه
            //   - DetectChanges کندتر و کندتر می‌شود
            //   - داده کهنه در کش first-level می‌ماند
            //   - thread-safe نیست

            // ✅ در ASP.NET: یک context برای هر درخواست HTTP
            //   با IoC Container این را per-request ثبت می‌کنی
        }
    }

    public class Order
    {
        public int Id { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
