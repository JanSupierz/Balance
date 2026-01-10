using Balance.Models;
using Balance.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Balance.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "User")]
    public class PrizesController : Controller
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public PrizesController(UnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // GET: Index (Rewards Shop)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var prizes = await _unitOfWork.Prizes.GetAllAsync(p => p.UserId == user.Id);

            ViewBag.CurrentPoints = user.CurrentPoints;
            return View(prizes);
        }

        // POST: Create Prize
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Cost,Description")] Prize prize)
        {
            var userId = _userManager.GetUserId(User);
            prize.UserId = userId;

            if (string.IsNullOrWhiteSpace(prize.Title) || prize.Cost < 1)
            {
                TempData["ErrorMessage"] = "Invalid reward details.";
                return RedirectToAction(nameof(Index));
            }

            await _unitOfWork.Prizes.AddAsync(prize);
            await _unitOfWork.SaveAsync();

            TempData["SuccessMessage"] = "Reward created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Redeem Prize
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Redeem(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var prize = await _unitOfWork.Prizes.GetByIdAsync(id);

            if (prize == null || prize.UserId != user.Id)
                return NotFound();

            if (user.CurrentPoints < prize.Cost)
            {
                TempData["ErrorMessage"] = "Not enough points!";
                return RedirectToAction(nameof(Index));
            }

            user.CurrentPoints -= prize.Cost;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = $"Redeemed \"{prize.Title}\"";
            return RedirectToAction(nameof(Index));
        }

        // POST: Edit Prize
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("Id,Title,Cost,Description")] Prize updatedPrize)
        {
            var prize = await _unitOfWork.Prizes.GetByIdAsync(updatedPrize.Id);
            var user = await _userManager.GetUserAsync(User);

            if (prize == null || prize.UserId != user.Id)
                return NotFound();

            if (string.IsNullOrWhiteSpace(updatedPrize.Title) || updatedPrize.Cost < 1)
            {
                TempData["ErrorMessage"] = "Invalid reward details.";
                return RedirectToAction(nameof(Index));
            }

            prize.Title = updatedPrize.Title;
            prize.Cost = updatedPrize.Cost;
            prize.Description = updatedPrize.Description;

            await _unitOfWork.SaveAsync();

            TempData["SuccessMessage"] = "Reward updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Delete Prize
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var prize = await _unitOfWork.Prizes.GetByIdAsync(id);
            var userId = _userManager.GetUserId(User);

            if (prize == null || prize.UserId != userId)
                return NotFound();

            await _unitOfWork.Prizes.DeleteAsync(id);
            await _unitOfWork.SaveAsync();

            TempData["SuccessMessage"] = "Reward deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
