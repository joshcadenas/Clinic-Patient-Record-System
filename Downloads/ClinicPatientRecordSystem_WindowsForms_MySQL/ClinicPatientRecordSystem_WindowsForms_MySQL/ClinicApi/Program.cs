using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ClinicDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 36))
    ));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWeb", policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Clinic Patient Record System API",
        Version = "v1",
        Description = "API for web and Windows C# clients. Both clients share this API and MySQL database."
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
    db.Database.EnsureCreated();
    SeedData.EnsureSeeded(db);
}

app.UseCors("AllowWeb");
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapPost("/api/auth/login", async (LoginRequest request, ClinicDbContext db) =>
{
    var account = await db.Accounts.FirstOrDefaultAsync(a => a.Username == request.Username && a.Password == request.Password);
    return account is null
        ? Results.Unauthorized()
        : Results.Ok(new LoginResponse(account.Id, account.Username, account.Role, account.PatientId));
}).WithTags("Authentication");

app.MapGet("/api/patients", async (ClinicDbContext db, string? search) =>
{
    var query = db.Patients.AsQueryable();
    if (!string.IsNullOrWhiteSpace(search))
    {
        var key = search.Trim().ToLower();
        query = query.Where(p => p.Name.ToLower().Contains(key) || p.Contact.ToLower().Contains(key) || p.Address.ToLower().Contains(key));
    }
    return await query.OrderBy(p => p.Name).ToListAsync();
}).WithTags("Patients");

app.MapGet("/api/patients/{id:int}", async (int id, ClinicDbContext db) =>
    await db.Patients.FindAsync(id) is Patient patient ? Results.Ok(patient) : Results.NotFound()).WithTags("Patients");

app.MapPost("/api/patients", async (Patient patient, ClinicDbContext db) =>
{
    patient.Id = 0;
    db.Patients.Add(patient);
    await db.SaveChangesAsync();
    return Results.Created($"/api/patients/{patient.Id}", patient);
}).WithTags("Patients");

app.MapPut("/api/patients/{id:int}", async (int id, Patient input, ClinicDbContext db) =>
{
    var patient = await db.Patients.FindAsync(id);
    if (patient is null) return Results.NotFound();
    patient.Name = input.Name;
    patient.Age = input.Age;
    patient.Birthday = input.Birthday;
    patient.Contact = input.Contact;
    patient.Address = input.Address;
    await db.SaveChangesAsync();
    return Results.Ok(patient);
}).WithTags("Patients");

