using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebAPP.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        // UserId là khóa ngoại tham chiếu đến AppUserModel
        public string UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên cửa hàng")]
        public string StoreName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        public string? Address { get; set; }

        // Tham chiếu đến AppUserModel
        [ForeignKey("UserId")]
        public virtual AppUserModel User { get; set; }
    }
}
