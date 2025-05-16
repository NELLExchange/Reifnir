using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nellebot.NotificationHandlers;

namespace Nellebot.Tests;

[TestClass]
public class SeventeenTests
{
    [TestMethod]
    [DataRow("Lorem ipsum dolor sitt amet, consectetur adipiscing elit")]
    public void TestSeventeen_WhenIsDissallowed(string input)
    {
        Assert.IsTrue(Seventeen.IsDissallowed(input));
    }

    [TestMethod]
    [DataRow("1706")]
    public void TestSeventeen_WhenAllowed(string input)
    {
        Assert.IsFalse(Seventeen.IsDissallowed(input));
    }
}
