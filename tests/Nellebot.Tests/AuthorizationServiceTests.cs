using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nellebot.Common.AppDiscordModels;
using Nellebot.Services;
using NSubstitute;

namespace Nellebot.Tests;

[TestClass]
public class AuthorizationServiceTests
{
    private IOptions<BotOptions> _optionsMock = null!;
    private AuthorizationService _sut = null!;

    [TestInitialize]
    public void Initialize()
    {
        _sut = null!;
        _optionsMock = Substitute.For<IOptions<BotOptions>>();
    }

    [TestMethod]
    public void IsAdminOrMod_WhenAdmin_ReturnTrue()
    {
        // Arrange
        _optionsMock.Value.Returns(new BotOptions());
        _sut = new AuthorizationService(_optionsMock);

        AppDiscordMember member = BuildAdminMember(1);

        // Act
        bool result = _sut.IsAdminOrMod(member);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsAdminOrMod_WhenMod_ReturnTrue()
    {
        // Arrange
        _optionsMock.Value.Returns(
            new BotOptions
            {
                ModRoleId = 1,
            });

        _sut = new AuthorizationService(_optionsMock);

        // Member with admin role
        AppDiscordMember member = BuildMember(1, 1);

        // Act
        bool result = _sut.IsAdminOrMod(member);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsAdminOrMod_WhenNeitherAdminNorMod_ReturnFalse()
    {
        // Arrange
        _optionsMock.Value.Returns(
            new BotOptions
            {
                ModRoleId = 1,
            });

        _sut = new AuthorizationService(_optionsMock);

        // Member without admin role
        AppDiscordMember member = BuildMember(1, 2);

        // Act
        bool result = _sut.IsAdminOrMod(member);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsTrustedMember_WhenHasTrustedMemberRole_ReturnTrue()
    {
        // Arrange
        _optionsMock.Value.Returns(new BotOptions { TrustedRoleIds = [1] });

        _sut = new AuthorizationService(_optionsMock);

        // Member with trusted role
        AppDiscordMember member = BuildMember(1, 1);

        // Act
        bool result = _sut.IsTrustedMember(member);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsTrustedMember_WhenIsAdmin_ReturnTrue()
    {
        // Arrange
        _optionsMock.Value.Returns(new BotOptions());

        _sut = new AuthorizationService(_optionsMock);

        // Member without trusted role
        AppDiscordMember member = BuildAdminMember(1);

        // Act
        bool result = _sut.IsTrustedMember(member);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsTrustedMember_WhenIsMod_ReturnTrue()
    {
        // Arrange
        _optionsMock.Value.Returns(new BotOptions { ModRoleId = 1 });

        _sut = new AuthorizationService(_optionsMock);

        // Member with trusted role
        AppDiscordMember member = BuildMember(1, 1);

        // Act
        bool result = _sut.IsTrustedMember(member);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsTrustedMember_WhenDoesNotHaveTrustedRole_ReturnFalse()
    {
        // Arrange
        _optionsMock.Value.Returns(new BotOptions { TrustedRoleIds = [1] });

        _sut = new AuthorizationService(_optionsMock);

        // Member with trusted role
        AppDiscordMember member = BuildMember(1, 2);

        // Act
        bool result = _sut.IsTrustedMember(member);

        // Assert
        Assert.IsFalse(result);
    }

    private static AppDiscordMember BuildMember(ulong id, ulong roleId)
    {
        return new AppDiscordMember
        {
            Id = id,
            Roles = new List<AppDiscordRole>
            {
                new()
                {
                    Id = roleId,
                },
            },
        };
    }

    private static AppDiscordMember BuildAdminMember(ulong id)
    {
        return new AppDiscordMember
        {
            Id = id,
            Roles = new List<AppDiscordRole>
            {
                new()
                {
                    Id = 0,
                    HasAdminPermission = true,
                },
            },
        };
    }
}
