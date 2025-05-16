using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nellebot.NotificationHandlers;

namespace Nellebot.Tests;

[TestClass]
public class SeventeenTests
{
    [TestMethod]
    [DataRow("1705")]
    [DataRow("a !1q_=7bba0AAQ       5   qqq ")]
    [DataRow("syttendemai")]
    [DataRow("seventeenohfive")]
    [DataRow("17o5")]
    public void TestSeventeen_WhenMatching(string input)
    {
        Assert.IsTrue(Seventeen.IsMatch(input));
    }

    [TestMethod]
    [DataRow("5071")]
    [DataRow("Lorem ipsum dolor sitt amet, consectetur adipiscing elit")]
    public void TestSeventeen_WhenNotMatching(string input)
    {
        Assert.IsFalse(Seventeen.IsMatch(input));
    }
}
