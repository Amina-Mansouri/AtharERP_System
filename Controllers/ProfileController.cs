using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly FileUploadService _fileUpload;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            FileUploadService fileUpload)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _fileUpload = fileUpload;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.Users.Include(u => u.Department).Include(u => u.JobRankRef).ThenInclude(r => r!.CareerTrack).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return RedirectToAction("Login", "Account");
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePhoto(IFormFile? profilePhoto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (profilePhoto == null || profilePhoto.Length == 0)
            {
                TempData["Error"] = "الرجاء اختيار صورة";
                return RedirectToAction("Index");
            }

            if (!string.IsNullOrEmpty(user.ProfilePhotoPath))
                _fileUpload.DeleteFile(user.ProfilePhotoPath);

            var photoResult = await _fileUpload.SaveFileAsync(profilePhoto, $"users/{user.Id}");
            if (photoResult.Success)
            {
                user.ProfilePhotoPath = photoResult.FilePath;
                await _userManager.UpdateAsync(user);
                TempData["Success"] = "تم تحديث الصورة الشخصية";
            }
            else
            {
                TempData["Error"] = photoResult.ErrorMessage ?? "فشل رفع الصورة";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmPassword)
            {
                TempData["Error"] = "كلمة المرور الجديدة وتأكيدها غير متطابقين";
                return RedirectToAction("Index");
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "تم تغيير كلمة المرور بنجاح";
            }
            else
            {
                TempData["Error"] = string.Join(" — ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("Index");
        }
    }
}