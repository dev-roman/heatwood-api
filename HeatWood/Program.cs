using System.Security.Claims;
using System.Text;
using HeatWood.Database;
using HeatWood.Models;
using HeatWood.Services.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<HeatWoodDbContext>(opts =>
{
    opts.UseSqlServer(builder.Configuration.GetConnectionString("HeatWood"));
});

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<HeatWoodDbContext>();


builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opts =>
    {
        opts.RequireHttpsMetadata = false; // TODO: change to true after HTTPS setup
        opts.SaveToken = true;
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
                    builder.Configuration["JwtBearer:AccessToken:Secret"]
                )),
            ValidateAudience = false,
            ValidateIssuer = false
        };
        opts.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var userMgr = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
                var signInMgr = ctx.HttpContext.RequestServices.GetRequiredService<SignInManager<IdentityUser>>();

                var username = ctx.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                IdentityUser? idUser = await userMgr.FindByIdAsync(username);
                ctx.Principal = await signInMgr.CreateUserPrincipalAsync(idUser);
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.Configure<JwtBearerSettings>(
    builder.Configuration.GetSection("JwtBearer")
);

builder.Services.AddScoped<IAuthManager<IdentityUser>, AuthManager<IdentityUser>>();
builder.Services.AddScoped<IJwtBearerManager>(_ => new JwtBearerManager(builder.Configuration["JwtBearer:Secret"]));
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.Configure<RouteOptions>(opts =>
{
    opts.LowercaseUrls = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}