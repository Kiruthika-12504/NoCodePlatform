using Microsoft.AspNetCore.Mvc;
using Supabase;
using WorkflowBackend.DTOs;
using WorkflowBackend.Models;
using WorkflowBackend.Repositories;
using System.Text.Json;
// using WorkflowAutomation.Models;

// Aliases
using SupabaseClient = Supabase.Client;
using GoTrueClient = Supabase.Gotrue.Client;

var builder = WebApplication.CreateBuilder(args);
// =======================
// CORS
// =======================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin(); // for development only
    });
});

// =======================
// Swagger / OpenAPI
// =======================
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =======================
// Supabase Setup
// =======================
var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseKey = builder.Configuration["Supabase:Key"];

builder.Services.AddSingleton(provider =>
{
    var options = new Supabase.SupabaseOptions
    {
        AutoConnectRealtime = true
    };

    var client = new SupabaseClient(supabaseUrl!, supabaseKey!, options);
    client.InitializeAsync().Wait();

    return client;
});

// GoTrue Auth
builder.Services.AddSingleton(provider =>
{
    var authOptions = new Supabase.Gotrue.ClientOptions
    {
        Url = supabaseUrl!.TrimEnd('/') + "/auth/v1"
    };

    return new GoTrueClient(authOptions);
});

// =======================
// Repositories
// =======================
builder.Services.AddScoped<WorkflowRepository>();
builder.Services.AddScoped<ActivityRepository>();
builder.Services.AddScoped<WorkflowLogRepository>();
builder.Services.AddScoped<NotificationRepository>();
builder.Services.AddScoped<FileRepository>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();

// =======================
// Workflow Endpoints
// =======================

// CREATE workflow
// CREATE workflow
app.MapPost("/workflow/create", async (
    CreateWorkflowRequest req,
    WorkflowRepository repo) =>
{
    var workflow = new Workflow
    {
        Id = Guid.NewGuid(),
        Name = req.Name,
        Status = req.Status,
        WorkflowJson = req.WorkflowJson,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    var result = await repo.CreateWorkflow(workflow);

    return Results.Ok(new
    {
        id = result.Id,
        name = result.Name,
        status = result.Status,
        workflowJson = result.WorkflowJson,
        createdAt = result.CreatedAt,
        updatedAt = result.UpdatedAt
    });
})
.WithTags("Workflow");

// GET all workflows
app.MapGet("/workflow/all", async (WorkflowRepository repo) =>
{
    var result = await repo.GetAllWorkflows();
    return Results.Ok(result.Select(w => new
    {
        id = w.Id,
        name = w.Name,
        status = w.Status,
        workflowJson = w.WorkflowJson,
        createdAt = w.CreatedAt,
        updatedAt = w.UpdatedAt
    }));
})
.WithTags("Workflow");


// UPDATE workflow status
app.MapPut("/workflow/status", async (
    UpdateWorkflowStatusRequest req,
    WorkflowRepository workflowRepo,
    WorkflowLogRepository logRepo) =>
{
    // 1. Update workflow
    var updated = await workflowRepo.UpdateStatus(req.WorkflowId, req.Status);

    // 2. Create log
    await logRepo.CreateLog(new WorkflowLog
    {
        WorkflowId = req.WorkflowId,
        ActivityId = null, // no activity for status change
        Status = req.Status,
        Message = $"Workflow status changed to {req.Status}"
    });

    // 3. Return updated workflow
    return Results.Ok(new
{
    id = updated.Id,
    name = updated.Name,
    status = updated.Status,
    workflowJson = updated.WorkflowJson,
    createdAt = updated.CreatedAt,
    updatedAt = updated.UpdatedAt
});

})
.WithTags("Workflow");

// CREATE activity
app.MapPost("/activity/create", async (
    CreateActivityRequest req,
    ActivityRepository activityRepo,
    WorkflowLogRepository logRepo) =>
{
    var activity = new Activity
    {
        WorkflowId = req.WorkflowId,
        Type = req.Type,
        Status = req.Status,
        Parameters = req.Parameters,
        Order = req.Order
    };

    var result = await activityRepo.CreateActivity(activity);

    // ✅ create log
    await logRepo.CreateLog(new WorkflowLog
    {
        WorkflowId = req.WorkflowId,
        ActivityId = result.Id,
        Status = result.Status,
        Message = $"Activity '{result.Type}' created"
    });

    return Results.Ok(new
{
    id = result.Id,
    workflowId = result.WorkflowId,
    type = result.Type,
    status = result.Status,
    parameters = result.Parameters,
    order = result.Order,
    startedAt = result.StartedAt,
    endedAt = result.EndedAt
});

})
.WithTags("Activity");


// UPDATE activity status
app.MapPut("/activity/status", async (
    Guid activityId,
    string status,
    ActivityRepository activityRepo,
    WorkflowLogRepository logRepo) =>
{
    var updated = await activityRepo.UpdateStatus(activityId, status);

    await logRepo.CreateLog(new WorkflowLog
    {
        WorkflowId = updated.WorkflowId,
        ActivityId = updated.Id,
        Status = status,
        Message = $"Activity '{updated.Type}' changed status to {status}"
    });

    return Results.Ok(new {
        updated.Id,
        updated.WorkflowId,
        updated.Type,
        updated.Status,
        updated.Parameters,
        updated.Order,
        updated.StartedAt,
        updated.EndedAt
    });
})
.WithTags("Activity");

app.MapPost("/file/upload", async ([FromForm] FileUploadRequest req, FileRepository fileRepo) =>
{
    if (req.File == null)
        return Results.BadRequest("No file uploaded");

    if (req.ActivityId == Guid.Empty)
        return Results.BadRequest("activityId is required");

    var fileUrl = await fileRepo.UploadAndAttachToActivity(
        req.File,
        "papers",
        req.ActivityId
    );

    return Results.Ok(new { fileUrl });
})
.DisableAntiforgery()
.Accepts<FileUploadRequest>("multipart/form-data")
.WithTags("Files");

app.MapGet("/file/download", async (Guid activityId, FileRepository fileRepo) =>
{
    var signedUrl = await fileRepo.GetSignedUrlByActivity(activityId, 60); // await here

    if (string.IsNullOrEmpty(signedUrl))
        return Results.NotFound("No file found for this activity.");

    return Results.Ok(new { signedUrl });
})
.WithTags("Files");

app.Run();

// =======================
// WeatherForecast model
// =======================
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
