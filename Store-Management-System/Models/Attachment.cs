using System;
using System.Collections.Generic;

namespace Store_Management_System.Models;

public partial class Attachment
{
    public int Id { get; set; }

    public string EntityType { get; set; } = null!;

    public int EntityId { get; set; }

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public long FileSize { get; set; }

    public string ContentType { get; set; } = null!;

    public DateTime UploadedAt { get; set; }

    public int UploadedBy { get; set; }
}
