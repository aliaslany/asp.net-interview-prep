using System;
using System.Collections.Generic;

namespace InterviewPrep.Patterns
{
    // ================================================================
    //  Singleton — فقط یک نمونه در کل برنامه
    // ================================================================

    // ❌ نسخه‌ای که در محیط چندرشته‌ای می‌شکند
    public class BadSingleton
    {
        private static BadSingleton _instance;

        private BadSingleton() { }

        public static BadSingleton Instance
        {
            get
            {
                // اگر دو رشته همزمان به اینجا برسند و هر دو
                // _instance را null ببینند، دو نمونه ساخته می‌شود
                if (_instance == null)
                    _instance = new BadSingleton();

                return _instance;
            }
        }
    }

    // ✅ روش ۱: قفل دوگانه (Double-Check Locking)
    public class LockedSingleton
    {
        private static LockedSingleton _instance;
        private static readonly object _lock = new object();

        private LockedSingleton() { }

        public static LockedSingleton Instance
        {
            get
            {
                if (_instance == null)              // بررسی اول بدون قفل (سریع)
                {
                    lock (_lock)
                    {
                        if (_instance == null)      // بررسی دوم داخل قفل
                            _instance = new LockedSingleton();
                    }
                }
                return _instance;
            }
        }
    }

    // ✅ روش ۲: Lazy<T> — تمیزترین راه در .NET
    public class LazySingleton
    {
        private static readonly Lazy<LazySingleton> _lazy =
            new Lazy<LazySingleton>(() => new LazySingleton());

        private LazySingleton() { }

        public static LazySingleton Instance => _lazy.Value;

        // Lazy<T> به‌صورت پیش‌فرض thread-safe است
        // و تا اولین دسترسی، نمونه ساخته نمی‌شود
    }

    // ✅ روش ۳: static readonly — ساده‌ترین
    public class EagerSingleton
    {
        public static readonly EagerSingleton Instance = new EagerSingleton();

        private EagerSingleton() { }

        // CLR تضمین می‌کند فیلد static فقط یک بار مقداردهی می‌شود
        // عیب: حتی اگر هیچ‌وقت استفاده نشود، ساخته می‌شود
    }

    // ================================================================
    //  Factory — ساخت آبجکت بدون افشای منطق ساخت
    // ================================================================

    public interface IPaymentGateway
    {
        string Pay(decimal amount);
    }

    public class SamanGateway : IPaymentGateway
    {
        public string Pay(decimal amount) => $"پرداخت {amount:N0} از درگاه سامان";
    }

    public class MellatGateway : IPaymentGateway
    {
        public string Pay(decimal amount) => $"پرداخت {amount:N0} از درگاه ملت";
    }

    public class ZarinpalGateway : IPaymentGateway
    {
        public string Pay(decimal amount) => $"پرداخت {amount:N0} از زرین‌پال";
    }

    // ❌ بدون Factory: کد ساخت در همه‌جای پروژه تکرار می‌شود
    public class WithoutFactory
    {
        public void Checkout(string gateway, decimal amount)
        {
            IPaymentGateway g;
            if (gateway == "saman") g = new SamanGateway();
            else if (gateway == "mellat") g = new MellatGateway();
            else g = new ZarinpalGateway();

            g.Pay(amount);
        }
        // این if در هر جایی که پرداخت لازم باشد تکرار می‌شود
    }

    // ✅ با Factory: منطق ساخت در یک جا
    public interface IPaymentGatewayFactory
    {
        IPaymentGateway Create(string gatewayName);
    }

    public class PaymentGatewayFactory : IPaymentGatewayFactory
    {
        private readonly Dictionary<string, Func<IPaymentGateway>> _map =
            new Dictionary<string, Func<IPaymentGateway>>(StringComparer.OrdinalIgnoreCase)
            {
                ["saman"]    = () => new SamanGateway(),
                ["mellat"]   = () => new MellatGateway(),
                ["zarinpal"] = () => new ZarinpalGateway(),
            };

        public IPaymentGateway Create(string gatewayName)
        {
            if (!_map.TryGetValue(gatewayName, out var creator))
                throw new NotSupportedException($"درگاه {gatewayName} پشتیبانی نمی‌شود");

            return creator();
        }
    }

    public class CheckoutService
    {
        private readonly IPaymentGatewayFactory _factory;

        public CheckoutService(IPaymentGatewayFactory factory) => _factory = factory;

        public string Checkout(string gatewayName, decimal amount)
        {
            var gateway = _factory.Create(gatewayName);
            return gateway.Pay(amount);
        }
        // این کلاس اصلاً نمی‌داند چند درگاه وجود دارد
    }

    // ================================================================
    //  Strategy — الگوریتم‌های قابل تعویض در زمان اجرا
    // ================================================================

    public interface IShippingStrategy
    {
        decimal CalculateCost(decimal weight, decimal distance);
        string Name { get; }
    }

    public class StandardShipping : IShippingStrategy
    {
        public string Name => "پست معمولی";
        public decimal CalculateCost(decimal weight, decimal distance)
            => weight * 2_000 + distance * 500;
    }

    public class ExpressShipping : IShippingStrategy
    {
        public string Name => "پست پیشتاز";
        public decimal CalculateCost(decimal weight, decimal distance)
            => (weight * 2_000 + distance * 500) * 2;
    }

    public class FreeShipping : IShippingStrategy
    {
        public string Name => "ارسال رایگان";
        public decimal CalculateCost(decimal weight, decimal distance) => 0;
    }

    public class ShippingCalculator
    {
        private IShippingStrategy _strategy;

        public ShippingCalculator(IShippingStrategy strategy) => _strategy = strategy;

        // می‌توان استراتژی را در زمان اجرا عوض کرد
        public void SetStrategy(IShippingStrategy strategy) => _strategy = strategy;

        public decimal Calculate(decimal weight, decimal distance)
            => _strategy.CalculateCost(weight, distance);
    }

    public class StrategyUsage
    {
        public void Demo(decimal orderTotal)
        {
            var calculator = new ShippingCalculator(new StandardShipping());

            // قانون کسب‌وکار: سفارش بالای ۵۰۰ هزار، ارسال رایگان
            if (orderTotal > 500_000)
                calculator.SetStrategy(new FreeShipping());

            var cost = calculator.Calculate(weight: 2, distance: 100);
        }
    }

    // ================================================================
    //  تفاوت Factory و Strategy — سوال پرتکرار
    // ================================================================
    //
    //  Factory:  "کدام آبجکت را بسازم؟"     ← درباره ساخت
    //  Strategy: "کدام الگوریتم را اجرا کنم؟" ← درباره رفتار
    //
    //  Factory معمولاً یک بار در ابتدا صدا زده می‌شود.
    //  Strategy می‌تواند وسط کار عوض شود.
    //
    //  در عمل زیاد با هم استفاده می‌شوند:
    //  یک Factory که Strategy مناسب را می‌سازد.
}
