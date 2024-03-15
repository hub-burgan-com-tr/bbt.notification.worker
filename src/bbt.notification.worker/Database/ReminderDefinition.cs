using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ReminderDefinition
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string DefinitionCode { get; set; }
    public string Title { get; set; }
    public string Language { get; set; }
}