using CompraVendas.Data;
using CompraVendas.Infra;
using CompraVendas.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CompraVendasDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=compravendas.db"));

builder.Services.AddScoped<FornecedorService>();
builder.Services.AddScoped<CompraService>();
builder.Services.AddScoped<VendaService>();
builder.Services.AddScoped<MovimentacaoService>();

builder.Services.AddHttpClient<IEstoqueClient, EstoqueClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Servicos:Estoque"] ?? "http://localhost:5002/");
    c.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddExceptionHandler<TratadorDeExcecoes>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // Trocar por migrations (dotnet ef migrations add Inicial) quando o schema estabilizar.
    scope.ServiceProvider.GetRequiredService<CompraVendasDbContext>().Database.EnsureCreated();
}

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
