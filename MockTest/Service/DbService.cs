using Microsoft.Data.SqlClient;
using MockTest.Exceptions;
using MockTest.Models;

namespace MockTest.Service;

public interface IDbService
{
    public Task<IEnumerable<StudentWithGroupsGetDTO>> GetStudentsWithGroups(string? studentName);
    public Task<StudentWithGroupsGetDTO> CreateStudentWithGroups(StudentWithGroupsCreateDTO studentWithGroups);
}


public class DbService(IConfiguration config) : IDbService
{
    public async Task<IEnumerable<StudentWithGroupsGetDTO>> GetStudentsWithGroups(string? studentName)
    {
        var result = new Dictionary<int, StudentWithGroupsGetDTO>();
        var connectionString = config.GetConnectionString("Default");
        await using var connection = new SqlConnection(connectionString);

        var sqlCommand = new SqlCommand();
        if (studentName == null)
        {
           sqlCommand = new SqlCommand(@"SELECT s.Id, s.FirstName, s.LastName, s.Age, g.Id, g.Name
                                    FROM Student s LEFT JOIN GroupAssignment ga ON ga.Student_Id = s.Id
                                    LEFT JOIN [Group] g ON ga.Group_Id = g.Id;", connection);
        }
        else
        {
           sqlCommand = new SqlCommand(@"SELECT s.Id, s.FirstName, s.LastName, s.Age, g.Id, g.Name
                                    FROM Student s LEFT JOIN GroupAssignment ga ON ga.Student_Id = s.Id
                                    LEFT JOIN [Group] g ON ga.Group_Id = g.Id WHERE s.FirstName = @studentFirstName;", connection);
           sqlCommand.Parameters.AddWithValue("@studentFirstName", studentName);
        }

        await connection.OpenAsync();
        await using var reader = await sqlCommand.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            int idStudent = reader.GetInt32(0);
            if (!result.ContainsKey(idStudent))
            {
                result.Add(idStudent, new StudentWithGroupsGetDTO
                {
                    Id = idStudent,
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                    Age = reader.GetInt32(3),
                    Groups = new List<GroupGetDTO>()
                });
            }

            int groupId = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);

            if (groupId != 0)
            {
                result[idStudent].Groups.Add(new GroupGetDTO()
                {
                    Id = groupId,
                    Name = reader.GetString(5)
                });
            }
        }

        if (result.Count == 0)
        {
            throw new NotFoundException("No students found");
        }
        return result.Values;
    }


    public async Task<StudentWithGroupsGetDTO> CreateStudentWithGroups(StudentWithGroupsCreateDTO student)
    {
        var connectionString = config.GetConnectionString("Default");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        StudentWithGroupsGetDTO newStudent = new StudentWithGroupsGetDTO();
        
        try
        {
            await using var addStudentCommand = new SqlCommand(@"INSERT INTO Student (FirstName, LastName, Age)
                       VALUES (@FirstName, @LastName, @Age); SELECT scope_identity();", connection, (SqlTransaction)transaction);
            addStudentCommand.Parameters.AddWithValue("@FirstName", student.FirstName);
            addStudentCommand.Parameters.AddWithValue("@LastName", student.LastName);
            addStudentCommand.Parameters.AddWithValue("@Age", student.Age);
            var newStudentId = Convert.ToInt32(await addStudentCommand.ExecuteScalarAsync());

            newStudent = new StudentWithGroupsGetDTO()
            {
                Id = newStudentId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Age = student.Age
            };
            
            if (student.Groups != null)
            {
                foreach (int gId in student.Groups)
                {
                    await using var addStudentToGroupCommand = new SqlCommand(@"INSERT INTO GroupAssignment
                                  (Student_Id, Group_Id) VALUES (@Student_Id, @Group_Id);", connection, (SqlTransaction)transaction);
                    addStudentToGroupCommand.Parameters.AddWithValue("@Student_Id", newStudentId);
                    addStudentToGroupCommand.Parameters.AddWithValue("@Group_Id", gId);
                    await addStudentToGroupCommand.ExecuteNonQueryAsync();
                    
                    await using var getGroupNameCommand = new SqlCommand(@"SELECT Name FROM [Group]
                            WHERE Id = @Group_Id;", connection, (SqlTransaction)transaction);
                    getGroupNameCommand.Parameters.AddWithValue("@Group_Id", gId);
                    var groupName = Convert.ToString(await getGroupNameCommand.ExecuteScalarAsync());
                    
                    if(newStudent.Groups == null) newStudent.Groups = new List<GroupGetDTO>();

                    newStudent.Groups.Add(new GroupGetDTO()
                    {
                        Id = gId,
                        Name = groupName
                    });
                }
            }
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new Exception("An error occurred while creating the student with groups.", ex);
        }
        return newStudent;
    }
}