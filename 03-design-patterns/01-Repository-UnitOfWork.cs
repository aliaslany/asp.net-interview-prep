using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace InterviewPrep.Patterns
{
    // ================================================================
    //  Repository و Unit of Work
    //  پرکاربردترین الگو در پروژه‌های .NET سازمانی
    // ================================================================

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ------------------------------------------------------------
    // Repository عمومی — عملیات مشترک همه موجودیت‌ها
    // ------------------------------------------------------------
    public interface IRepository<T> where T : class
    {
        T GetById(int id);
        IEnumerable<T> GetAll();
        IEnumerable<T> Find(System.Linq.Expressions.Expression<Func<T, bool>> predicate);
        void Add(T entity);
        void Remove(T entity);
    }

    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly DbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(DbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public T GetById(int id) => _dbSet.Find(id);

        public IEnumerable<T> GetAll() => _dbSet.ToList();

        public IEnumerable<T> Find(
            System.Linq.Expressions.Expression<Func<T, bool>> predicate)
            => _dbSet.Where(predicate).ToList();

        public void Add(T entity) => _dbSet.Add(entity);

        public void Remove(T entity) => _dbSet.Remove(entity);

        // توجه: هیچ SaveChanges اینجا نیست!
        // ذخیره‌سازی وظیفه Unit of Work است
    }

    // ------------------------------------------------------------
    // Repository اختصاصی — وقتی کوئری خاص آن موجودیت را لازم داری
    // ------------------------------------------------------------
    public interface IOrderRepository : IRepository<Order>
    {
        IEnumerable<Order> GetRecentOrders(int customerId, int days);
        decimal GetTotalSpent(int customerId);
    }

    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(DbContext context) : base(context) { }

        public IEnumerable<Order> GetRecentOrders(int customerId, int days)
        {
            var since = DateTime.Now.AddDays(-days);

            return _dbSet
                .Where(o => o.CustomerId == customerId && o.CreatedAt >= since)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();
        }

        public decimal GetTotalSpent(int customerId)
            => _dbSet.Where(o => o.CustomerId == customerId).Sum(o => (decimal?)o.Total) ?? 0;
    }

    // ------------------------------------------------------------
    // Unit of Work — مدیریت تراکنش و هماهنگی بین repository ها
    // ------------------------------------------------------------
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Customer> Customers { get; }
        IOrderRepository Orders { get; }
        int Complete();
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContext _context;

        public UnitOfWork(DbContext context)
        {
            _context = context;
            Customers = new Repository<Customer>(_context);
            Orders = new OrderRepository(_context);
        }

        public IRepository<Customer> Customers { get; }
        public IOrderRepository Orders { get; }

        // همه تغییرات در یک تراکنش ذخیره می‌شوند
        public int Complete() => _context.SaveChanges();

        public void Dispose() => _context.Dispose();
    }

    // ------------------------------------------------------------
    // استفاده در عمل
    // ------------------------------------------------------------
    public class OrderService
    {
        private readonly IUnitOfWork _uow;

        public OrderService(IUnitOfWork uow) => _uow = uow;

        public void PlaceOrder(int customerId, decimal amount)
        {
            var customer = _uow.Customers.GetById(customerId);
            if (customer == null)
                throw new InvalidOperationException("مشتری پیدا نشد");

            var order = new Order
            {
                CustomerId = customerId,
                Total = amount,
                CreatedAt = DateTime.Now
            };

            _uow.Orders.Add(order);

            // نکته کلیدی: اگر اینجا چند تغییر روی چند جدول داشته باشیم،
            // همه با یک Complete در یک تراکنش ذخیره می‌شوند.
            // یا همه موفق، یا هیچ‌کدام.
            _uow.Complete();
        }
    }

    // ------------------------------------------------------------
    // چرا این الگو مفید است: تست بدون دیتابیس
    // ------------------------------------------------------------
    public class FakeCustomerRepository : IRepository<Customer>
    {
        private readonly List<Customer> _data = new List<Customer>
        {
            new Customer { Id = 1, Name = "علی", Email = "ali@test.com" }
        };

        public Customer GetById(int id) => _data.FirstOrDefault(c => c.Id == id);
        public IEnumerable<Customer> GetAll() => _data;
        public IEnumerable<Customer> Find(
            System.Linq.Expressions.Expression<Func<Customer, bool>> p)
            => _data.AsQueryable().Where(p).ToList();
        public void Add(Customer e) => _data.Add(e);
        public void Remove(Customer e) => _data.Remove(e);
    }
}
