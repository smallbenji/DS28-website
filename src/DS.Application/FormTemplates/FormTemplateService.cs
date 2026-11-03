namespace DS.Application.FormTemplates;

public class FormTemplateService(IEnumerable<FormTemplate> templates)
{
    private readonly Dictionary<string, FormTemplate> templatesById = templates.ToDictionary(template => template.Id, StringComparer.Ordinal);

    public FormTemplate GetTemplate(string id)
    {
        return templatesById.GetValueOrDefault(id);
    }
}
