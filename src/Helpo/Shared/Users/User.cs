using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpo.Data;

namespace Helpo.Shared.Users
{
    public class User : BaseEntity
    {
        public string ExternalId { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? EmailAddress { get; set; }
    }
}