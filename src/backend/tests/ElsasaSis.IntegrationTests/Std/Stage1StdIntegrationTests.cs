using System.Net;
using System.Net.Http.Json;
using Dapper;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using ElsasaSis.Modules.Std.Features.Admissions;
using ElsasaSis.Modules.Std.Features.Admissions.EnrollCandidate;
using ElsasaSis.Modules.Std.Features.Admissions.RecordAdmissionDecision;
using ElsasaSis.Modules.Std.Features.Admissions.RegisterCandidate;
using ElsasaSis.Modules.Std.Features.Placements.PlaceStudent;
using ElsasaSis.Modules.Std.Features.Students;
using ElsasaSis.Modules.Std.Features.Students.UpdateStudent;
using ElsasaSis.Modules.Std.Features.StudyGroups;
using ElsasaSis.Modules.Std.Features.StudyGroups.CreateStudyGroup;
using ElsasaSis.Modules.Std.Features.StudyGroups.UpdateStudyGroup;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.IntegrationTests.Std;

public sealed class Stage1StdIntegrationTests : IntegrationTestBase
{
    public Stage1StdIntegrationTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedStage1FixturesAsync();
    }

    private async Task<Guid> GetActiveAcademicYearIdAsync()
    {
        await using var connection = new SqlConnection(Factory.ConnectionString);
        return await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [opr].[academic_years] WHERE [status] = 'ACTIVE' ORDER BY [created_at] ASC;");
    }

    [Fact]
    public async Task Candidate_Admissions_Atomic_Enrollment_And_StudyGroup_HomeroomScoping_Lifecycle()
    {
        using var adminClient = await CreateAuthenticatedAdminClientAsync();
        var academicYearId = await GetActiveAcademicYearIdAsync();

        // 1. Candidate Registration (201 Created)
        var regRequest = new RegisterCandidateRequest(
            academicYearId,
            "Ahmad Dahlan",
            "Ahmad",
            new DateOnly(2012, 5, 10),
            "MALE",
            "081298765432",
            "ahmad@example.com",
            "Jl. Sudirman No. 10",
            "SMP",
            "7",
            [
                new RegisterCandidateGuardianRequest(
                    "Dahlan Senior",
                    "FATHER",
                    "081211112222",
                    "dahlan.sr@example.com",
                    "Jl. Sudirman No. 10")
            ]);

        var regResp = await adminClient.PostAsJsonAsync("/api/std/candidates", regRequest);
        Assert.Equal(HttpStatusCode.Created, regResp.StatusCode);
        var candidate = await regResp.Content.ReadFromJsonAsync<CandidateDto>(JsonOptions);
        Assert.NotNull(candidate);
        Assert.Equal("REGISTERED", candidate!.AdmissionStatus);
        Assert.Single(candidate.Guardians);
        Assert.Equal("Dahlan Senior", candidate.Guardians[0].FullName);

        // 2. Principal approves candidate (ACCEPTED with decision_notes)
        using var principalClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultPrincipalUsername,
            Stage1Seeder.DefaultPrincipalPassword);

        var decisionReq = new RecordAdmissionDecisionRequest("ACCEPTED", "Memenuhi seluruh kriteria penerimaan");
        var decisionResp = await principalClient.PostAsJsonAsync($"/api/std/candidates/{candidate.Id}/decision", decisionReq);
        Assert.Equal(HttpStatusCode.OK, decisionResp.StatusCode);
        var decidedCandidate = await decisionResp.Content.ReadFromJsonAsync<CandidateDto>(JsonOptions);
        Assert.NotNull(decidedCandidate);
        Assert.Equal("ACCEPTED", decidedCandidate!.AdmissionStatus);
        Assert.Equal("Memenuhi seluruh kriteria penerimaan", decidedCandidate.DecisionNotes);

        // 3. GET /api/std/candidates/{id}/next-nis returns suggested NIS ending in 0001
        var nextNisResp = await adminClient.GetAsync($"/api/std/candidates/{candidate.Id}/next-nis");
        Assert.Equal(HttpStatusCode.OK, nextNisResp.StatusCode);
        var nextNis = await nextNisResp.Content.ReadFromJsonAsync<NextNisResponse>(JsonOptions);
        Assert.NotNull(nextNis);
        Assert.EndsWith("0001", nextNis!.SuggestedNis);

        // 4. Admin enrolls candidate atomically
        var enrollReq = new EnrollCandidateRequest(nextNis.SuggestedNis, Nisn: null, EnrollmentDate: new DateOnly(2026, 7, 15));
        var enrollResp = await adminClient.PostAsJsonAsync($"/api/std/candidates/{candidate.Id}/enroll", enrollReq);
        Assert.Equal(HttpStatusCode.Created, enrollResp.StatusCode);
        var enrolledStudent = await enrollResp.Content.ReadFromJsonAsync<EnrolledStudentResponse>(JsonOptions);
        Assert.NotNull(enrolledStudent);
        Assert.Equal(nextNis.SuggestedNis, enrolledStudent!.Nis);
        Assert.Equal("ACTIVE", enrolledStudent.Status);
        Assert.Single(enrolledStudent.Guardians);
        Assert.Equal("Dahlan Senior", enrolledStudent.Guardians[0].FullName);
        Assert.Equal("081211112222", enrolledStudent.Guardians[0].Phone);

        // Verify database persistence and candidate status transition
        await using (var connection = new SqlConnection(Factory.ConnectionString))
        {
            var studentRow = await connection.QuerySingleAsync<(string Nis, string Status)>(
                "SELECT nis, status FROM std.students WHERE id = @Id;",
                new { enrolledStudent.Id });
            Assert.Equal(nextNis.SuggestedNis, studentRow.Nis);
            Assert.Equal("ACTIVE", studentRow.Status);

            var guardianCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM std.student_guardians WHERE student_id = @Id;",
                new { enrolledStudent.Id });
            Assert.Equal(1, guardianCount);

            var candidateRow = await connection.QuerySingleAsync<(string AdmissionStatus, Guid? EnrolledStudentId)>(
                "SELECT admission_status, enrolled_student_id FROM std.candidates WHERE id = @Id;",
                new { candidate.Id });
            Assert.Equal("ENROLLED", candidateRow.AdmissionStatus);
            Assert.Equal(enrolledStudent.Id, candidateRow.EnrolledStudentId);
        }

        // Verify duplicate NIS returns 409 Conflict
        var secondCandReg = await adminClient.PostAsJsonAsync("/api/std/candidates", regRequest with
        {
            FullName = "Siti Aisyah",
            Guardians = [new RegisterCandidateGuardianRequest("Aisyah Mother", "MOTHER", "081233334444")]
        });
        Assert.Equal(HttpStatusCode.Created, secondCandReg.StatusCode);
        var secondCand = await secondCandReg.Content.ReadFromJsonAsync<CandidateDto>(JsonOptions);
        Assert.NotNull(secondCand);

        var secondApprove = await principalClient.PostAsJsonAsync(
            $"/api/std/candidates/{secondCand!.Id}/decision",
            new RecordAdmissionDecisionRequest("ACCEPTED", "Lulus seleksi"));
        Assert.Equal(HttpStatusCode.OK, secondApprove.StatusCode);

        var dupNisResp = await adminClient.PostAsJsonAsync(
            $"/api/std/candidates/{secondCand.Id}/enroll",
            new EnrollCandidateRequest(nextNis.SuggestedNis, Nisn: null, EnrollmentDate: new DateOnly(2026, 7, 15)));
        Assert.Equal(HttpStatusCode.Conflict, dupNisResp.StatusCode);

        // 5. Update student & guardians (BR-STU-003 and duplicate NISN)
        // Update student with NISN
        var updateStudentReq = new UpdateStudentRequest(
            "0012345678",
            "Ahmad Dahlan Updated",
            "Ahmad",
            new DateOnly(2012, 5, 10),
            "MALE",
            "081298765432",
            "ahmad.updated@example.com",
            "Jl. Sudirman No. 10 Updated");

        var updateStudentResp = await adminClient.PutAsJsonAsync($"/api/std/students/{enrolledStudent.Id}", updateStudentReq);
        Assert.Equal(HttpStatusCode.OK, updateStudentResp.StatusCode);
        var updatedStudent = await updateStudentResp.Content.ReadFromJsonAsync<StudentDto>(JsonOptions);
        Assert.NotNull(updatedStudent);
        Assert.Equal("0012345678", updatedStudent!.Nisn);
        Assert.Equal("Ahmad Dahlan Updated", updatedStudent.FullName);

        // Enroll second candidate with distinct NIS and try setting duplicate NISN -> returns 409
        var enrollSecondResp = await adminClient.PostAsJsonAsync(
            $"/api/std/candidates/{secondCand.Id}/enroll",
            new EnrollCandidateRequest("20260002", Nisn: null, EnrollmentDate: new DateOnly(2026, 7, 15)));
        Assert.Equal(HttpStatusCode.Created, enrollSecondResp.StatusCode);
        var secondEnrolled = await enrollSecondResp.Content.ReadFromJsonAsync<EnrolledStudentResponse>(JsonOptions);
        Assert.NotNull(secondEnrolled);

        var dupNisnResp = await adminClient.PutAsJsonAsync(
            $"/api/std/students/{secondEnrolled!.Id}",
            updateStudentReq with { FullName = "Siti Aisyah" });
        Assert.Equal(HttpStatusCode.Conflict, dupNisnResp.StatusCode);

        // BR-STU-003: Deleting the last guardian returns 409 Conflict
        var lastGuardianId = enrolledStudent.Guardians[0].Id;
        var deleteGuardianResp = await adminClient.DeleteAsync(
            $"/api/std/students/{enrolledStudent.Id}/guardians/{lastGuardianId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteGuardianResp.StatusCode);

        // 6. Study Group and Student Placement
        Guid studyGroup7AId;
        Guid studyGroup7BId;
        await using (var connection = new SqlConnection(Factory.ConnectionString))
        {
            studyGroup7AId = await connection.ExecuteScalarAsync<Guid>(
                "SELECT [id] FROM [std].[study_groups] WHERE [name] = '7A' AND [academic_year_id] = @AcademicYearId;",
                new { academicYearId });
            studyGroup7BId = await connection.ExecuteScalarAsync<Guid>(
                "SELECT [id] FROM [std].[study_groups] WHERE [name] = '7B' AND [academic_year_id] = @AcademicYearId;",
                new { academicYearId });
        }

        // BR-STU-008: Placement mismatch rejection (study group 7A is SMP grade 7, mismatching grade "8" returns 409)
        var mismatchPlacementReq = new PlaceStudentFlatRequest(
            enrolledStudent.Id,
            academicYearId,
            "SMP",
            "8",
            studyGroup7AId);

        var mismatchResp = await adminClient.PostAsJsonAsync("/api/std/placements", mismatchPlacementReq);
        Assert.Equal(HttpStatusCode.Conflict, mismatchResp.StatusCode);

        // Valid placement into 7A
        var validPlacementReq = new PlaceStudentFlatRequest(
            enrolledStudent.Id,
            academicYearId,
            "SMP",
            "7",
            studyGroup7AId);

        var placementResp = await adminClient.PostAsJsonAsync("/api/std/placements", validPlacementReq);
        Assert.Equal(HttpStatusCode.OK, placementResp.StatusCode);

        // BR-STU-004: Upsert behavior - placing again in same academic year updates existing placement
        var upsertResp = await adminClient.PostAsJsonAsync("/api/std/placements", validPlacementReq);
        Assert.Equal(HttpStatusCode.OK, upsertResp.StatusCode);

        // 7. Homeroom Isolation: teacher.budi gets 200 for 7A and 403 Forbidden for 7B
        using var teacherClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultTeacherUsername,
            Stage1Seeder.DefaultTeacherPassword);

        // 7A roster: teacher.budi is the homeroom teacher -> 200 OK with roster and guardian contacts
        var roster7AResp = await teacherClient.GetAsync($"/api/std/study-groups/{studyGroup7AId}/roster");
        Assert.Equal(HttpStatusCode.OK, roster7AResp.StatusCode);
        var roster7A = await roster7AResp.Content.ReadFromJsonAsync<StudyGroupRosterDto>(JsonOptions);
        Assert.NotNull(roster7A);
        Assert.Equal("7A", roster7A!.Name);
        Assert.Contains(roster7A.Students, s => s.Id == enrolledStudent.Id);
        var rosterStudent = roster7A.Students.First(s => s.Id == enrolledStudent.Id);
        Assert.Contains(rosterStudent.Guardians, g => g.Phone == "081211112222");

        // 7B roster: teacher.budi is NOT the homeroom teacher -> 403 Forbidden
        var roster7BResp = await teacherClient.GetAsync($"/api/std/study-groups/{studyGroup7BId}/roster");
        Assert.Equal(HttpStatusCode.Forbidden, roster7BResp.StatusCode);
    }
}
