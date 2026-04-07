// ================================================
//  AgroBillling.API / Program.cs
//  ✅ PostgreSQL + Security Hardened
// ================================================

using AgroBilling.API.Services;
using AgroBillling.DAL.Context;
using AgroBillling.DAL.Models;
using AgroBillling.DAL.Repositories;
using AgroBillling.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IO.Compression;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── MEMORY CACHE ────────────────────────────────────────────
builder.Services.AddMemoryCache();

// ─── COMPRESSION ─────────────────────────────────────────────
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes
        .Concat(new[] { "application/json" });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

// ─── DATABASE — PostgreSQL ────────────────────────────────────
builder.Services.AddDbContext<AgroBillingDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.CommandTimeout(30)
    ));

// ─── REPOSITORIES ────────────────────────────────────────────
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IShopRepository, ShopRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IBillRepository, BillRepository>();
builder.Services.AddScoped<IPurchaseRepository, PurchaseRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<ICreditNoteRepository, CreditNoteRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// ─── SERVICES ────────────────────────────────────────────────
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<AgroBilling.API.Services.EmailService>(); // ✅ Email OTP

builder.Services.AddHttpClient();
builder.Services.AddScoped<EmailValidationService>();

// In Program.cs after adding DbContext:
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// ─── JWT AUTH ─────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing from configuration");

// ✅ Security: JWT key must be at least 32 chars
if (jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters long");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role",
            NameClaimType = "sub"
        };

        // ✅ Return proper 401 instead of redirect
        options.Events = new JwtBearerEvents
        {
            OnChallenge = ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync("{\"success\":false,\"message\":\"Unauthorized\"}");
            },
            OnForbidden = ctx =>
            {
                ctx.Response.StatusCode = 403;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync("{\"success\":false,\"message\":\"Forbidden\"}");
            }
        };
    });

builder.Services.AddAuthorization();

// ─── CORS ─────────────────────────────────────────────────────
// ✅ Read allowed origins from config — easy to add production URL
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>()
    ?? new[] { "http://localhost:4200", "https://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ─── CONTROLLERS ─────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        // ✅ camelCase → consistent with Angular
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ─── SWAGGER (Dev only) ───────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AgroBilling API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ─── SECURITY HEADERS ─────────────────────────────────────────
app.Use(async (context, next) =>
{
    // ✅ Prevent clickjacking
    context.Response.Headers["X-Frame-Options"] = "DENY";
    // ✅ Prevent MIME sniffing
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    // ✅ XSS protection
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    // ✅ Referrer policy
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    // ✅ Remove server header
    context.Response.Headers.Remove("Server");
    await next();
});

// ─── PIPELINE ─────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // ✅ Production: HSTS
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseResponseCompression();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ✅ Health check endpoint — for Railway/Render monitoring
app.MapGet("/health", () => Results.Ok(new { status = "healthy", time = DateTime.UtcNow }));

// Dev only redirect
if (app.Environment.IsDevelopment())
    app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();