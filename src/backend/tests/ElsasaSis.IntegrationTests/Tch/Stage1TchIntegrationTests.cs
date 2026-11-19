using System.Net;
using System.Net.Http.Json;
using Dapper;
using ElsasaSis.Api.Features.Academic.GetSubjects;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using ElsasaSis.Modules.Tch.Features.Assignments;
using ElsasaSis.Modules.Tch.Features.Assignments.AssignTeacher;
using ElsasaSis.Modules.Tch.Features.Assignments.GetTeacherWorkload;
using ElsasaSis.Modules.Tch.Features.Teachers;
using ElsasaSis.Modules.Tch.Features.Teachers.AddTeachingCapability;
using ElsasaSis.Modules.Tch.Features.Teachers.CreateTeacher;
using ElsasaSis.Modules.Tch.Features.Teachers.UpdateTeacher;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.IntegrationTests.Tch;

public sealed class Stage1TchIntegrationTests : IntegrationTestBase
{
    public Stage1TchIntegrationTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedStage1FixturesAsync();
    }

    [Fact]
    public async Task AcademicSubjects_ReturnsAllTenSeededSubjects()
    {
        using var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.GetAsync("/api/acd/subjects");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var subjects = await response.Content.ReadFromJsonAsync<IReadOnlyList<SubjectResponse>>(JsonOptions);
        Assert.NotNull(subjects);
        Assert.Equal(10, subjects!.Count);

        var subjectNames = subjects.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Matematika", subjectNames);
        Assert.Contains("Bahasa Indonesia", subjectNames);
        Assert.Contains("Ilmu Pengetahuan Alam", subjectNames);
        Assert.Contains("Ilmu Pengetahuan Sosial", subjectNames);
        Assert.Contains("Bahasa Inggris", subjectNames);
    }

    [Fact]
    public async Task Teacher_Lifecycle_Capabilities_Assignments_And_PrincipalWorkload()
    {
        using var adminClient = await CreateAuthenticatedAdminClientAsync();

        // 1. Create teacher validation: BR-TCH-002 missing phone & email returns 422
        var invalidContactReq = new CreateTeacherRequest(
            "Dewi Sartika",
            "Dewi",
            "199001012015012001",
            "3201010101900001",
            new DateOnly(1990, 1, 1),
            "FEMALE",
            Phone: null,
            Email: null,
            "Jl. Merdeka No. 1");

        var invalidContactResp = await adminClient.PostAsJsonAsync("/api/tch/teachers", invalidContactReq);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidContactResp.StatusCode);

        // Valid teacher creation (201 Created)
        var validTeacherReq = invalidContactReq with
        {
            Phone = "081234567800",
            Email = "dewi@example.com"
        };
        var createResp = await adminClient.PostAsJsonAsync("/api/tch/teachers", validTeacherReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var teacher = await createResp.Content.ReadFromJsonAsync<TeacherDto>(JsonOptions);
        Assert.NotNull(teacher);
        Assert.Equal("Dewi Sartika", teacher!.FullName);
        Assert.Equal("ACTIVE", teacher.Status);

        // Duplicate NIP returns 409 Conflict
        var dupNipReq = validTeacherReq with
        {
            FullName = "Dewi Duplicate NIP",
            Nik = "3201010101900099"
        };
        var dupNipResp = await adminClient.PostAsJsonAsync("/api/tch/teachers", dupNipReq);
        Assert.Equal(HttpStatusCode.Conflict, dupNipResp.StatusCode);

        // Duplicate NIK returns 409 Conflict
        var dupNikReq = validTeacherReq with
        {
            FullName = "Dewi Duplicate NIK",
            Nip = "199001012015012099"
        };
        var dupNikResp = await adminClient.PostAsJsonAsync("/api/tch/teachers", dupNikReq);
        Assert.Equal(HttpStatusCode.Conflict, dupNikResp.StatusCode);

        // Update teacher (200 OK)
        var updateTeacherReq = new UpdateTeacherRequest(
            "Dewi Sartika M.Pd",
            "Dewi",
            "199001012015012001",
            "3201010101900001",
            new DateOnly(1990, 1, 1),
            "FEMALE",
            "081234567800",
            "dewi.sartika@example.com",
            "Jl. Merdeka No. 1 Bandung");

        var updateResp = await adminClient.PutAsJsonAsync($"/api/tch/teachers/{teacher.Id}", updateTeacherReq);
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        var updatedTeacher = await updateResp.Content.ReadFromJsonAsync<TeacherDto>(JsonOptions);
        Assert.NotNull(updatedTeacher);
        Assert.Equal("Dewi Sartika M.Pd", updatedTeacher!.FullName);

        // 2. Capabilities: Add / Remove / Duplicate (BR-TCH-006)
        var subjectsResp = await adminClient.GetAsync("/api/acd/subjects");
        var subjects = await subjectsResp.Content.ReadFromJsonAsync<IReadOnlyList<SubjectResponse>>(JsonOptions);
        Assert.NotNull(subjects);
        var mathSubject = subjects!.First(s => s.Name.Contains("Matematika", StringComparison.OrdinalIgnoreCase));
        var ipaSubject = subjects.First(s => s.Name.Contains("Alam", StringComparison.OrdinalIgnoreCase));

        // Add Matematika capability (201 Created)
        var addCapResp = await adminClient.PostAsJsonAsync(
            $"/api/tch/teachers/{teacher.Id}/capabilities",
            new AddTeachingCapabilityRequest(mathSubject.Id));
        Assert.Equal(HttpStatusCode.Created, addCapResp.StatusCode);

        // Duplicate capability returns 409 Conflict (BR-TCH-006)
        var dupCapResp = await adminClient.PostAsJsonAsync(
            $"/api/tch/teachers/{teacher.Id}/capabilities",
            new AddTeachingCapabilityRequest(mathSubject.Id));
        Assert.Equal(HttpStatusCode.Conflict, dupCapResp.StatusCode);

        // Remove capability (204 NoContent)
        var removeCapResp = await adminClient.DeleteAsync(
            $"/api/tch/teachers/{teacher.Id}/capabilities/{mathSubject.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removeCapResp.StatusCode);

        // Re-add Matematika capability for assignment testing
        var readdCapResp = await adminClient.PostAsJsonAsync(
            $"/api/tch/teachers/{teacher.Id}/capabilities",
            new AddTeachingCapabilityRequest(mathSubject.Id));
        Assert.Equal(HttpStatusCode.Created, readdCapResp.StatusCode);

        // 3. Teacher Assignment
        Guid operationalPeriodId;
        Guid studyGroup7AId;
        await using (var connection = new SqlConnection(Factory.ConnectionString))
        {
            operationalPeriodId = await connection.ExecuteScalarAsync<Guid>(
                "SELECT TOP (1) [id] FROM [opr].[operational_periods] WHERE [status] = 'ACTIVE' ORDER BY [start_date] ASC;");
            studyGroup7AId = await connection.ExecuteScalarAsync<Guid>(
                "SELECT TOP (1) [id] FROM [std].[study_groups] WHERE [name] = '7A';");
        }

        // BR-TCH-005: Reject assignment when teacher lacks capability for subject (409 Conflict)
        var noCapAssignReq = new AssignTeacherRequest(
            teacher.Id,
            operationalPeriodId,
            ipaSubject.Id,
            studyGroup7AId);
        var noCapAssignResp = await adminClient.PostAsJsonAsync("/api/tch/assignments", noCapAssignReq);
        Assert.Equal(HttpStatusCode.Conflict, noCapAssignResp.StatusCode);

        // Succeeds when active and capable (201 Created)
        var validAssignReq = new AssignTeacherRequest(
            teacher.Id,
            operationalPeriodId,
            mathSubject.Id,
            studyGroup7AId);
        var assignResp = await adminClient.PostAsJsonAsync("/api/tch/assignments", validAssignReq);
        Assert.Equal(HttpStatusCode.Created, assignResp.StatusCode);
        var assignment = await assignResp.Content.ReadFromJsonAsync<TeacherAssignmentDto>(JsonOptions);
        Assert.NotNull(assignment);
        Assert.Equal("ASSIGNED", assignment!.Status);
        Assert.Equal(mathSubject.Name, assignment.SubjectName);

        // BR-TCH-004: Reject active duplicate assignment tuple (409 Conflict)
        var dupAssignResp = await adminClient.PostAsJsonAsync("/api/tch/assignments", validAssignReq);
        Assert.Equal(HttpStatusCode.Conflict, dupAssignResp.StatusCode);

        // Remove assignment (204 NoContent) -> transitions ASSIGNED to REMOVED
        var removeAssignResp = await adminClient.DeleteAsync($"/api/tch/assignments/{assignment.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removeAssignResp.StatusCode);

        // Cleanly reassigns same tuple -> reactivates existing assignment to ASSIGNED
        var reassignResp = await adminClient.PostAsJsonAsync("/api/tch/assignments", validAssignReq);
        Assert.Equal(HttpStatusCode.Created, reassignResp.StatusCode);
        var reassigned = await reassignResp.Content.ReadFromJsonAsync<TeacherAssignmentDto>(JsonOptions);
        Assert.NotNull(reassigned);
        Assert.Equal("ASSIGNED", reassigned!.Status);
        Assert.Equal(assignment.Id, reassigned.Id);

        // BR-TCH-003: Reject assignment for inactive teacher (409 Conflict)
        var inactiveTeacherReq = new CreateTeacherRequest(
            "Guru Nonaktif",
            "Nonaktif",
            "198001012005011005",
            "3201010101800005",
            new DateOnly(1980, 1, 1),
            "MALE",
            "081299990000",
            "guru.nonaktif@example.com",
            "Alamat");
        var createInactiveResp = await adminClient.PostAsJsonAsync("/api/tch/teachers", inactiveTeacherReq);
        var inactiveTeacher = await createInactiveResp.Content.ReadFromJsonAsync<TeacherDto>(JsonOptions);
        Assert.NotNull(inactiveTeacher);

        // Add capability first
        await adminClient.PostAsJsonAsync(
            $"/api/tch/teachers/{inactiveTeacher!.Id}/capabilities",
            new AddTeachingCapabilityRequest(mathSubject.Id));

        // Deactivate teacher
        var deactResp = await adminClient.PostAsync($"/api/tch/teachers/{inactiveTeacher.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactResp.StatusCode);

        // Try assigning inactive teacher -> 409 Conflict
        var inactiveAssignReq = new AssignTeacherRequest(
            inactiveTeacher.Id,
            operationalPeriodId,
            mathSubject.Id,
            studyGroup7AId);
        var inactiveAssignResp = await adminClient.PostAsJsonAsync("/api/tch/assignments", inactiveAssignReq);
        Assert.Equal(HttpStatusCode.Conflict, inactiveAssignResp.StatusCode);

        // 4. BR-TCH-007: Deactivating teacher preserves existing assignments
        var deactActiveTeacherResp = await adminClient.PostAsync($"/api/tch/teachers/{teacher.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactActiveTeacherResp.StatusCode);

        // Verify assignment record in database remains 'ASSIGNED'
        await using (var connection = new SqlConnection(Factory.ConnectionString))
        {
            var assignmentStatus = await connection.ExecuteScalarAsync<string>(
                "SELECT [status] FROM [tch].[teacher_assignments] WHERE [id] = @Id;",
                new { assignment.Id });
            Assert.Equal("ASSIGNED", assignmentStatus);
        }

        // 5. Principal calls GET /api/tch/workload (200 OK) and verifies workload oversight
        using var principalClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultPrincipalUsername,
            Stage1Seeder.DefaultPrincipalPassword);

        var workloadResp = await principalClient.GetAsync($"/api/tch/workload?operationalPeriodId={operationalPeriodId}");
        Assert.Equal(HttpStatusCode.OK, workloadResp.StatusCode);
        var workload = await workloadResp.Content.ReadFromJsonAsync<TeacherWorkloadResponse>(JsonOptions);
        Assert.NotNull(workload);
        Assert.True(workload!.TotalAssignments >= 1);
        Assert.Contains(workload.Teachers, t => t.TeacherId == teacher.Id);
        var teacherWorkload = workload.Teachers.First(t => t.TeacherId == teacher.Id);
        Assert.Equal(1, teacherWorkload.TotalAssignments);
        Assert.Contains(teacherWorkload.StudyGroups, sg => sg.StudyGroupId == studyGroup7AId);
        Assert.Contains(teacherWorkload.Subjects, s => s.SubjectId == mathSubject.Id);
    }
}
