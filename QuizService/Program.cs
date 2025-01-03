using DotnetGeminiSDK;
using DotnetGeminiSDK.Client;
using DotnetGeminiSDK.Client.Interfaces;
using Google.Api;
using QuizService.Services;

namespace QuizService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddGrpc();

            // Configure Firebase
            var firebaseProjectId = builder.Configuration["Firebase:ProjectId"] ?? throw new Exception("Firebase ProjectId is required");
            var firebaseCredentialsPath = builder.Configuration["Firebase:CredentialsPath"] ?? throw new Exception("Firebase CredentialsPath is required");
            builder.Services.AddSingleton(new FirebaseService(firebaseProjectId, firebaseCredentialsPath));

            // Configure Gemini
            builder.Services.AddGeminiClient(config =>
            {
                config.ApiKey = builder.Configuration["Gemini:ApiKey"] ?? throw new Exception("Gemini API key is required");
            });

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Cấu hình CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigins", policy =>
                {
                    policy.WithOrigins("https://localhost:7282") // Thay bằng domain frontend của bạn
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // DI
            builder.Services.AddTransient<GeminiService>();
            builder.Services.AddTransient<QuizManagerService>();
            builder.Services.AddTransient<IGeminiClient, GeminiClient>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            // Configure the gRPC endpoints
            app.MapGrpcService<QuizManagerService>();
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

            app.MapControllers();

            app.Run();
        }
    }
}
