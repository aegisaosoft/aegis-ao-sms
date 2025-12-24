using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmsService.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// JWT Key decoding (supports both base64 and UTF-8)
var jwtKey = builder.Configuration["Jwt:Key"]!;
byte[] keyBytes;
try
{
    keyBytes = Convert.FromBase64String(jwtKey);
    Console.WriteLine("[SmsService] JWT key loaded from base64");
}
catch
{
    keyBytes = Encoding.UTF8.GetBytes(jwtKey);
    Console.WriteLine("[SmsService] JWT key loaded from UTF-8");
}

var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];
Console.WriteLine($"[SmsService] JWT Issuer: {issuer}");
Console.WriteLine($"[SmsService] JWT Audience: {audience}");
Console.WriteLine($"[SmsService] Auth Server: {builder.Configuration["AuthServer:BaseUrl"]}");

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Services
builder.Services.AddSingleton<ISmsService, AzureSmsService>();
builder.Services.AddHttpClient<IUrlShortenerService, UrlShortenerService>();

// HttpClient for Auth Server
builder.Services.AddHttpClient("AuthServer", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AuthServer:BaseUrl"]!);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SMS Service API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Swagger (enabled for all environments)
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "sms-service" }));

app.Run();
