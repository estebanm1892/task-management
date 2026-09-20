using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Middleware;
using TaskManagement.Core.Tasks;
using TaskManagement.Core.Users;
using TaskManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<TaskManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TaskManagement")
        ?? @"Server=.\SQLEXPRESS;Database=TaskManagement;Integrated Security=True;TrustServerCertificate=True"));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TaskService>();

var app = builder.Build();

app.UseMiddleware<ProblemDetailsMiddleware>();
app.MapControllers();

app.Run();

public partial class Program;
