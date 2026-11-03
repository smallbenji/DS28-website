namespace DS.Application.FormTemplates;

public class TestFormTemplate : FormTemplate
{
    public override string Id  => "test-form-template";
    public override List<FormSection> Sections => [
        new() {
            Title = "Grundoplysninger",
            Fields = [
                new() {
                    Key = "test-field",
                    Type = FormFieldType.String,
                    Label = "Titel",
                    Placeholder = "Giv dit eksempel en titel"
                }
            ]
        },
        new() {
            Title = "Planlægning",
            Fields = [
                new() {
                    Key = "participant-count",
                    Type = FormFieldType.Number,
                    Label = "Antal deltagere",
                    Placeholder = "Fx 24"
                },
                new() {
                    Key = "date",
                    Type = FormFieldType.Date,
                    Label = "Dato"
                }
            ]
        }
    ];
}
