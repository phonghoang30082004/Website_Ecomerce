using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

namespace WebAPP.Controllers
{
    public class FirebaseController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public FirebaseController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        public async Task<IActionResult> GetUserInfo(string token)
        {
            try
            {
                // Kiểm tra token Firebase
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                var uid = decodedToken.Uid;
                // Lấy thông tin người dùng hoặc thực hiện các hành động khác
                return Ok(new { uid });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error verifying Firebase token: {ex.Message}");
            }
        }
    }
}
