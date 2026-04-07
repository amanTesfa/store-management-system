using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Store_Management_System.ViewModels
{
    // For Index/List page with tree view
    public class CategoryIndexViewModel
    {
        public List<CategoryTreeNodeViewModel> Categories { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
    }

    // For tree view nodes
    public class CategoryTreeNodeViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public int ChildCount { get; set; }
        public int ArticleCount { get; set; }
        public List<CategoryTreeNodeViewModel> Children { get; set; } = new();
        public int Level { get; set; }
        public bool HasChildren => Children.Any();
    }

    // For Create/Edit
    public class CategoryCreateViewModel
    {
        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [Display(Name = "Parent Category")]
        public int? ParentId { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
        [NotMapped]
        public SelectList ParentCategories { get; set; }
    }

    public class CategoryEditViewModel : CategoryCreateViewModel
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // For drag-and-drop reorder
    public class CategoryReorderModel
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public int DisplayOrder { get; set; }
    }
}