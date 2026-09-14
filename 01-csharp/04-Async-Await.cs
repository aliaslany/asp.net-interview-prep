using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace InterviewPrep.CSharp
{
    // ================================================================
    //  async / await — و تله deadlock در ASP.NET Framework
    // ================================================================

    public class AsyncBasics
    {
        private static readonly HttpClient _http = new HttpClient();

        // ------------------------------------------------------------
        // الگوی درست: از بالا تا پایین async
        // ------------------------------------------------------------
        public async Task<string> GetDataAsync(string url)
        {
            // در طول این await، رشته (thread) آزاد می‌شود و
            // می‌تواند به درخواست دیگری سرویس بدهد
            var response = await _http.GetStringAsync(url);
            return response;
        }

        // ------------------------------------------------------------
        // خطای مرگبار: .Result یا .Wait() روی متد async
        // در ASP.NET Framework و WinForms باعث Deadlock می‌شود
        // ------------------------------------------------------------
        public string DeadlockTrap(string url)
        {
            // ❌ هرگز این کار را نکن
            // return GetDataAsync(url).Result;

            // چرا؟
            // 1. رشته اصلی روی .Result بلاک می‌شود و منتظر نتیجه می‌ماند
            // 2. وقتی کار async تمام شد، می‌خواهد به همان SynchronizationContext برگردد
            // 3. ولی آن context توسط رشته بلاک‌شده اشغال شده
            // 4. هر دو برای همیشه منتظر هم می‌مانند → Deadlock

            return "به‌جایش متد را async کن و await بزن";
        }

        // ------------------------------------------------------------
        // اجرای موازی — نکته‌ای که امتیاز می‌گیرد
        // ------------------------------------------------------------
        public async Task<int> Sequential()
        {
            // هر کدام ۱ ثانیه → مجموع ۳ ثانیه
            var a = await GetDataAsync("https://api.example.com/a");
            var b = await GetDataAsync("https://api.example.com/b");
            var c = await GetDataAsync("https://api.example.com/c");

            return a.Length + b.Length + c.Length;
        }

        public async Task<int> Parallel()
        {
            // هر سه همزمان شروع می‌شوند → مجموع ۱ ثانیه
            var taskA = GetDataAsync("https://api.example.com/a");
            var taskB = GetDataAsync("https://api.example.com/b");
            var taskC = GetDataAsync("https://api.example.com/c");

            var results = await Task.WhenAll(taskA, taskB, taskC);

            return results.Sum(r => r.Length);
        }

        // ------------------------------------------------------------
        // async void فقط برای event handler ها
        // ------------------------------------------------------------

        // ❌ بد: خطا قابل گرفتن نیست و کل پروسه crash می‌کند
        public async void BadFireAndForget()
        {
            await Task.Delay(100);
            throw new Exception("این خطا را هیچ‌کس نمی‌گیرد");
        }

        // ✅ خوب: خطا قابل مدیریت است
        public async Task GoodAsync()
        {
            await Task.Delay(100);
            throw new Exception("این خطا با try/catch گرفته می‌شود");
        }

        // ------------------------------------------------------------
        // ConfigureAwait(false) — برای کد کتابخانه‌ای
        // ------------------------------------------------------------
        public async Task<string> LibraryCodeAsync(string url)
        {
            // ConfigureAwait(false) یعنی «لازم نیست به context اصلی برگردی»
            // در کد کتابخانه‌ای این هم کارایی را بهتر می‌کند
            // هم جلوی deadlock را می‌گیرد
            var data = await _http.GetStringAsync(url).ConfigureAwait(false);

            return data.ToUpper();
        }

        // ------------------------------------------------------------
        // async برای I/O است، نه برای CPU
        // ------------------------------------------------------------
        public async Task<int> CorrectUsage_IO()
        {
            // ✅ درست: عملیات I/O — دیتابیس، شبکه، فایل
            using (var conn = new SqlConnection("..."))
            {
                await conn.OpenAsync();
                // ...
                return 1;
            }
        }

        public int WrongUsage_CPU()
        {
            // ❌ async اینجا فایده‌ای ندارد — کار CPU است
            // async رشته را آزاد نمی‌کند وقتی CPU مشغول محاسبه است
            long sum = 0;
            for (int i = 0; i < 1_000_000_000; i++) sum += i;
            return (int)(sum % 100);

            // اگر می‌خواهی UI بلاک نشود، از Task.Run استفاده کن:
            //   await Task.Run(() => HeavyCalculation());
            // ولی در وب‌سرور این کار معمولاً اشتباه است، چون
            // فقط کار را از یک رشته به رشته دیگر منتقل می‌کند
        }

        // ------------------------------------------------------------
        // مدیریت خطا در async
        // ------------------------------------------------------------
        public async Task ErrorHandling()
        {
            try
            {
                await GoodAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"گرفته شد: {ex.Message}");
            }

            // با Task.WhenAll، اگر چند تسک خطا بدهند،
            // فقط اولی در catch دیده می‌شود
            var tasks = new[] { GoodAsync(), GoodAsync() };
            try
            {
                await Task.WhenAll(tasks);
            }
            catch
            {
                // برای دیدن همه خطاها باید به خود تسک‌ها نگاه کنی
                foreach (var t in tasks.Where(t => t.IsFaulted))
                    Console.WriteLine(t.Exception?.Message);
            }
        }
    }
}
