namespace DS;

[AttributeUsage(AttributeTargets.Property)]
public class ColumnAttribute : Attribute
{
    public string Header { get; set; }
    public bool Sortable { get; set; } = true;
    public bool Filterable { get; set; } = true;
    public string Type { get; set; } = "text";

    public ColumnAttribute(string header)
    {
        Header = header;
    }
}