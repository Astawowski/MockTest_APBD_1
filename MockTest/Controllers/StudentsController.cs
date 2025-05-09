using Microsoft.AspNetCore.Mvc;
using MockTest.Exceptions;
using MockTest.Models;
using MockTest.Service;

namespace MockTest.Controllers;



[ApiController]
[Route("api/[controller]")]
public class StudentsController(IDbService dbservice) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetStudentsGroups([FromQuery] string? firstName)
    {
        try
        {
            return Ok(await dbservice.GetStudentsWithGroups(firstName));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddStudentWithGroups([FromBody] StudentWithGroupsCreateDTO studentWithGroups)
    {
        var createdStudent = await dbservice.CreateStudentWithGroups(studentWithGroups);
        return Created($"students/{createdStudent.Id}", createdStudent);
    }
}