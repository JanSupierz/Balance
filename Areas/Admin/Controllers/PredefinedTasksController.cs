using Balance.Models;
using Balance.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Balance.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PredefinedTasksController : Controller
    {
        private readonly UnitOfWork _unitOfWork;

        public PredefinedTasksController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: Admin/PredefinedTasks
        public async Task<IActionResult> Index()
        {
            var tasks = await _unitOfWork.PredefinedTasks.GetAllAsync();
            // Optional: Sort by Title
            return View(tasks.OrderBy(t => t.Title));
        }

        // GET: Admin/PredefinedTasks/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var task = await _unitOfWork.PredefinedTasks.GetByIdAsync(id);
            if (task == null) return NotFound();
            return View(task);
        }

        // GET: Admin/PredefinedTasks/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/PredefinedTasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PredefinedTask task)
        {
            if (ModelState.IsValid)
            {
                await _unitOfWork.PredefinedTasks.AddAsync(task);
                await _unitOfWork.SaveAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(task);
        }

        // GET: Admin/PredefinedTasks/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _unitOfWork.PredefinedTasks.GetByIdAsync(id);
            if (task == null) return NotFound();
            return View(task);
        }

        // POST: Admin/PredefinedTasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PredefinedTask task)
        {
            if (id != task.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _unitOfWork.PredefinedTasks.Update(task);
                await _unitOfWork.SaveAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(task);
        }

        // GET: Admin/PredefinedTasks/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _unitOfWork.PredefinedTasks.GetByIdAsync(id);
            if (task == null) return NotFound();
            return View(task);
        }

        // POST: Admin/PredefinedTasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _unitOfWork.PredefinedTasks.DeleteAsync(id);
            await _unitOfWork.SaveAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}