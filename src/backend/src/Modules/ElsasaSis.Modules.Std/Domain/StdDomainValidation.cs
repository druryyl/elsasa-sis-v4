using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Std.Domain;

internal static class StdDomainValidation
{
    internal static Result ValidatePersonDetails(
        string fullName,
        DateOnly dateOfBirth,
        Gender gender,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result.Failure(StdErrors.FullNameRequired());
        }

        if (dateOfBirth == default || dateOfBirth > DateOnly.FromDateTime(utcNow))
        {
            return Result.Failure(StdErrors.DateOfBirthInvalid());
        }

        if (!Enum.IsDefined(gender))
        {
            return Result.Failure(StdErrors.GenderInvalid(gender.ToString()));
        }

        return Result.Success();
    }

    internal static Result ValidateGuardian(
        string fullName,
        GuardianRelationshipType relationshipType,
        string fullNameField = "fullName",
        string relationshipField = "relationshipType")
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result.Failure(
                fullNameField == "guardians"
                    ? StdErrors.GuardianFullNameRequired()
                    : StdErrors.FullNameRequired(fullNameField));
        }

        if (!Enum.IsDefined(relationshipType))
        {
            return Result.Failure(StdErrors.GuardianRelationshipTypeInvalid(relationshipType.ToString(), relationshipField));
        }

        return Result.Success();
    }

    internal static Result ValidateGuardianList(IReadOnlyList<GuardianInput>? guardians)
    {
        if (guardians is null || guardians.Count == 0)
        {
            return Result.Failure(StdErrors.AtLeastOneGuardianRequired());
        }

        foreach (var guardian in guardians)
        {
            var guardianResult = ValidateGuardian(
                guardian.FullName,
                guardian.RelationshipType,
                fullNameField: "guardians",
                relationshipField: "guardians");

            if (guardianResult.IsFailure)
            {
                return guardianResult;
            }
        }

        return Result.Success();
    }

    internal static Result ValidateAcademicYearReference(
        Guid academicYearId,
        string academicYearName,
        bool isAdmissionYear = false)
    {
        if (academicYearId == Guid.Empty)
        {
            return Result.Failure(isAdmissionYear
                ? StdErrors.AdmissionYearRequired()
                : StdErrors.AcademicYearRequired());
        }

        if (string.IsNullOrWhiteSpace(academicYearName))
        {
            return Result.Failure(isAdmissionYear
                ? StdErrors.AdmissionYearNameRequired()
                : StdErrors.AcademicYearNameRequired());
        }

        return Result.Success();
    }

    internal static Result ValidateSchoolLevelAndGrade(
        SchoolLevel schoolLevel,
        string grade,
        string schoolLevelField = "schoolLevel",
        string gradeField = "grade")
    {
        if (!Enum.IsDefined(schoolLevel))
        {
            return Result.Failure(StdErrors.SchoolLevelInvalid(schoolLevel.ToString(), schoolLevelField));
        }

        if (string.IsNullOrWhiteSpace(grade))
        {
            return Result.Failure(StdErrors.GradeRequired(gradeField));
        }

        return Result.Success();
    }
}
