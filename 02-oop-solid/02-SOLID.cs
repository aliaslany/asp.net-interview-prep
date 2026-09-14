using System;
using System.Collections.Generic;

namespace InterviewPrep.Solid
{
    // ================================================================
    //  S — Single Responsibility Principle
    //  هر کلاس فقط یک دلیل برای تغییر داشته باشد
    // ================================================================

    public class Order
    {
        public int Id { get; set; }
        public decimal Total { get; set; }
        public string CustomerEmail { get; set; }
    }

    // ❌ سه مسئولیت در یک کلاس
    public class BadOrderService
    {
        public void ProcessOrder(Order order)
        {
            // مسئولیت ۱: منطق کسب‌وکار
            if (order.Total <= 0) throw new Exception("مبلغ نامعتبر");

            // مسئولیت ۲: ذخیره در دیتابیس
            // using (var conn = new SqlConnection(...)) { ... }

            // مسئولیت ۳: ارسال ایمیل
            // var smtp = new SmtpClient(); smtp.Send(...);
        }
    }
    // اگر فرمت ایمیل عوض شود، باید این کلاس را دست بزنی
    // اگر از SQL Server به Postgres بروی، باز هم همین کلاس
    // اگر قانون کسب‌وکار عوض شود، باز هم همین کلاس
    // ← سه دلیل مختلف برای تغییر = نقض SRP

    // ✅ هر مسئولیت جدا
    public class OrderValidator
    {
        public void Validate(Order order)
        {
            if (order.Total <= 0)
                throw new ArgumentException("مبلغ سفارش باید مثبت باشد");
        }
    }

    public interface IOrderRepository
    {
        void Save(Order order);
    }

    public interface INotificationService
    {
        void NotifyOrderPlaced(Order order);
    }

    public class GoodOrderService
    {
        private readonly OrderValidator _validator;
        private readonly IOrderRepository _repository;
        private readonly INotificationService _notifier;

        public GoodOrderService(
            OrderValidator validator,
            IOrderRepository repository,
            INotificationService notifier)
        {
            _validator = validator;
            _repository = repository;
            _notifier = notifier;
        }

        // این کلاس فقط "هماهنگ‌کننده" است
        public void ProcessOrder(Order order)
        {
            _validator.Validate(order);
            _repository.Save(order);
            _notifier.NotifyOrderPlaced(order);
        }
    }

    // ================================================================
    //  O — Open/Closed Principle
    //  باز برای توسعه، بسته برای تغییر
    // ================================================================

    // ❌ برای هر نوع مشتری جدید باید این متد را دست بزنی
    public class BadDiscountCalculator
    {
        public decimal Calculate(string customerType, decimal amount)
        {
            if (customerType == "Regular") return amount * 0.05m;
            if (customerType == "Gold")    return amount * 0.10m;
            if (customerType == "Platinum") return amount * 0.20m;
            // فردا "Diamond" اضافه می‌شود → دوباره این متد را باز کن
            return 0;
        }
    }

    // ✅ نوع جدید = کلاس جدید، بدون دست زدن به کد موجود
    public interface IDiscountPolicy
    {
        decimal CalculateDiscount(decimal amount);
    }

    public class RegularDiscount : IDiscountPolicy
    {
        public decimal CalculateDiscount(decimal amount) => amount * 0.05m;
    }

    public class GoldDiscount : IDiscountPolicy
    {
        public decimal CalculateDiscount(decimal amount) => amount * 0.10m;
    }

    // فردا فقط این را اضافه می‌کنی — هیچ کد موجودی تغییر نمی‌کند
    public class DiamondDiscount : IDiscountPolicy
    {
        public decimal CalculateDiscount(decimal amount) => amount * 0.30m;
    }

    public class PriceCalculator
    {
        public decimal GetFinalPrice(decimal amount, IDiscountPolicy policy)
            => amount - policy.CalculateDiscount(amount);
    }

    // ================================================================
    //  L — Liskov Substitution Principle
    //  فرزند باید بدون شکستن چیزی جای پدر بنشیند
    // ================================================================

