using BLL;
using BLL.Background;
using BLL.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Flearn API",
        Version = "v1",
        Description = "API cho nền tảng học ngôn ngữ Flearn",
        Contact = new OpenApiContact
        {
            Name = "Flearn Support",
            Email = "support@flearn.com"
        }
    });

    // Add XML comments if available
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Support for nullable reference types
    c.SupportNonNullableReferenceTypes();

    // Simple file upload mapping - this is the key fix
    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });

    c.MapType<IList<IFormFile>>(() => new OpenApiSchema
    {
        Type = "array",
        Items = new OpenApiSchema
        {
            Type = "string",
            Format = "binary"
        }
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"Nhập JWT token (chỉ cần token, không cần 'Bearer ')",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "bearer",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
        return !controllerName?.Equals("Upload", StringComparison.OrdinalIgnoreCase) == true;
    });
});

// Configure JwtSettings from appsettings.json
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Add BLL, DAL, and Email services
builder.Services.AddBllServices(builder.Configuration);

// Add Background Services
try
{
    builder.Services.AddHostedService<TempRegistrationCleanupService>();
}
catch
{
    // Ignore if background service cannot be registered
}

// Configure JWT Authentication
var jwtSection = builder.Configuration.GetSection("JwtSettings");
var jwtSettings = jwtSection.Get<JwtSettings>();

if (jwtSettings != null && !string.IsNullOrEmpty(jwtSettings.SecretKey))
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RequireExpirationTime = true
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });

    // Add Authorization with Policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("StaffOnly", policy => policy.RequireRole("Staff"));
        options.AddPolicy("TeacherOnly", policy => policy.RequireRole("Teacher"));
        options.AddPolicy("LearnerOnly", policy => policy.RequireRole("Learner", "Teacher"));
    });
}
else
{
    // Fallback authentication if JWT is not configured
    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization();
}

// Configure GoogleAuthSettings from appsettings.json
var googleAuthSection = builder.Configuration.GetSection("Authentication:Google");
builder.Services.Configure<GoogleAuthSettings>(googleAuthSection);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Flearn API V1");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
});

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
