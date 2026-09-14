using System;
using System.Collections.Generic;

namespace InterviewPrep.Oop
{
    // ================================================================
    //  چهار ستون شیءگرایی
    // ================================================================

    // ------------------------------------------------------------
    // ۱. Encapsulation — کپسوله‌سازی
    // پنهان کردن وضعیت داخلی و کنترل دسترسی به آن
    // ------------------------------------------------------------

    // ❌ بد: هر کسی می‌تواند موجودی را منفی کند
    public class BadAccount
    {
        public decimal Balance;    // فیلد عمومی = هیچ کنترلی نداریم
    }

    // ✅ خوب: تغییر موجودی فقط از راه‌های مجاز
    public class BankAccount
    {
        private decimal _balance;              // وضعیت داخلی، پنهان
        private readonly List<string> _log = new List<string>();

        public decimal Balance => _balance;    // فقط خواندنی از بیرون

        public void Deposit(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("مبلغ واریز باید مثبت باشد");

            _balance += amount;
            _log.Add($"واریز {amount:N0}");
        }

        public void Withdraw(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("مبلغ برداشت باید مثبت باشد");

            if (amount > _balance)
                throw new InvalidOperationException("موجودی کافی نیست");

            _balance -= amount;
            _log.Add($"برداشت {amount:N0}");
        }

        // کلاس خودش تضمین می‌کند که هیچ‌وقت در وضعیت نامعتبر قرار نگیرد
        // به این می‌گویند "invariant" — قانونی که همیشه باید برقرار باشد
    }

    // ------------------------------------------------------------
    // ۲. Inheritance — وراثت
    // ------------------------------------------------------------

    public abstract class Vehicle
    {
        protected Vehicle(string plate) => Plate = plate;

        public string Plate { get; }

        // رفتار مشترک همه وسایل نقلیه
        public virtual string Start() => "موتور روشن شد";

        public abstract decimal TollFee();
    }

    public class Car : Vehicle
    {
        public Car(string plate) : base(plate) { }

        public override decimal TollFee() => 5_000;
    }

    public class Truck : Vehicle
    {
        private readonly int _axles;

        public Truck(string plate, int axles) : base(plate) => _axles = axles;

        public override decimal TollFee() => 5_000 * _axles;

        public override string Start() => "موتور دیزل روشن شد";
    }

    // ------------------------------------------------------------
    // ۳. Polymorphism — چندریختی
    // یک رابط، چند رفتار
    // ------------------------------------------------------------

    public class TollGate
    {
        public decimal Collect(List<Vehicle> vehicles)
        {
            decimal total = 0;

            foreach (var v in vehicles)
            {
                // نمی‌دانیم v دقیقاً چیست — ماشین، کامیون، یا نوعی که
                // فردا اضافه می‌شود. فقط می‌دانیم TollFee دارد.
                // در زمان اجرا نسخه درست صدا زده می‌شود.
                total += v.TollFee();
            }

            return total;
        }
    }

    // دو نوع Polymorphism:

    public class Calculator
    {
        // Compile-time (Overloading): چند متد هم‌نام با امضای متفاوت
        public int Add(int a, int b) => a + b;
        public double Add(double a, double b) => a + b;
        public int Add(int a, int b, int c) => a + b + c;

        // Run-time (Overriding): همان چیزی که بالا در Vehicle دیدیم
    }

    // ------------------------------------------------------------
    // ۴. Abstraction — انتزاع
    // نمایش فقط چیزهای ضروری، پنهان کردن پیچیدگی
    // ------------------------------------------------------------

    public interface IEmailSender
    {
        // کاربر این اینترفیس نمی‌داند و لازم نیست بداند که
        // پشت صحنه SMTP است یا SendGrid یا صف پیام
        void Send(string to, string subject, string body);
    }

    public class SmtpEmailSender : IEmailSender
    {
        public void Send(string to, string subject, string body)
        {
            // اتصال SMTP، احراز هویت، رمزنگاری TLS، مدیریت خطا...
            // تمام این پیچیدگی پشت یک متد ساده پنهان شده
        }
    }

    // ------------------------------------------------------------
    // Composition در برابر Inheritance
    // اصل مهم: "ترکیب را به وراثت ترجیح بده"
    // ------------------------------------------------------------

    // ❌ وراثت اشتباه: آیا حساب پس‌انداز واقعاً "یک نوع" لاگر است؟
    // public class SavingsAccount : Logger { }

    // ✅ ترکیب: حساب پس‌انداز یک لاگر "دارد"
    public class SavingsAccount
    {
        private readonly ILogger _logger;      // has-a

        public SavingsAccount(ILogger logger) => _logger = logger;

        public void AddInterest(decimal rate)
        {
            _logger.Log($"سود {rate:P} اعمال شد");
        }
    }

    public interface ILogger
    {
        void Log(string message);
    }
}
