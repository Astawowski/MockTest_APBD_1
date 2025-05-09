namespace MockTest.Models;

public class StudentWithGroupsGetDTO
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Age { get; set; }
    public List<GroupGetDTO>? Groups { get; set; }
}