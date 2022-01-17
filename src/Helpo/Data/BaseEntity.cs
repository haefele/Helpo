using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Helpo.Data;

public abstract class BaseEntity
{
    public string Id { get; set; } = string.Empty;
}
