using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace InterviewPrep.Concurrency
{
    // ================================================================
    //  Multithreading — در آگهی به‌عنوان "مزیت" ذکر شده
    // ================================================================

    public class ThreadingBasics
    {
        // ------------------------------------------------------------
        // Thread در برابر Task
        // ------------------------------------------------------------
        public void ThreadVsTask()
        {
            // Thread: یک رشته واقعی سیستم‌عامل
            //   - حدود ۱ مگابایت حافظه stack می‌گیرد
            //   - ساخت و نابودی‌اش پرهزینه است
            //   - خروجی برنگرداندن، ترکیب‌پذیر نیست
            var thread = new Thread(() => Console.WriteLine("سلام"));
            thread.Start();
            thread.Join();      // منتظر بمان تا تمام شود

            // Task: انتزاع سطح بالاتر روی Thread Pool
            //   - رشته‌ها بازاستفاده می‌شوند (سبک‌تر)
            //   - می‌تواند نتیجه برگرداند
            //   - قابل ترکیب با ContinueWith و await
            //   - مدیریت خطای بهتر
            var task = Task.Run(() => 42);
            int result = task.Result;
        }

        // ------------------------------------------------------------
        // Race Condition — مشکل کلاسیک
        // ------------------------------------------------------------
        private int _counter = 0;

        public void RaceConditionProblem()
        {
            // ❌ نتیجه غیرقابل پیش‌بینی است
            System.Threading.Tasks.Parallel.For(0, 100_000, i =>
            {
                _counter++;
                // چرا مشکل دارد؟
                // ++ در واقع سه عمل است:
                //   ۱. خواندن مقدار فعلی
                //   ۲. اضافه کردن یک
                //   ۳. نوشتن مقدار جدید
                //
                // اگر دو رشته همزمان مرحله ۱ را انجام دهند،
                // هر دو مقدار یکسان می‌خوانند و یکی از افزایش‌ها گم می‌شود
            });

            Console.WriteLine(_counter);   // معمولاً کمتر از ۱۰۰۰۰۰
        }

        // ------------------------------------------------------------
        // راه‌حل ۱: lock
        // ------------------------------------------------------------
        private readonly object _lockObject = new object();

        public void WithLock()
        {
            System.Threading.Tasks.Parallel.For(0, 100_000, i =>
            {
                lock (_lockObject)
                {
                    _counter++;
                }
            });
            // درست است ولی کند — هر رشته باید منتظر بماند
        }

        // نکات lock:
        //   - هرگز روی this یا typeof(X) یا رشته قفل نزن
        //     چون کد بیرونی هم می‌تواند روی همان قفل بزند
        //   - همیشه یک آبجکت private readonly اختصاصی بساز
        //   - بخش قفل‌شده را تا حد ممکن کوتاه نگه دار
        //   - داخل lock هرگز await نزن (کامپایلر هم اجازه نمی‌دهد)

        // ------------------------------------------------------------
        // راه‌حل ۲: Interlocked — سریع‌تر برای عملیات ساده
        // ------------------------------------------------------------
        public void WithInterlocked()
        {
            System.Threading.Tasks.Parallel.For(0, 100_000, i =>
            {
                Interlocked.Increment(ref _counter);
            });
            // عملیات اتمیک در سطح CPU — بدون قفل، خیلی سریع‌تر
        }

        // ------------------------------------------------------------
        // راه‌حل ۳: مجموعه‌های Concurrent
        // ------------------------------------------------------------
        public void ConcurrentCollections()
        {
            // ❌ Dictionary معمولی thread-safe نیست
            // ممکن است ساختار داخلی‌اش خراب شود و حلقه بی‌نهایت بسازد

            // ✅
            var dict = new ConcurrentDictionary<string, int>();
            dict.TryAdd("a", 1);
            dict.AddOrUpdate("a", 1, (key, old) => old + 1);
            dict.GetOrAdd("b", k => 10);

            var queue = new ConcurrentQueue<string>();
            queue.Enqueue("کار ۱");
            if (queue.TryDequeue(out var item)) { }

            var bag = new ConcurrentBag<int>();
            bag.Add(5);
        }

        // ------------------------------------------------------------
        // Deadlock — و چطور از آن جلوگیری کنیم
        // ------------------------------------------------------------
        private readonly object _lockA = new object();
        private readonly object _lockB = new object();

        public void DeadlockExample()
        {
            // رشته ۱: A را می‌گیرد، منتظر B است
            Task.Run(() =>
            {
                lock (_lockA)
                {
                    Thread.Sleep(100);
                    lock (_lockB) { }
                }
            });

            // رشته ۲: B را می‌گیرد، منتظر A است
            Task.Run(() =>
            {
                lock (_lockB)
                {
                    Thread.Sleep(100);
                    lock (_lockA) { }
                }
            });

            // هر دو برای همیشه منتظر می‌مانند

            // راه‌حل: همیشه قفل‌ها را به یک ترتیب ثابت بگیر
            // مثلاً همه‌جا اول A بعد B
        }

        // ------------------------------------------------------------
        // Parallel در برابر async
        // ------------------------------------------------------------
        public void CpuBoundWork(List<int> data)
        {
            // ✅ Parallel برای کار CPU-سنگین
            System.Threading.Tasks.Parallel.ForEach(data, item =>
            {
                HeavyCalculation(item);
            });

            // یا با PLINQ
            var results = data.AsParallel()
                              .Where(x => IsPrime(x))
                              .ToList();
        }

        public async Task IoBoundWork(List<string> urls)
        {
            // ✅ async برای کار I/O
            var tasks = urls.Select(url => DownloadAsync(url));
            await Task.WhenAll(tasks);
        }

        // هشدار: Parallel در وب‌سرور معمولاً اشتباه است
        // چون سرور همین الان هم درخواست‌ها را موازی پردازش می‌کند
        // و گرفتن رشته‌های بیشتر، ظرفیت کلی را کم می‌کند

        private void HeavyCalculation(int x) { }
        private bool IsPrime(int x) => true;
        private Task<string> DownloadAsync(string url) => Task.FromResult("");

        // ------------------------------------------------------------
        // CancellationToken — لغو عملیات طولانی
        // ------------------------------------------------------------
        public async Task<string> CancellableWork(CancellationToken ct)
        {
            for (int i = 0; i < 1000; i++)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(100, ct);
            }
            return "تمام شد";
        }

        public async Task WithTimeout()
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                try
                {
                    await CancellableWork(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("زمان تمام شد");
                }
            }
        }
    }

    // ================================================================
    //  SignalR — ارتباط بلادرنگ (در آگهی به‌عنوان مزیت ذکر شده)
    // ================================================================

    // Hub = نقطه ارتباطی بین سرور و کلاینت‌ها
    public class NotificationHub : Hub
    {
        // کلاینت می‌تواند این را صدا بزند
        public void SendToAll(string message)
        {
            // سرور به همه کلاینت‌ها پیام می‌فرستد
            Clients.All.receiveMessage(message);
        }

        // فرستادن به یک کاربر خاص
        public void SendToUser(string userId, string message)
        {
            Clients.User(userId).receiveMessage(message);
        }

        // گروه‌بندی — مثلاً همه کاربران یک سازمان
        public Task JoinGroup(string groupName)
            => Groups.Add(Context.ConnectionId, groupName);

        public void SendToGroup(string groupName, string message)
        {
            Clients.Group(groupName).receiveMessage(message);
        }

        // رویدادهای چرخه عمر اتصال
        public override Task OnConnected()
        {
            Console.WriteLine($"متصل شد: {Context.ConnectionId}");
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            Console.WriteLine($"قطع شد: {Context.ConnectionId}");
            return base.OnDisconnected(stopCalled);
        }
    }

    // فرستادن پیام از بیرون Hub (مثلاً از یک سرویس)
    public class OrderNotifier
    {
        public void NotifyOrderShipped(string userId, int orderId)
        {
            var hub = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
            hub.Clients.User(userId).receiveMessage($"سفارش {orderId} ارسال شد");
        }
    }
}
