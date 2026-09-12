using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallLogManagementSystem.Controllers
{
    // Polled by the header bell dropdown every ~30s (no SignalR/push involved).
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public NotificationController(ApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> Recent()
        {
            var userId = _currentUser.UserId!;

            var unreadCount = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

            var items = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .Take(10)
                .Select(n => new
                {
                    id = n.Id,
                    message = n.Message,
                    callLogId = n.CallLogId,
                    isRead = n.IsRead,
                    createdDate = n.CreatedDate.ToString("dd-MMM hh:mm tt")
                })
                .ToListAsync();

            return Json(new { unreadCount, items });
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _currentUser.UserId!;

            var items = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .Take(100)
                .Select(n => new ViewModels.Notifications.NotificationRowVm
                {
                    Id = n.Id,
                    Message = n.Message,
                    CallLogId = n.CallLogId,
                    CallNumber = n.CallLog!.CallNumber,
                    IsRead = n.IsRead,
                    CreatedDate = n.CreatedDate
                })
                .ToListAsync();

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = _currentUser.UserId!;
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "CallLog", new { id = notification.CallLogId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = _currentUser.UserId!;
            await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            return RedirectToAction(nameof(Index));
        }
    }
}