app.MapDelete("/api/patients/{id:int}", async (int id, ClinicDbContext db) =>
{
    var patient = await db.Patients.FindAsync(id);
    if (patient is null) return Results.NotFound();
    db.Patients.Remove(patient);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithTags("Patients");

app.MapGet("/api/medicalrecords/patient/{patientId:int}", async (int patientId, ClinicDbContext db) =>
    await db.MedicalRecords.Where(m => m.PatientId == patientId).OrderByDescending(m => m.VisitDate).ToListAsync()).WithTags("Medical Records");

app.MapPost("/api/medicalrecords", async (MedicalRecord record, ClinicDbContext db) =>
{
    record.Id = 0;
    db.MedicalRecords.Add(record);
    await db.SaveChangesAsync();
    return Results.Created($"/api/medicalrecords/{record.Id}", record);
}).WithTags("Medical Records");

app.MapGet("/api/doctors", async (ClinicDbContext db) => await db.Doctors.OrderBy(d => d.Name).ToListAsync()).WithTags("Doctors and Staff");
app.MapPost("/api/doctors", async (Doctor doctor, ClinicDbContext db) =>
{
    doctor.Id = 0;
    db.Doctors.Add(doctor);
    await db.SaveChangesAsync();
    return Results.Created($"/api/doctors/{doctor.Id}", doctor);
}).WithTags("Doctors and Staff");
app.MapPut("/api/doctors/{id:int}", async (int id, Doctor input, ClinicDbContext db) =>
{
    var doctor = await db.Doctors.FindAsync(id);
    if (doctor is null) return Results.NotFound();
    doctor.Name = input.Name;
    doctor.Specialization = input.Specialization;
    doctor.AvailableSchedule = input.AvailableSchedule;
    await db.SaveChangesAsync();
    return Results.Ok(doctor);
}).WithTags("Doctors and Staff");
app.MapDelete("/api/doctors/{id:int}", async (int id, ClinicDbContext db) =>
{
    var doctor = await db.Doctors.FindAsync(id);
    if (doctor is null) return Results.NotFound();
    db.Doctors.Remove(doctor);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithTags("Doctors and Staff");

app.MapGet("/api/staff", async (ClinicDbContext db) => await db.StaffAccounts.OrderBy(s => s.FullName).ToListAsync()).WithTags("Doctors and Staff");
app.MapPost("/api/staff", async (StaffAccount staff, ClinicDbContext db) =>
{
    staff.Id = 0;
    db.StaffAccounts.Add(staff);
    await db.SaveChangesAsync();
    return Results.Created($"/api/staff/{staff.Id}", staff);
}).WithTags("Doctors and Staff");

app.MapGet("/api/appointments", async (ClinicDbContext db, int? patientId) =>
{
    var query = db.Appointments.Include(a => a.Patient).Include(a => a.Doctor).AsQueryable();
    if (patientId.HasValue) query = query.Where(a => a.PatientId == patientId.Value);
    return await query.OrderByDescending(a => a.AppointmentDateTime).Select(a => new AppointmentDto(
        a.Id, a.PatientId, a.Patient!.Name, a.DoctorId, a.Doctor!.Name, a.AppointmentDateTime, a.Reason, a.Status
    )).ToListAsync();
}).WithTags("Appointments");

app.MapPost("/api/appointments", async (CreateAppointmentRequest request, ClinicDbContext db) =>
{
    var patientExists = await db.Patients.AnyAsync(p => p.Id == request.PatientId);
    var doctorExists = await db.Doctors.AnyAsync(d => d.Id == request.DoctorId);
    if (!patientExists || !doctorExists) return Results.BadRequest("Invalid patient or doctor.");
    var appointment = new Appointment
    {
        PatientId = request.PatientId,
        DoctorId = request.DoctorId,
        AppointmentDateTime = request.AppointmentDateTime,
        Reason = request.Reason,
        Status = "Pending"
    };
    db.Appointments.Add(appointment);
    await db.SaveChangesAsync();
    return Results.Created($"/api/appointments/{appointment.Id}", appointment);
}).WithTags("Appointments");

app.MapPut("/api/appointments/{id:int}/status", async (int id, UpdateAppointmentStatusRequest request, ClinicDbContext db) =>
{
    var appointment = await db.Appointments.FindAsync(id);
    if (appointment is null) return Results.NotFound();
    appointment.Status = request.Status;
    if (request.NewDateTime.HasValue) appointment.AppointmentDateTime = request.NewDateTime.Value;
    await db.SaveChangesAsync();
    return Results.Ok(appointment);
}).WithTags("Appointments");

app.MapGet("/api/reports/summary", async (ClinicDbContext db) =>
{
    var today = DateTime.Today;
    var tomorrow = today.AddDays(1);

    var dailyPatients = await db.Appointments
        .CountAsync(a => a.AppointmentDateTime >= today && a.AppointmentDateTime < tomorrow);

    var totalPatients = await db.Patients.CountAsync();
    var pendingAppointments = await db.Appointments.CountAsync(a => a.Status == "Pending");
    var approvedAppointments = await db.Appointments.CountAsync(a => a.Status == "Approved");
    var income = approvedAppointments * 500m;

    var diagnoses = await db.MedicalRecords
        .Select(m => m.Diagnosis)
        .ToListAsync();

    var diagnosisSummary = diagnoses
        .Where(d => !string.IsNullOrWhiteSpace(d))
        .GroupBy(d => d)
        .Select(g => new DiagnosisCount(g.Key, g.Count()))
        .OrderByDescending(x => x.Count)
        .ToList();

    return Results.Ok(new ReportSummary(
        dailyPatients,
        totalPatients,
        pendingAppointments,
        approvedAppointments,
        income,
        diagnosisSummary
    ));
}).WithTags("Reports");

app.Run();

