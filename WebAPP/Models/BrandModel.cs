﻿using System.ComponentModel.DataAnnotations;

namespace WebAPP.Models
{
	public class BrandModel
	{
		[Key]
		public int Id { get; set; }
		[Required( ErrorMessage = "Yêu cầu nhập ")]
		public string Name { get; set; }
		[Required( ErrorMessage = "Yêu cầu nhập ")]
		public string Description { get; set; }
		public string Slug { get; set; }

		public int Status {  get; set; }

        public string UserId { get; set; }
    }
}
