using System.ComponentModel.DataAnnotations;

namespace MockTest.Models;

public class StudentWithGroupsCreateDTO
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string FirstName { get; set; }
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string LastName { get; set; }
    public int Age { get; set; }
    public List<int>? Groups { get; set; }
}