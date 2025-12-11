using Microsoft.AspNetCore.Mvc;
using StoreFront.Model;
using StoreFront.Service;

namespace StoreFront.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProductClient _client;

        public HomeController(ProductClient client)
        {
            _client = client;
        }

        // 1. INDEX (List)
        public async Task<IActionResult> Index()
        {
            ViewBag.CurrentWarehouse = Request.Cookies["CurrentWarehouse"] ?? "warehouse-a";
            var products = await _client.GetAllAsync();
            return View(products);
        }

        [HttpPost]
        public IActionResult SetWarehouse(string warehouse)
        {
            Response.Cookies.Append("CurrentWarehouse", warehouse);
            return RedirectToAction("Index");
        }

        // 2. CREATE (The Form)
        public IActionResult Create()
        {
            return View();
        }

        // 2. CREATE (The Action)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _client.CreateAsync(model);
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // 3. EDIT (The Form)
        public async Task<IActionResult> Edit(string id)
        {
            var product = await _client.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // 3. EDIT (The Action)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _client.UpdateAsync(id, model);
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // 4. DELETE (The Confirmation Page)
        public async Task<IActionResult> Delete(string id)
        {
            var product = await _client.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // 4. DELETE (The Action)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _client.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}