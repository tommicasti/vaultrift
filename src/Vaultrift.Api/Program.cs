using System.IO.Compression;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.ResponseCompression;
using Vaultrift.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddOpenApi();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

builder.Services.AddGameStorage(builder.Configuration, builder.Environment);
builder.Services.AddAccounts(builder.Configuration, builder.Environment);
builder.Services.AddRealtime(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<IncompatibleSaveHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHttpsRedirection();
}

app.UseGameClient();
app.UseAccounts();
app.UseAuthorization();
app.MapControllers();
app.MapRealtime();
app.MapGameClientFallback();

app.Run();
