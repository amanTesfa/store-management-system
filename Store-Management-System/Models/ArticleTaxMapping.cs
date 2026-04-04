using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class ArticleTaxMapping
{
    public int Id { get; set; }

    public int ArticleId { get; set; }

    public int TaxGroupId { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public virtual Article Article { get; set; } = null!;

    public virtual TaxGroup TaxGroup { get; set; } = null!;
}
