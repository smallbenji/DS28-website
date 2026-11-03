namespace DS.Application.FormTemplates;

public abstract class FormTemplate
{
    public abstract string Id { get; }
    public abstract List<FormSection> Sections { get; }
}

public class FormSection
{
    public string Title { get; set; }
    public List<FormField> Fields { get; set; }
}

public class FormField
{
    public required string Key { get; set; }
    public required FormFieldType Type { get; set; }
    public required string Label { get; set; }
    public string Placeholder { get; set; }
}

public enum FormFieldType
{
    String,
    Number,
    Date,
}