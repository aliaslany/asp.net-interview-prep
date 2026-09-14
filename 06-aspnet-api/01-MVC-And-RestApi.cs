using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Mvc;
using System.Web.Http;

namespace InterviewPrep.AspNet
{
    // ================================================================
    //  ASP.NET MVC — کنترلر، فیلتر، Model Binding
    // ================================================================

    public class ProductViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "نام الزامی است")]
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string Name { get; set; }

        [System.ComponentModel.DataAnnotations.Range(1, 1_000_000_000)]
        public decimal Price { get; set; }

        public int CategoryId { get; set; }
    }

    public interface IProductService
    {
        List<ProductViewModel> GetAll();
        ProductViewModel GetById(int id);
        int Create(ProductViewModel model);
        void Update(int id, ProductViewModel model);
        void Delete(int id);
    }

    // ------------------------------------------------------------
    // کنترلر MVC — خروجی View می‌دهد
    // ------------------------------------------------------------
    public class ProductsController : Controller
    {
        private readonly IProductService _service;

        // Dependency Injection از طریق سازنده
        public ProductsController(IProductService service) => _service = service;

        // GET: /Products
        public ActionResult Index()
        {
            var products = _service.GetAll();
            return View(products);
        }

        // GET: /Products/Details/5
        public ActionResult Details(int id)
        {
            var product = _service.GetById(id);

            if (product == null)
                return HttpNotFound();

            return View(product);
        }

        // GET: /Products/Create
        public ActionResult Create() => View();

        // POST: /Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]      // ← محافظت در برابر CSRF
        public ActionResult Create(ProductViewModel model)
        {
            // ModelState را همیشه چک کن — اعتبارسنجی سمت کلاینت
            // را می‌شود دور زد، پس سمت سرور اجباری است
            if (!ModelState.IsValid)
                return View(model);

            var id = _service.Create(model);

            // الگوی PRG: Post-Redirect-Get
            // بعد از POST موفق، redirect کن تا رفرش صفحه
            // باعث ارسال دوباره فرم نشود
            return RedirectToAction("Details", new { id });
        }

        // فراخوانی AJAX
        [HttpPost]
        public JsonResult QuickSearch(string term)
        {
            var results = _service.GetAll()
                .Where(p => p.Name.Contains(term))
                .Take(10)
                .Select(p => new { p.Name, p.Price });

            return Json(results, JsonRequestBehavior.AllowGet);
        }
    }

    // ------------------------------------------------------------
    // Action Filter — کد قبل و بعد از اجرای اکشن
    // ------------------------------------------------------------
    public class LogActionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var action = context.ActionDescriptor.ActionName;
            System.Diagnostics.Debug.WriteLine($"شروع: {action}");
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null)
                System.Diagnostics.Debug.WriteLine($"خطا: {context.Exception.Message}");
        }
    }

    // فیلتر مدیریت خطا
    public class CustomErrorFilter : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            // لاگ کردن خطا
            System.Diagnostics.Debug.WriteLine(context.Exception);

            // هرگز جزئیات فنی را به کاربر نشان نده
            context.Result = new ViewResult { ViewName = "Error" };
            context.ExceptionHandled = true;
        }
    }

    // ================================================================
    //  Web API — خروجی JSON برای کلاینت
    // ================================================================

    [RoutePrefix("api/products")]
    public class ProductsApiController : ApiController
    {
        private readonly IProductService _service;

        public ProductsApiController(IProductService service) => _service = service;

        // GET api/products
        [HttpGet, Route("")]
        public IHttpActionResult GetAll(int page = 1, int pageSize = 20)
        {
            // صفحه‌بندی را اجباری کن — هرگز کل جدول را برنگردان
            if (pageSize > 100) pageSize = 100;

            var items = _service.GetAll()
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                page,
                pageSize,
                items
            });
        }

        // GET api/products/5
        [HttpGet, Route("{id:int}")]
        public IHttpActionResult GetById(int id)
        {
            var product = _service.GetById(id);

            if (product == null)
                return NotFound();      // 404

            return Ok(product);         // 200
        }

        // POST api/products
        [HttpPost, Route("")]
        public IHttpActionResult Create(ProductViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);      // 400 با جزئیات خطا

            var id = _service.Create(model);

            // 201 Created + آدرس منبع جدید در هدر Location
            return Created($"api/products/{id}", new { id });
        }

        // PUT api/products/5  — جایگزینی کامل
        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, ProductViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (_service.GetById(id) == null)
                return NotFound();

            _service.Update(id, model);

            return StatusCode(HttpStatusCode.NoContent);    // 204
        }

        // DELETE api/products/5
        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            if (_service.GetById(id) == null)
                return NotFound();

            _service.Delete(id);

            return StatusCode(HttpStatusCode.NoContent);    // 204
        }
    }

    // ------------------------------------------------------------
    // مدیریت متمرکز خطا در Web API
    // ------------------------------------------------------------
    public class ApiExceptionFilter : System.Web.Http.Filters.ExceptionFilterAttribute
    {
        public override void OnException(
            System.Web.Http.Filters.HttpActionExecutedContext context)
        {
            // لاگ کن
            System.Diagnostics.Debug.WriteLine(context.Exception);

            // پاسخ یکنواخت بده، بدون افشای جزئیات داخلی
            context.Response = context.Request.CreateResponse(
                HttpStatusCode.InternalServerError,
                new { error = new { code = "INTERNAL_ERROR", message = "خطای داخلی سرور" } });
        }
    }
}
