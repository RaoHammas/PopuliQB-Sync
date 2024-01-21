using System.Text.Json.Serialization;

namespace PopuliQB_Tool.BusinessObjects;

public class PopPerson
{
    [JsonPropertyName("object?")] public string? Object { get; set; }


    [JsonPropertyName("id")] public int? Id { get; set; }


    [JsonPropertyName("first_name")] public string? FirstName { get; set; }


    [JsonPropertyName("last_name")] public string? LastName { get; set; }


    [JsonPropertyName("middle_name")] public string? MiddleName { get; set; }


    [JsonPropertyName("prefix")] public string? Prefix { get; set; }


    [JsonPropertyName("suffix")] public string? Suffix { get; set; }


    [JsonPropertyName("preferred_name")] public string? PreferredName { get; set; }


    [JsonPropertyName("display_name")] public string? DisplayName { get; set; }


    [JsonPropertyName("gender")] public string? Gender { get; set; }


    [JsonPropertyName("other_gender")] public string? OtherGender { get; set; }


    [JsonPropertyName("raceid")] public int? Raceid { get; set; }


    [JsonPropertyName("image_file_id")] public object? ImageFileId { get; set; }


    [JsonPropertyName("birth_date")] public string? BirthDate { get; set; }


    [JsonPropertyName("status")] public string? Status { get; set; }


    [JsonPropertyName("social_security_number")]
    public string? SocialSecurityNumber { get; set; }


    [JsonPropertyName("alien_registration_number")]
    public string? AlienRegistrationNumber { get; set; }


    [JsonPropertyName("social_insurance_number")]
    public string? SocialInsuranceNumber { get; set; }


    [JsonPropertyName("home_city")] public string? HomeCity { get; set; }


    [JsonPropertyName("home_state")] public string? HomeState { get; set; }


    [JsonPropertyName("home_country")] public string? HomeCountry { get; set; }


    [JsonPropertyName("former_name")] public string? FormerName { get; set; }


    [JsonPropertyName("license_plate")] public string? LicensePlate { get; set; }


    [JsonPropertyName("bio")] public string? Bio { get; set; }


    [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }


    [JsonPropertyName("hispanic_latino")] public bool? HispanicLatino { get; set; }


    [JsonPropertyName("resident_alien")] public bool? ResidentAlien { get; set; }


    [JsonPropertyName("localization_id")] public int? LocalizationId { get; set; }


    [JsonPropertyName("notification_email_id")]
    public int? NotificationEmailId { get; set; }


    [JsonPropertyName("deceased_date")] public DateTime? DeceasedDate { get; set; }


    [JsonPropertyName("added_by_id")] public int? AddedById { get; set; }


    [JsonPropertyName("added_at")] public DateTime? AddedAt { get; set; }


    [JsonPropertyName("import_id")] public int? ImportId { get; set; }


    [JsonPropertyName("private_profile")] public bool? PrivateProfile { get; set; }


    [JsonPropertyName("is_user")] public bool? IsUser { get; set; }


    [JsonPropertyName("addresses")] public List<PopAddress>? Addresses { get; set; }


    [JsonPropertyName("phone_numbers")] public List<PopPhoneNumber>? PhoneNumbers { get; set; }
    // [JsonPropertyName("email_addresses")] public List<PopEmailAddress> EmailAddresses { get; set; }


    [JsonPropertyName("student")] public PopStudent? PopStudent { get; set; }

    [JsonPropertyName("report_data")] public PersonReportData? ReportData { get; set; }
    [JsonPropertyName("student_degrees")] public List<PersonDegree>? Degrees { get; set; }
}

public class PopStudent
{
    [JsonPropertyName("object?")] public string? Object { get; set; }


    [JsonPropertyName("id")] public int? Id { get; set; }


    [JsonPropertyName("visible_student_id")]
    public string? VisibleStudentId { get; set; }


    [JsonPropertyName("entrance_term_id")] public int? EntranceTermId { get; set; }


    [JsonPropertyName("first_time")] public object? FirstTime { get; set; }


    [JsonPropertyName("last_academic_term_id")]
    public int? LastAcademicTermId { get; set; }


    [JsonPropertyName("exit_date")] public DateTime? ExitDate { get; set; }


    [JsonPropertyName("exit_reason_id")] public int? ExitReasonId { get; set; }


    [JsonPropertyName("loa_start_date")] public DateTime? LoaStartDate { get; set; }


    [JsonPropertyName("loa_end_date")] public DateTime? LoaEndDate { get; set; }


    [JsonPropertyName("proctored")] public bool? Proctored { get; set; }


    [JsonPropertyName("max_enrolled_credits")]
    public object? MaxEnrolledCredits { get; set; }


    [JsonPropertyName("max_enrolled_hours")]
    public object? MaxEnrolledHours { get; set; }


    [JsonPropertyName("max_audit_credits")]
    public object? MaxAuditCredits { get; set; }


    [JsonPropertyName("max_audit_hours")] public object? MaxAuditHours { get; set; }


    [JsonPropertyName("student_type_campus")]
    public string? StudentTypeCampus { get; set; }


    [JsonPropertyName("student_type_online")]
    public string? StudentTypeOnline { get; set; }
}

public class PopAddress
{
    [JsonPropertyName("object?")] public string? Object { get; set; }


    [JsonPropertyName("id")] public int? Id { get; set; }


    [JsonPropertyName("owner_id")] public int? OwnerId { get; set; }


    [JsonPropertyName("owner_type")] public string? OwnerType { get; set; }


    [JsonPropertyName("street")] public string? Street { get; set; }


