using ZooIds;

namespace Nellebot.Common.Utils;

public class PseudonymGenerator
{
    public static readonly GeneratorConfig PseudonymConfig = new(1, " ", CaseStyle.TitleCase);
    public static readonly GeneratorConfig FriendlyIdConfig = new(2, "-", CaseStyle.LowerCase);

    public static string GetRandomPseudonym()
    {
        var zoo = new ZooIdGenerator(PseudonymConfig);

        return zoo.GenerateId();
    }

    public static string NewFriendlyId()
    {
        var friendly = new ZooIdGenerator(FriendlyIdConfig);

        return friendly.GenerateId();
    }
}
