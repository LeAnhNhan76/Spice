using Spice.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Spice.Models
{
    [Table(Constant.Table_Categories)]
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Category Name")]
        [Required]
        public string Name { get; set; }
    }
}