    [JsonPropertyName("city")] public string? City { get; set; }


    [JsonPropertyName("state")] public string? State { get; set; }


    [JsonPropertyName("postal")] public string? Postal { get; set; }


    [JsonPropertyName("country")] public string? Country { get; set; }


    [JsonPropertyName("type")] public string? Type { get; set; }


    [JsonPropertyName("primary")] public bool? Primary { get; set; }


    [JsonPropertyName("old")] public bool? Old { get; set; }


    [JsonPropertyName("public")] public bool? Public { get; set; }


    [JsonPropertyName("synced_from")] public int? SyncedFrom { get; set; }


    [JsonPropertyName("added_by_id")] public int? AddedById { get; set; }


    [JsonPropertyName("added_at")] public DateTime? AddedAt { get; set; }


    [JsonPropertyName("import_id")] public int? ImportId { get; set; }
}

public class PopPhoneNumber
{
    [JsonPropertyName("object?")] public string? Object { get; set; }


    [JsonPropertyName("id")] public int? Id { get; set; }


    [JsonPropertyName("owner_id")] public int? OwnerId { get; set; }


    [JsonPropertyName("owner_type")] public string? OwnerType { get; set; }


    [JsonPropertyName("number")] public string? Number { get; set; }


    [JsonPropertyName("type")] public string? Type { get; set; }


    [JsonPropertyName("primary")] public bool? Primary { get; set; }


    [JsonPropertyName("old")] public bool? Old { get; set; }


    [JsonPropertyName("public")] public bool? Public { get; set; }


    [JsonPropertyName("synced_from")] public int? SyncedFrom { get; set; }


    [JsonPropertyName("ext")] public string? Ext { get; set; }


    [JsonPropertyName("import_id")] public int? ImportId { get; set; }


    [JsonPropertyName("added_at")] public DateTime? AddedAt { get; set; }


    [JsonPropertyName("added_by_id")] public int? AddedById { get; set; }
}

public class PopEmailAddress
{
    [JsonPropertyName("object?")] public string? Object { get; set; }


    [JsonPropertyName("id")] public int? Id { get; set; }


    [JsonPropertyName("email")] public string? Email { get; set; }


    [JsonPropertyName("owner_type")] public string? OwnerType { get; set; }


    [JsonPropertyName("owner_id")] public int? OwnerId { get; set; }


    [JsonPropertyName("type")] public string? Type { get; set; }


    [JsonPropertyName("primary")] public bool? Primary { get; set; }


    [JsonPropertyName("old")] public bool? Old { get; set; }


    [JsonPropertyName("system")] public bool? System { get; set; }


    [JsonPropertyName("public")] public bool? Public { get; set; }


    [JsonPropertyName("synced_from")] public int? SyncedFrom { get; set; }


    [JsonPropertyName("delivery_problem")] public object? DeliveryProblem { get; set; }


    [JsonPropertyName("delivery_problem_at")]
    public DateTime? DeliveryProblemAt { get; set; }


    [JsonPropertyName("delivery_problem_info")]
    public string? DeliveryProblemInfo { get; set; }


    [JsonPropertyName("verified_at")] public DateTime? VerifiedAt { get; set; }


    [JsonPropertyName("import_id")] public int? ImportId { get; set; }


    [JsonPropertyName("added_at")] public DateTime? AddedAt { get; set; }


    [JsonPropertyName("added_by_id")] public int? AddedById { get; set; }
}

public class PersonReportData
{
    [JsonPropertyName("person_id")] public int PersonId { get; set; }

    [JsonPropertyName("active_roles")] public object? ActiveRoles { get; set; }

    [JsonPropertyName("username")] public string? Username { get; set; }

    [JsonPropertyName("primary_address_street")]
    public string? PrimaryAddressStreet { get; set; }

    [JsonPropertyName("primary_address_city")]
    public string? PrimaryAddressCity { get; set; }

    [JsonPropertyName("primary_address_state")]
    public string? PrimaryAddressState { get; set; }

    [JsonPropertyName("primary_address_country")]
    public string? PrimaryAddressCountry { get; set; }

    [JsonPropertyName("is_alum")] public int IsAlum { get; set; }

    [JsonPropertyName("primary_org_title")]
    public string? PrimaryOrgTitle { get; set; }

    [JsonPropertyName("primary_org_name")] public string? PrimaryOrgName { get; set; }

    [JsonPropertyName("contact_primary_email")]
    public string? ContactPrimaryEmail { get; set; }

    [JsonPropertyName("contact_primary_phone")]
    public string? ContactPrimaryPhone { get; set; }

    [JsonPropertyName("visible_student_id")]
    public object? VisibleStudentId { get; set; }
}

public class PersonDegree
{
    [JsonPropertyName("object")] public string? Object { get; set; }

    [JsonPropertyName("id")] public int? Id { get; set; }

    [JsonPropertyName("student_id")] public int? StudentId { get; set; }

    [JsonPropertyName("degree_id")] public int? DegreeId { get; set; }

    [JsonPropertyName("status")] public string? Status { get; set; }

    [JsonPropertyName("graduation_date")] public object? GraduationDate { get; set; }

    [JsonPropertyName("active_date")] public string? ActiveDate { get; set; }

    [JsonPropertyName("inactive_date")] public object? InactiveDate { get; set; }

    [JsonPropertyName("catalog_year_id")] public int? CatalogYearId { get; set; }

    [JsonPropertyName("anticipated_completion_date")]
    public string? AnticipatedCompletionDate { get; set; }

    [JsonPropertyName("show_on_transcript")]
    public int? ShowOnTranscript { get; set; }
}