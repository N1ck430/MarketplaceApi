using DataLibrary.EntityFramework;
using DataLibrary.Models.Application;
using DataLibrary.Models.User;
using DataLibrary.Services.User;
using MarketplaceApi.Authorization;
using MarketplaceApi.Services.Cache;
using MarketplaceApi.Services.Image;
using MarketplaceApi.Services.Mail;
using MarketplaceApi.Services.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using DataLibrary.Services.Software;
using MarketplaceApi.Services.Software;
using DataLibrary.Services.DateTime;
using MarketplaceApi.Services.Background;

namespace MarketplaceApi
{
    public class Program
    {
        private const string AllowAppOrigins = "_allowAppOrigins";
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers(options =>
                {
                    options.Filters.Add<LockedInAuthorizationFilter>();
                    options.Filters.Add<SubscriptionStatusAuthorizationFilter>();
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                var securitySchema = new OpenApiSecurityScheme
                {
                    Description = "Using the Authorization header with the Bearer scheme.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                };

                options.AddSecurityDefinition("Bearer", securitySchema);

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { securitySchema, new[] { "Bearer" } }
                });
            });


            builder.Services.AddAuthentication().AddBearerToken(IdentityConstants.BearerScheme);
            builder.Services.AddAuthorization(options =>
            {
                Enum.GetNames(typeof(Role))
                    .ToList()
                    .ForEach(role =>
                        options.AddPolicy(role, policy =>
                            policy.RequireRole(role)));
            });

            var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
#if DEBUG
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            }
#endif
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("ConnectionString not set");
            }

            builder.Services.AddDbContext<MarketplaceDbContext>(x =>
                x.UseSqlServer(connectionString,
                    options => options.EnableRetryOnFailure(4)));

            builder.Services.AddIdentityCore<User>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<MarketplaceDbContext>()
                .AddApiEndpoints();

            builder.Services.Configure<IdentityOptions>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            });

            // app.MapIdentityApi<>()

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(AllowAppOrigins,
                    b =>
                        b.WithOrigins(builder.Configuration.GetSection("ApplicationSettings:ClientOrigins").Value!)
                            .AllowAnyHeader().AllowAnyMethod());
            });

            builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection("ApplicationSettings"));

            builder.Services.AddHostedService<CheckSubscriptionsBackgroundService>();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<IUserDataService, UserDataService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IMailService, MailService>();
            builder.Services.AddScoped<ICacheService, CacheService>();
            builder.Services.AddScoped<IImageService, ImageService>();
            builder.Services.AddScoped<ISoftwareDataService, SoftwareDataService>();
            builder.Services.AddScoped<ISoftwareService, SoftwareService>();
            builder.Services.AddScoped<IDateTimeProvider, DateTimeProvider>();

            await IdentitySetup(builder);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger(c => c.SerializeAsV2 = false);
                app.UseSwaggerUI();
            }

            app.UseCors(AllowAppOrigins);

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            await app.RunAsync();
        }

        private static async Task IdentitySetup(WebApplicationBuilder builder)
        {
            var serviceProvider = builder.Services.BuildServiceProvider();

            await InitializeDatabase(serviceProvider);

            using var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            using var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            var userService = serviceProvider.GetRequiredService<IUserService>();

            foreach (var role in Enum.GetNames(typeof(Role)))
            {
                if (await roleManager.RoleExistsAsync(role))
                {
                    continue;
                }

                await roleManager.CreateAsync(new IdentityRole(role));
            }

            var userName = builder.Configuration.GetSection("ApplicationSettings:AdminUser:Username").Value!;

            var powerUser = new User
            {
                UserName = userName,
                Email = builder.Configuration.GetSection("ApplicationSettings:AdminUser:Email").Value!,
                RegisterDate = DateTime.UtcNow,
                LastLoginDate = DateTime.UtcNow,
                UserSequenceId = 1,
                ProfilePicture = userService.GenerateProfilePicture(userName)
            };

            if (await userManager.FindByEmailAsync(powerUser.Email) != null)
            {
                return;
            }

            var password = builder.Configuration.GetSection("ApplicationSettings:AdminUser:Password").Value!;
            var result = await userManager.CreateAsync(powerUser, password);
            if (!result.Succeeded)
            {
                return;
            }

            var confirmEmailToken = await userManager.GenerateEmailConfirmationTokenAsync(powerUser);
            await userManager.ConfirmEmailAsync(powerUser, confirmEmailToken);
            await userManager.AddToRoleAsync(powerUser, Role.Admin.ToString());
            await userManager.AddToRoleAsync(powerUser, Role.User.ToString());
        }

        private static async Task InitializeDatabase(ServiceProvider serviceProvider)
        {
            var dbContext = serviceProvider.GetRequiredService<MarketplaceDbContext>();
            await dbContext.Database.MigrateAsync();
        }
    }
}