    // ❌ مثال کلاسیک نقض LSP
    public class Rectangle
    {
        public virtual int Width { get; set; }
        public virtual int Height { get; set; }
        public int Area() => Width * Height;
    }

    public class Square : Rectangle
    {
        // مربع باید طول و عرضش برابر باشد، پس هر دو را ست می‌کنیم
        public override int Width
        {
            get => base.Width;
            set { base.Width = value; base.Height = value; }
        }

        public override int Height
        {
            get => base.Height;
            set { base.Width = value; base.Height = value; }
        }
    }

    public class LspViolationDemo
    {
        // این متد با Rectangle درست کار می‌کند
        public void Test(Rectangle r)
        {
            r.Width = 5;
            r.Height = 4;

            // انتظار: 20
            // اگر Square پاس داده شود: 16 ← رفتار شکست!
            Console.WriteLine(r.Area());
        }
    }

    // ✅ راه‌حل: وراثت اشتباه بود. مربع از نظر رفتاری مستطیل نیست
    public interface IShape
    {
        int Area();
    }

    public class RectangleShape : IShape
    {
        public RectangleShape(int w, int h) { Width = w; Height = h; }
        public int Width { get; }
        public int Height { get; }
        public int Area() => Width * Height;
    }

    public class SquareShape : IShape
    {
        public SquareShape(int side) => Side = side;
        public int Side { get; }
        public int Area() => Side * Side;
    }

    // ================================================================
    //  I — Interface Segregation Principle
    //  چند اینترفیس کوچک بهتر از یکی بزرگ
    // ================================================================

    // ❌ اینترفیس چاق
    public interface IMachine
    {
        void Print(string doc);
        void Scan(string doc);
        void Fax(string doc);
    }

    public class OldPrinter : IMachine
    {
        public void Print(string doc) { /* درست کار می‌کند */ }

        // این پرینتر اسکنر ندارد، ولی مجبور است پیاده‌سازی کند
        public void Scan(string doc) => throw new NotImplementedException();
        public void Fax(string doc) => throw new NotImplementedException();
    }
    // پرتاب NotImplementedException نشانه قطعی نقض ISP است

    // ✅ اینترفیس‌های کوچک و مستقل
    public interface IPrinter { void Print(string doc); }
    public interface IScanner { void Scan(string doc); }
    public interface IFax { void Fax(string doc); }

    public class SimplePrinter : IPrinter
    {
        public void Print(string doc) { }
    }

    public class AllInOnePrinter : IPrinter, IScanner, IFax
    {
        public void Print(string doc) { }
        public void Scan(string doc) { }
        public void Fax(string doc) { }
    }

    // ================================================================
    //  D — Dependency Inversion Principle
    //  به انتزاع وابسته باش، نه به پیاده‌سازی
    // ================================================================

    // ❌ وابستگی سفت به کلاس مشخص
    public class BadReportService
    {
        private readonly SqlOrderRepository _repo = new SqlOrderRepository();

        public string Generate()
        {
            var orders = _repo.GetAll();
            return $"{orders.Count} سفارش";
        }
    }
    // مشکلات:
    //   - نمی‌توانی تستش کنی بدون دیتابیس واقعی
    //   - نمی‌توانی منبع داده را عوض کنی
    //   - این کلاس "می‌داند" که SQL Server وجود دارد

    // ✅ وابستگی به انتزاع، تزریق از بیرون
    public class GoodReportService
    {
        private readonly IOrderRepository2 _repo;

        public GoodReportService(IOrderRepository2 repo) => _repo = repo;

        public string Generate()
        {
            var orders = _repo.GetAll();
            return $"{orders.Count} سفارش";
        }
    }

    public interface IOrderRepository2
    {
        List<Order> GetAll();
    }

    public class SqlOrderRepository : IOrderRepository2
    {
        public List<Order> GetAll() => new List<Order>();
    }

    // در تست می‌توانی این را بدهی — بدون نیاز به دیتابیس
    public class FakeOrderRepository : IOrderRepository2
    {
        public List<Order> GetAll() => new List<Order>
        {
            new Order { Id = 1, Total = 100 },
            new Order { Id = 2, Total = 200 },
        };
    }
}
