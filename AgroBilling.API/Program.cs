// ================================================
//  AgroBillling.API / Program.cs
// ================================================

using AgroBillling.DAL.Context;
using AgroBillling.DAL.Repositories;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IO.Compression;
using System.Linq;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json" });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o =>
{
    o.Level = CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(o =>
{
    o.Level = CompressionLevel.Fastest;
});

// ─── DB CONTEXT ───
// Use AddDbContext (not pool) unless AgroBillingDbContext is audited for pool safety; pooled + bad OnConfiguring caused login 500s.
builder.Services.AddDbContext<AgroBillingDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── REPOSITORIES ───
builder.Services.AddScoped<IAuthRepository,         AuthRepository>();
builder.Services.AddScoped<IShopRepository,         ShopRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<ICustomerRepository,     CustomerRepository>();
builder.Services.AddScoped<IProductRepository,      ProductRepository>();
builder.Services.AddScoped<ISupplierRepository,     SupplierRepository>();
builder.Services.AddScoped<IBillRepository,         BillRepository>();
builder.Services.AddScoped<IPurchaseRepository,     PurchaseRepository>();
builder.Services.AddScoped<IExpenseRepository,      ExpenseRepository>();
builder.Services.AddScoped<ICreditNoteRepository,   CreditNoteRepository>();
builder.Services.AddScoped<IReportRepository,       ReportRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// ─── SERVICES (business logic) ───
builder.Services.AddScoped<IBillService,     BillService>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();

// ─── JWT AUTH ───
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ─── CORS ───
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.SetIsOriginAllowed(origin =>
            (origin.StartsWith("http://localhost:") || origin.StartsWith("https://localhost:") ||
             origin.StartsWith("http://127.0.0.1:") || origin.StartsWith("https://127.0.0.1:")))
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ─── CONTROLLERS ───
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Prevent circular reference serialization issues
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// ─── SWAGGER ───
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "AgroBilling API",
        Version = "v1"
    });

    // JWT in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter: Bearer {your token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ─── PIPELINE (routing → CORS → auth → endpoints) ───
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAngular");
app.UseResponseCompression();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Redirect root to Swagger so https://localhost:44307/ doesn't 404
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
