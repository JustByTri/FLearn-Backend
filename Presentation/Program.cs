using BLL;
using BLL.Background;
using BLL.Hubs;
using BLL.Settings;
using Common.Authorization;
using Hangfire;
using Hangfire.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Presentation.Filter;
using Presentation.Middlewares;
using System.Net;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHangfire(config =>
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseStorage(new MySqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"),
              new MySqlStorageOptions
              {
                  TablesPrefix = "Hangfire_",
                  TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
                  QueuePollInterval = TimeSpan.FromSeconds(15),
                  JobExpirationCheckInterval = TimeSpan.FromHours(1),
                  CountersAggregateInterval = TimeSpan.FromMinutes(5),
                  PrepareSchemaIfNecessary = true,
              }))
);

builder.Services.AddHangfireServer();

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 500_000_000;
    options.MultipartHeadersLengthLimit = int.MaxValue;
    options.MemoryBufferThreshold = int.MaxValue;
});


builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 500_000_000;
});


builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 500_000_000;
});


builder.Services.AddControllers(options =>
{

    options.Filters.Add(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(500_000_000));
});

builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Flearn API",
        Version = "v1",
        Description = "API cho nền tảng học ngôn ngữ Flearn với Voice Assessment",

        Contact = new OpenApiContact
        {
            Name = "Flearn Support",
            Email = "support@flearn.com"
        }
    });


    c.OperationFilter<FileUploadOperationFilter>();

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }


    c.SupportNonNullableReferenceTypes();

    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary",
        Description = "Upload file (multipart/form-data) - Max 500MB for videos"
    });

    c.MapType<IList<IFormFile>>(() => new OpenApiSchema
    {
        Type = "array",
        Items = new OpenApiSchema
        {
            Type = "string",
            Format = "binary",
            Description = "Upload multiple files - Max 500MB per file"
        }
    });

    c.SchemaFilter<FormFileSchemaFilter>();


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


builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));


builder.Services.AddBllServices(builder.Configuration);


try
{
    builder.Services.AddHostedService<TempRegistrationCleanupService>();
}
catch
{

}


var jwtSection = builder.Configuration.GetSection("JwtSettings");
var jwtSettings = jwtSection.Get<JwtSettings>();

if (jwtSettings != null && !string.IsNullOrEmpty(jwtSettings.SecretKey))
{

    if (jwtSettings.SecretKey.Length < 32)
    {
        throw new InvalidOperationException("JWT SecretKey must be at least 32 characters");
    }

    if (jwtSettings.AccessTokenExpirationMinutes <= 0)
    {
        throw new InvalidOperationException("AccessTokenExpirationMinutes must be positive");
    }

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
            ClockSkew = TimeSpan.FromMinutes(5),
            RequireExpirationTime = true,

            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
            NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                logger.LogWarning("🚨 JWT Authentication failed: {Exception} at {Time}",
                    context.Exception.Message, DateTime.UtcNow);

                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                    logger.LogWarning("⏰ Token expired for user at {Time}", DateTime.UtcNow);
                }
                else if (context.Exception.GetType() == typeof(SecurityTokenInvalidIssuerException))
                {
                    context.Response.Headers.Add("Token-Invalid-Issuer", "true");
                    logger.LogError("🔒 Invalid issuer: Expected {Expected}", jwtSettings.Issuer);
                }
                else if (context.Exception.GetType() == typeof(SecurityTokenInvalidAudienceException))
                {
                    context.Response.Headers.Add("Token-Invalid-Audience", "true");
                    logger.LogError("🎯 Invalid audience: Expected {Expected}", jwtSettings.Audience);
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var username = context.Principal?.Identity?.Name;
                var expiry = context.SecurityToken.ValidTo;

                logger.LogInformation("✅ Token validated for user: {Username}, expires: {Expiry}",
                    username, expiry);

                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("🔐 JWT Challenge triggered: {Error} - {Description}",
                    context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("StaffOnly", policy => policy.RequireRole("Staff", "Admin"));
        options.AddPolicy("TeacherOnly", policy => policy.RequireRole("Teacher", "Admin"));
        options.AddPolicy("LearnerOnly", policy => policy.RequireRole("Learner", "Teacher", "Staff", "Admin"));
        options.AddPolicy("OnlyLearner", policy => policy.Requirements.Add(new ExclusiveRoleRequirement("Learner")));
        options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
    });
}
else
{

    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization();
}


var googleAuthSection = builder.Configuration.GetSection("Authentication:Google");
builder.Services.Configure<GoogleAuthSettings>(googleAuthSection);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();

    });
    options.AddPolicy("SignalRCors", policy =>
    {
        policy.WithOrigins(
            "https://f-learn.app",
            "http://localhost:3000",
            "http://10.0.2.2:3000"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));
});

var app = builder.Build();
app.UseForwardedHeaders();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Flearn API V1");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
});

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "FLearn Platform - Hangfire Dashboard",
    Authorization = []
});

app.UseHttpsRedirection();

app.UseCors("AllowAll");
app.UseErrorHandlingMiddleware();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ConversationHub>("/conversationHub").RequireCors("SignalRCors");
app.Run();

