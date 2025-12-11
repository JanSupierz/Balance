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

        // GET: Index (The Shop)
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
        public async Task<IActionResult> Create(Prize prize)
        {
            var userId = _userManager.GetUserId(User);
            prize.UserId = userId;

            // Simple validation since it's a modal or small form
            if (string.IsNullOrWhiteSpace(prize.Title) || prize.Cost < 1)
            {
                return RedirectToAction(nameof(Index));
            }

            await _unitOfWork.Prizes.AddAsync(prize);
            await _unitOfWork.SaveAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: Redeem (Buy)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Redeem(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var prize = await _unitOfWork.Prizes.GetByIdAsync(id);

            if (prize == null || prize.UserId != user.Id) return NotFound();

            if (user.CurrentPoints >= prize.Cost)
            {
                user.CurrentPoints -= prize.Cost;
                await _userManager.UpdateAsync(user);

                TempData["SuccessMessage"] = $"Redeemed *{prize.Title}*";
            }
            else
            {
                TempData["ErrorMessage"] = "Not enough points!";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Delete Prize
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var prize = await _unitOfWork.Prizes.GetByIdAsync(id);
            if (prize != null && prize.UserId == _userManager.GetUserId(User))
            {
                await _unitOfWork.Prizes.DeleteAsync(id);
                await _unitOfWork.SaveAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}