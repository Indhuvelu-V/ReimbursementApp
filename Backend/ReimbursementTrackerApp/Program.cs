using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ReimbursementTrackerApp.Contexts;
using ReimbursementTrackerApp.Data;
using ReimbursementTrackerApp.Filters;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.Enums;
using ReimbursementTrackerApp.Repositories;
using ReimbursementTrackerApp.Services;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddControllers()
//    .AddJsonOptions(options =>
//    {
//        options.JsonSerializerOptions.Converters.Add(
//            new System.Text.Json.Serialization.JsonStringEnumConverter());
//    });


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(c =>

{

    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Reimbursement Tracker API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme

    {

        Name = "Authorization",

        Type = SecuritySchemeType.ApiKey,

        Scheme = "Bearer",

        BearerFormat = "JWT",

        In = ParameterLocation.Header,

        Description = "Enter 'Bearer' followed by your JWT token. Example: Bearer abc123"

    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement

    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

#region Contexts
builder.Services.AddDbContext<ReimbursementContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion
#region CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
#endregion

#region Repository
// 2?? Add Repository
// =============================
//builder.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

builder.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

builder.Services.AddScoped<IExpenseService, ExpenseService>();

builder.Services.AddHttpContextAccessor();

#endregion Repository

#region Services
// =============================
// 3?? Add Services
// =============================
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
builder.Services.AddScoped<INotificationService,NotificationService>();

builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();

#endregion Services
builder.Services.AddScoped<AuditLogActionFilter>();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditLogActionFilter>();
});

// =============================
// 4?? Configure JWT Authentication
// =============================

#region Middlewares
string key = builder.Configuration["Keys:Jwt"] ?? throw new InvalidOperationException("Secret key not found in configuration.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(option =>
    option.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuerSigningKey = true,
        RoleClaimType=ClaimTypes.Role
    }
);
#endregion



var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ReimbursementContext>();
    await CategorySeeder.SeedCategoriesAsync(context);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();


app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.Run();
