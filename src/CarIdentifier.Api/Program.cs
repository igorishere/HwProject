using CarIdentifier.Api.Configuration;
using CarIdentifier.Application;
using CarIdentifier.Application.Abstraction.CarIdentification;
using CarIdentifier.Application.Abstraction.PriceFinder;
using CarIdentifier.Application.CommandHandlers.GetPriceSearchJob;
using CarIdentifier.Application.CommandHandlers.IdentifyCar;
using CarIdentifier.Application.Contracts;
using CarIdentifier.Infra;
using CarIdentifier.Infra.PriceSearch;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOptions<OpenAIConfiguration>()
    .Bind(builder.Configuration.GetSection(OpenAIConfiguration.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IChatClient>(service =>
 {
     var openAIConfig = service.GetRequiredService<IOptions<OpenAIConfiguration>>().Value;

#pragma warning disable OPENAI001
     return new OpenAIClient(openAIConfig.ApiKey)
         .GetResponsesClient()
         .AsIChatClient(openAIConfig.Model);
#pragma warning restore OPENAI001
 });
builder.Services.AddScoped<ICarIdentifier, IdentifierService>();
builder.Services.AddScoped<IPriceSearcher, PriceSearcherService>();
builder.Services.AddSingleton<IPriceJobStore, InMemoryPriceJobStore>();
builder.Services.AddSingleton<IPriceJobQueue, ChannelPriceJobQueue>();
builder.Services.AddScoped<ICommandHandler<IdentifyCarCommand, CarResult>, IdentifyCarCommandHandler>();
builder.Services.AddScoped<ICommandHandler<GetPriceSearchJobCommand, CarResult?>, GetPriceSearchJobCommandHandler>();
builder.Services.AddHostedService<PriceSearchWorker>();
builder.Services.AddHostedService<PriceJobCleanupService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
