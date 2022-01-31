using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpo.Data;

namespace Helpo.Shared.Questions;

public class Question : BaseEntity
{
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;

    

    public string AskedByUser { get; set; } = default!;
}
