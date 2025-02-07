using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Nellebot.Data;

public class ProtectedConverter : ValueConverter<ulong, string>
{
    public ProtectedConverter(IDataProtectionProvider provider, string purpose)
        : this(new Wrapper(provider, purpose))
    { }

    private ProtectedConverter(Wrapper wrapper)
        : base(wrapper.To, wrapper.From)
    { }

    private class Wrapper
    {
        private readonly IDataProtector _dataProtector;

        public Wrapper(IDataProtectionProvider dataProtectionProvider, string purpose)
        {
            _dataProtector = dataProtectionProvider.CreateProtector(purpose);
        }

        public Expression<Func<ulong, string>> To => x => _dataProtector.Protect(x.ToString());

        public Expression<Func<string, ulong>> From => x => ulong.Parse(_dataProtector.Unprotect(x));
    }
}
