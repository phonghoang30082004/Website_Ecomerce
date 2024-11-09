using System.ComponentModel.DataAnnotations;

namespace WebAPP.Models
{
    public class SupplierDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tên cửa hàng.")]
        public string StoreName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
        public string Address { get; set; }

    }
}
