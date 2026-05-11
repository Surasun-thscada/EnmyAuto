using EnmyAuto.Api.Configuration;
using EnmyAuto.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddAuthServices(builder.Configuration);
builder.Services.AddAiServices(builder.Configuration);
builder.Services.AddMediaServices(builder.Configuration);
builder.Services.AddTikTokServices(builder.Configuration);
builder.Services.AddHangfireJobs(builder.Configuration);
builder.Services.AddRealTime();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapEnmyEndpoints();

app.Run();
