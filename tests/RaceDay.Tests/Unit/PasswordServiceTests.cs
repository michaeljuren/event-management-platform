using Microsoft.AspNetCore.Identity;
using RaceDay.Api.Data.Seed;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Tests.Unit;

public class PasswordServiceTests
{
    private readonly PasswordService _service = new(new PasswordHasher<User>());

    [Fact]
    public void Hash_IsNotPlainText_AndIsSaltedPerCall()
    {
        var user = new User();

        var first = _service.Hash(user, "Str0ngPassw0rd");
        var second = _service.Hash(user, "Str0ngPassw0rd");

        Assert.NotEqual("Str0ngPassw0rd", first);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_AcceptsCorrectPassword_AndRejectsWrongOne()
    {
        var user = new User();
        user.PasswordHash = _service.Hash(user, "Str0ngPassw0rd");

        Assert.True(_service.Verify(user, "Str0ngPassw0rd"));
        Assert.False(_service.Verify(user, "str0ngpassw0rd"));
    }

    [Fact]
    public void SeededHash_VerifiesWithDemoPassword()
    {
        var user = new User { PasswordHash = SeedData.DemoPasswordHash };

        Assert.True(_service.Verify(user, SeedData.DemoPassword));
    }
}
