using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? ParentId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public int? CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual ICollection<Category> InverseParent { get; set; } = new List<Category>();
    public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
    public virtual Category? Parent { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
