using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;
using MultiAI.Models.Entities;
using MultiAI.Services;
using MultiAI.Services.Interface;

namespace MultiAI.Config
{
    public static class DependencyConfig
    {
        public static IServiceCollection RegisterAppServices (this IServiceCollection services,IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            services.AddIdentityCore<Users>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddApiEndpoints(); 

            //services.AddAuthentication(IdentityConstants.BearerScheme)
            //    .AddBearerToken(IdentityConstants.BearerScheme);

            services.AddScoped<IJwtService, JwtService>();

            return services;
        }
    }
}
