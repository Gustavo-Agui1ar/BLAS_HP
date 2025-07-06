var builder = WebApplication.CreateBuilder(args);

var nomeDaPoliticaCORS = "AllowWebAppPolicy";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: nomeDaPoliticaCORS,
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// ----------------------------------------------------

builder.Services.AddSingleton<IResourceThrottleService, ResourceThrottleService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(nomeDaPoliticaCORS);

//app.UseAuthorization(); // Esta linha pode continuar comentada por enquanto

app.MapControllers();

app.Run();