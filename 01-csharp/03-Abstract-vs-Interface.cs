using System;

namespace InterviewPrep.CSharp
{
    // ================================================================
    //  abstract class در برابر interface
    // ================================================================

    // ------------------------------------------------------------
    // interface = فقط قرارداد. «چه کاری می‌تواند بکند»
    // ------------------------------------------------------------
    public interface IPayable
    {
        decimal CalculatePay();
    }

    public interface IReportable
    {
        string GenerateReport();
    }

    // ------------------------------------------------------------
    // abstract class = کلاس ناقص با کد مشترک. «چه چیزی هست»
    // ------------------------------------------------------------
    public abstract class Employee
    {
        // abstract class می‌تواند فیلد داشته باشد — interface نمی‌تواند
        protected readonly DateTime _hiredAt;

        // abstract class می‌تواند سازنده داشته باشد — interface نمی‌تواند
        protected Employee(string name, DateTime hiredAt)
        {
            Name = name;
            _hiredAt = hiredAt;
        }

        public string Name { get; }

        // متد معمولی با پیاده‌سازی مشترک بین همه فرزندان
        public int YearsOfService => DateTime.Now.Year - _hiredAt.Year;

        // abstract = فرزند حتماً باید پیاده‌سازی کند
        public abstract decimal CalculateSalary();

        // virtual = پیاده‌سازی پیش‌فرض دارد، فرزند می‌تواند عوضش کند
        public virtual string Describe() => $"{Name} با {YearsOfService} سال سابقه";
    }

    // ------------------------------------------------------------
    // یک کلاس فقط از یک abstract class ارث می‌برد،
    // ولی می‌تواند چند interface را پیاده‌سازی کند
    // ------------------------------------------------------------
    public class FullTimeEmployee : Employee, IPayable, IReportable
    {
        private readonly decimal _monthlySalary;

        public FullTimeEmployee(string name, DateTime hiredAt, decimal monthlySalary)
            : base(name, hiredAt)
        {
            _monthlySalary = monthlySalary;
        }

        // اجباری چون در پدر abstract بود
        public override decimal CalculateSalary() => _monthlySalary;

        // اختیاری — پیاده‌سازی پیش‌فرض را عوض می‌کنیم
        public override string Describe() => $"[تمام‌وقت] {base.Describe()}";

        public decimal CalculatePay() => CalculateSalary();

        public string GenerateReport() => $"{Name}: {CalculateSalary():N0} تومان";
    }

    public class Contractor : Employee, IPayable
    {
        private readonly decimal _hourlyRate;
        private readonly int _hours;

        public Contractor(string name, DateTime hiredAt, decimal rate, int hours)
            : base(name, hiredAt)
        {
            _hourlyRate = rate;
            _hours = hours;
        }

        public override decimal CalculateSalary() => _hourlyRate * _hours;

        public decimal CalculatePay() => CalculateSalary();

        // Describe را override نکردیم، پس نسخه پدر استفاده می‌شود
    }

    // ------------------------------------------------------------
    // نکته: interface می‌تواند روی کلاس‌هایی بنشیند که
    // هیچ رابطه ارث‌بری با هم ندارند
    // ------------------------------------------------------------
    public class Invoice : IReportable   // فاکتور کارمند نیست، ولی گزارش‌پذیر است
    {
        public string GenerateReport() => "گزارش فاکتور";
    }

    // ------------------------------------------------------------
    // Polymorphism در عمل
    // ------------------------------------------------------------
    public class PayrollService
    {
        public decimal TotalPayroll(IPayable[] items)
        {
            decimal total = 0;

            // اینجا نمی‌دانیم و اهمیتی نمی‌دهیم که هر آیتم دقیقاً چیست
            // فقط می‌دانیم قرارداد IPayable را رعایت کرده
            foreach (var item in items)
                total += item.CalculatePay();

            return total;
        }
    }

    // ------------------------------------------------------------
    // new در برابر override — تله کلاسیک مصاحبه
    // ------------------------------------------------------------
    public class Parent
    {
        public virtual string Who() => "Parent";
        public string NonVirtual() => "Parent";
    }

    public class ChildOverride : Parent
    {
        public override string Who() => "Child";        // بازنویسی واقعی
    }

    public class ChildNew : Parent
    {
        public new string NonVirtual() => "Child";      // فقط پنهان کردن
    }

    public class PolymorphismTrap
    {
        public void Demo()
        {
            Parent a = new ChildOverride();
            Console.WriteLine(a.Who());          // "Child"  ← در زمان اجرا تصمیم گرفته می‌شود

            Parent b = new ChildNew();
            Console.WriteLine(b.NonVirtual());   // "Parent" ← در زمان کامپایل تصمیم گرفته شد!

            ChildNew c = new ChildNew();
            Console.WriteLine(c.NonVirtual());   // "Child"  ← چون نوع متغیر ChildNew است
        }
    }
}
