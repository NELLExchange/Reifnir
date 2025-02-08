using Microsoft.AspNetCore.DataProtection;

namespace Nellebot.Data;

/// <summary>
///     A mock implementation of <see cref="IDataProtector" /> that does not protect or unprotect data.
/// </summary>
internal class MockProtector : IDataProtector
{
    public IDataProtector CreateProtector(string purpose)
    {
        return new MockProtector();
    }

    public byte[] Protect(byte[] plaintext)
    {
        return plaintext;
    }

    public byte[] Unprotect(byte[] protectedData)
    {
        return protectedData;
    }
}
