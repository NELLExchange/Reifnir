using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Scriban;

namespace Nellebot.Services;

public class ScribanTemplateLoader
{
    private readonly Dictionary<string, Template> _templateCache = new();

    public async Task<Template> LoadTemplate(string templateName)
    {
        if (_templateCache.TryGetValue(templateName, out Template? loadTemplate))
            return loadTemplate;

        string templateString = await File.ReadAllTextAsync($"Resources/ScribanTemplates/{templateName}.sbntxt");

        Template? template = Template.Parse(templateString);

#if !DEBUG
        _templateCache.Add(templateName, template);
#endif

        return template;
    }
}
