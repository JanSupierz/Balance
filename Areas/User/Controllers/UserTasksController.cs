using Balance.Models;
using Balance.Models.Enums;
using Balance.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Balance.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "User")]
    public class UserTasksController : Controller
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        // ApplicationDbContext is injected to allow specific complex queries if needed,
        // specifically for fetching tasks with tags via UnitOfWork if the generic repo isn't sufficient.
        private readonly ApplicationDbContext _context;

        public UserTasksController(UnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _context = context;
        }

        // GET: User/UserTasks
        public async Task<IActionResult> Index(string sortOrder, int? filterTagId, Frequency? filterFrequency)
        {
            var userId = _userManager.GetUserId(User);

            // 1. Fetch ALL tasks for user with Tags included
            var tasksEnumerable = await _unitOfWork.RepeatedTasks.GetAllAsync(
                filter: t => t.UserId == userId,
                includeProperties: "Tags"
            );

            var tasks = tasksEnumerable.ToList();

            // 2. Run Reset Logic (Daily/Weekly Maintenance)
            bool dataChanged = false;
            var today = DateTime.Today;

            foreach (var task in tasks)
            {
                bool shouldReset = false;

                // Daily Reset
                if (task.Frequency == Frequency.Daily)
                {
                    if (task.LastModified.Date < today) shouldReset = true;
                }
                // Weekly Reset (Monday)
                else if (task.Frequency == Frequency.Weekly)
                {
                    int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                    var startOfWeek = today.AddDays(-1 * diff).Date;
                    if (task.LastModified.Date < startOfWeek) shouldReset = true;
                }
                // OneTime tasks NEVER reset automatically.

                if (shouldReset)
                {
                    task.CompletedCount = 0;
                    task.CompletedAt = null;
                    task.LastModified = DateTime.Now;

                    // Recalculate Deadline for the new cycle
                    if (task.Frequency == Frequency.Daily)
                    {
                        task.Deadline = DateTime.Today.AddDays(1);
                    }
                    else if (task.Frequency == Frequency.Weekly)
                    {
                        int days = ((int)DayOfWeek.Monday - (int)DateTime.Today.DayOfWeek + 7) % 7;
                        if (days == 0) days = 7;
                        task.Deadline = DateTime.Today.AddDays(days);
                    }

                    _unitOfWork.RepeatedTasks.Update(task);
                    dataChanged = true;
                }
            }

            if (dataChanged) await _unitOfWork.SaveAsync();

            // 3. Apply Filtering
            if (filterTagId.HasValue)
            {
                tasks = tasks.Where(t => t.Tags.Any(tag => tag.Id == filterTagId.Value)).ToList();
            }

            if (filterFrequency.HasValue)
            {
                tasks = tasks.Where(t => t.Frequency == filterFrequency.Value).ToList();
            }

            // 4. Apply Sorting
            switch (sortOrder)
            {
                case "title":
                    tasks = tasks.OrderBy(t => t.Title).ToList();
                    break;

                // "Smart" Priority Sort (Default)
                default:
                    var now = DateTime.Now;
                    var isWeekend = now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;

                    tasks = tasks.OrderBy(t =>
                    {
                        // Priority 1: OneTime tasks that are Overdue or Due within 24h
                        bool isOneTimeUrgent = t.Frequency == Frequency.OneTime && (t.Deadline < now || (t.Deadline - now).TotalDays <= 1);
                        if (isOneTimeUrgent) return 1;

                        // Priority 2: Weekly tasks IF it is the weekend (Urgent)
                        bool isWeeklyUrgent = t.Frequency == Frequency.Weekly && isWeekend;
                        if (isWeeklyUrgent) return 2;

                        // Priority 3: Daily Tasks (Routine)
                        if (t.Frequency == Frequency.Daily) return 3;

                        // Priority 4: Everything else
                        return 4;
                    })
                    .ThenBy(t => t.Deadline) // Tie-breaker
                    .ToList();
                    break;
            }

            // 5. Prepare View Data
            var finishedTasks = tasks.Where(t => t.CompletedCount >= t.HowManyTimes).ToList();
            var activeTasks = tasks.Where(t => t.CompletedCount < t.HowManyTimes).ToList();

            ViewBag.FinishedTasks = finishedTasks;

            // Pass Filter/Sort Data back to View
            ViewBag.CurrentSort = sortOrder;
            ViewBag.CurrentTag = filterTagId;
            ViewBag.CurrentFreq = filterFrequency;
            ViewBag.AllTags = await _unitOfWork.TaskTags.GetAllAsync(t => t.UserId == userId);

            // Pass Templates for the Modal
            ViewBag.PredefinedTasks = await _unitOfWork.PredefinedTasks.GetAllAsync();

            return View(activeTasks);
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserTask task, int[] selectedTagIds, DateTime? customDeadline)
        {
            var userId = _userManager.GetUserId(User);
            ModelState.Remove("UserId");

            if (!ModelState.IsValid)
            {
                ViewBag.UserTags = await _unitOfWork.TaskTags.GetAllAsync(t => t.UserId == userId);
                return View(task);
            }

            task.UserId = userId;
            task.CompletedCount = 0;
            task.CompletedAt = null;
            task.LastModified = DateTime.Now;

            // --- DEADLINE LOGIC ---
            if (task.Frequency == Frequency.OneTime)
            {
                // Set to End of Selected Day
                task.Deadline = customDeadline.HasValue
                    ? customDeadline.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59)
                    : DateTime.Today.AddHours(23).AddMinutes(59).AddSeconds(59);
            }
            else if (task.Frequency == Frequency.Daily)
            {
                task.Deadline = DateTime.Today.AddDays(1);
            }
            else if (task.Frequency == Frequency.Weekly)
            {
                int daysUntilNextMonday = ((int)DayOfWeek.Monday - (int)DateTime.Today.DayOfWeek + 7) % 7;
                if (daysUntilNextMonday == 0) daysUntilNextMonday = 7;
                task.Deadline = DateTime.Today.AddDays(daysUntilNextMonday);
            }

            // Handle Tags
            if (selectedTagIds != null && selectedTagIds.Any())
            {
                var tags = await _unitOfWork.TaskTags.GetAllAsync(t => selectedTagIds.Contains(t.Id) && t.UserId == userId);
                task.Tags = tags.ToList();
            }

            await _unitOfWork.RepeatedTasks.AddAsync(task);
            await _unitOfWork.SaveAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: Import Template
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(int templateId)
        {
            var template = await _unitOfWork.PredefinedTasks.GetByIdAsync(templateId);
            if (template == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            var newTask = new UserTask
            {
                Title = template.Title,
                Description = template.Description,
                PointsPerClick = template.PointsPerClick,
                Frequency = template.Frequency,
                HowManyTimes = template.HowManyTimes,
                UserId = userId,
                CompletedCount = 0,
                LastModified = DateTime.Now
            };

            // Calculate Deadline
            if (newTask.Frequency == Frequency.Daily)
            {
                newTask.Deadline = DateTime.Today.AddDays(1);
            }
            else if (newTask.Frequency == Frequency.Weekly)
            {
                int daysUntilNextMonday = ((int)DayOfWeek.Monday - (int)DateTime.Today.DayOfWeek + 7) % 7;
                if (daysUntilNextMonday == 0) daysUntilNextMonday = 7;
                newTask.Deadline = DateTime.Today.AddDays(daysUntilNextMonday);
            }
            else
            {
                newTask.Deadline = DateTime.Today.AddHours(23).AddMinutes(59);
            }

            await _unitOfWork.RepeatedTasks.AddAsync(newTask);
            await _unitOfWork.SaveAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserTask task, int[] selectedTagIds, DateTime? customDeadline)
        {
            var userId = _userManager.GetUserId(User);
            var existingTasks = await _unitOfWork.RepeatedTasks.GetAllAsync(t => t.Id == id && t.UserId == userId, "Tags");
            var existingTask = existingTasks.FirstOrDefault();

            if (existingTask == null) return NotFound();

            ModelState.Remove("UserId");
            if (!ModelState.IsValid)
            {
                ViewBag.UserTags = await _unitOfWork.TaskTags.GetAllAsync(t => t.UserId == userId);
                return View(task);
            }

            // Update Fields
            existingTask.Title = task.Title;
            existingTask.Description = task.Description;
            existingTask.PointsPerClick = task.PointsPerClick;
            existingTask.Frequency = task.Frequency;
            existingTask.HowManyTimes = task.HowManyTimes;

            // Update Deadline Logic
            if (task.Frequency == Frequency.OneTime && customDeadline.HasValue)
            {
                existingTask.Deadline = customDeadline.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            }
            else if (task.Frequency != Frequency.OneTime && existingTask.Frequency == Frequency.OneTime)
            {
                // Switched from OneTime -> Regular. Provide safe default.
                existingTask.Deadline = DateTime.Today.AddDays(1);
            }

            // Update Tags
            existingTask.Tags.Clear();
            if (selectedTagIds != null && selectedTagIds.Any())
            {
                var tags = await _unitOfWork.TaskTags.GetAllAsync(t => selectedTagIds.Contains(t.Id) && t.UserId == userId);
                foreach (var tag in tags) existingTask.Tags.Add(tag);
            }

            _unitOfWork.RepeatedTasks.Update(existingTask);
            await _unitOfWork.SaveAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Create
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);
            ViewBag.UserTags = await _unitOfWork.TaskTags.GetAllAsync(t => t.UserId == userId);
            return View();
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var tasks = await _unitOfWork.RepeatedTasks.GetAllAsync(t => t.Id == id && t.UserId == userId, "Tags");
            var task = tasks.FirstOrDefault();

            if (task == null) return NotFound();

            ViewBag.UserTags = await _unitOfWork.TaskTags.GetAllAsync(t => t.UserId == userId);
            return View(task);
        }

        // GET: Details
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var tasks = await _unitOfWork.RepeatedTasks.GetAllAsync(t => t.Id == id && t.UserId == userId, "Tags");
            var task = tasks.FirstOrDefault();
            if (task == null) return NotFound();
            return View(task);
        }

        // POST: Toggle (API)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle([FromBody] ToggleTaskModel model)
        {
            var task = await _unitOfWork.RepeatedTasks.GetByIdAsync(model.Id);
            var user = await _userManager.GetUserAsync(User);

            if (task == null || user == null || task.UserId != user.Id) return BadRequest();

            if (task.CompletedCount < task.HowManyTimes)
            {
                task.CompletedCount++;
                task.LastModified = DateTime.Now;

                // Add Points
                user.CurrentPoints += task.PointsPerClick;

                if (task.CompletedCount >= task.HowManyTimes) task.CompletedAt = DateTime.Now;

                _unitOfWork.RepeatedTasks.Update(task);
                await _userManager.UpdateAsync(user);
                await _unitOfWork.SaveAsync();
            }
            return Json(GenerateResponse(task, user));
        }

        // POST: Revert (API)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revert([FromBody] ToggleTaskModel model)
        {
            var task = await _unitOfWork.RepeatedTasks.GetByIdAsync(model.Id);
            var user = await _userManager.GetUserAsync(User);

            if (task == null || user == null || task.UserId != user.Id) return BadRequest();

            if (task.CompletedCount > 0)
            {
                // If it was complete, we are undoing it
                if (task.CompletedCount >= task.HowManyTimes) task.CompletedAt = null;

                task.CompletedCount--;
                task.LastModified = DateTime.Now;

                // Remove Points
                user.CurrentPoints -= task.PointsPerClick;
                if (user.CurrentPoints < 0) user.CurrentPoints = 0;

                _unitOfWork.RepeatedTasks.Update(task);
                await _userManager.UpdateAsync(user);
                await _unitOfWork.SaveAsync();
            }
            return Json(GenerateResponse(task, user));
        }

        // POST: Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _unitOfWork.RepeatedTasks.GetByIdAsync(id);
            if (task == null || task.UserId != _userManager.GetUserId(User)) return NotFound();
            await _unitOfWork.RepeatedTasks.DeleteAsync(id);
            await _unitOfWork.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        // AJAX: Create Tag
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTag([FromBody] TagDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Name)) return BadRequest("Name required");
            var userId = _userManager.GetUserId(User);

            var existing = await _unitOfWork.TaskTags.GetAllAsync(t => t.UserId == userId && t.Name == model.Name);
            if (existing.Any()) return BadRequest("Tag exists");

            var newTag = new TaskTag
            {
                Name = model.Name.Trim(),
                Color = !string.IsNullOrEmpty(model.Color) ? model.Color : "#6c757d",
                UserId = userId
            };
            await _unitOfWork.TaskTags.AddAsync(newTag);
            await _unitOfWork.SaveAsync();

            return Json(new { id = newTag.Id, name = newTag.Name, color = newTag.Color });
        }

        // AJAX: Delete Tag
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTag([FromBody] TagDto model)
        {
            var userId = _userManager.GetUserId(User);
            var tag = (await _unitOfWork.TaskTags.GetAllAsync(t => t.Id == model.Id && t.UserId == userId)).FirstOrDefault();

            if (tag == null) return NotFound();

            await _unitOfWork.TaskTags.DeleteAsync(tag.Id);
            await _unitOfWork.SaveAsync();

            return Json(new { success = true });
        }

        private object GenerateResponse(UserTask task, ApplicationUser user)
        {
            string completedAtStr = task.CompletedAt.HasValue ? task.CompletedAt.Value.ToString("ddd, HH:mm") : "";
            return new
            {
                success = true,
                completedCount = task.CompletedCount,
                howMany = task.HowManyTimes,
                isCompleted = task.IsCompleted,
                completedAt = completedAtStr,
                newTotalPoints = user.CurrentPoints
            };
        }

        public class ToggleTaskModel { public int Id { get; set; } }
        public class TagDto { public int Id { get; set; } public string Name { get; set; } public string Color { get; set; } }
    }
}