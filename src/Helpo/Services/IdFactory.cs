using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Helpo.Services;

public class IdFactory
{
    public string GenerateId()
    {
        return Ulid.NewUlid().ToString();
    }
}
