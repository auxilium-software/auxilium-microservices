using AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner.BackgroundServices;
using AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner.Services;
using AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner.Services.Implementations;
using AuxiliumSoftware.AuxiliumServices.Common.Configuration.Sections.Databases;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using AuxiliumSoftware.AuxiliumServices.Common.Messaging;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using AuxiliumSoftware.AuxiliumServices.Common.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Console.WriteLine(@"
                     _ _ _                       _______        _          _____                             
     /\             (_) (_)                     |__   __|      | |        |  __ \                            
    /  \  _   ___  ___| |_ _   _ _ __ ___          | | __ _ ___| | __     | |__) |   _ _ __  _ __   ___ _ __ 
   / /\ \| | | \ \/ / | | | | | | '_ ` _ \         | |/ _` / __| |/ /     |  _  / | | | '_ \| '_ \ / _ \ '__|
  / ____ \ |_| |>  <| | | | |_| | | | | | |        | | (_| \__ \   <      | | \ \ |_| | | | | | | |  __/ |   
 /_/    \_\__,_/_/\_\_|_|_|\__,_|_| |_| |_|        |_|\__,_|___/_|\_\     |_|  \_\__,_|_| |_|_| |_|\___|_|   
");

var builder = Host.CreateApplicationBuilder(args);



// config
var cliConfigPath = args
    .SkipWhile(a => a != "--config-path")
    .Skip(1)
    .FirstOrDefault();

var configPath = cliConfigPath;

builder.Configuration.AddYamlFile(
    configPath,
    optional: false,
    reloadOnChange: true
);



// logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();



// mariadb
var mariaDbHost = builder.Configuration["Databases:MariaDB:Host"] ?? throw new InvalidOperationException("MariaDB Host not found");
var mariaDbPort = builder.Configuration["Databases:MariaDB:Port"] ?? throw new InvalidOperationException("MariaDB Port not found");
var mariaDbUsername = builder.Configuration["Databases:MariaDB:Username"] ?? throw new InvalidOperationException("MariaDB Username not found");
var mariaDbPassword = builder.Configuration["Databases:MariaDB:Password"] ?? throw new InvalidOperationException("MariaDB Password not found");
var mariaDbDatabase = builder.Configuration["Databases:MariaDB:Database"] ?? throw new InvalidOperationException("MariaDB Database not found");

var connectionString =
    $"Server={mariaDbHost};"
    + $"Port={mariaDbPort};"
    + $"Database={mariaDbDatabase};"
    + $"User={mariaDbUsername};"
    + $"Password={mariaDbPassword};"
    + $"CharSet=utf8mb4;"
    + $"Pooling=true;"
    + $"MinimumPoolSize=5;"
    + $"MaximumPoolSize=100;"
    + $"ConnectionLifeTime=300;"
    + $"ConnectionIdleTimeout=180;"
    + $"CancellationTimeout=5;"
    + $"ConnectionReset=false;"
    + $"DefaultCommandTimeout=30;";

builder.Services.AddDbContext<AuxiliumDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null
            );
        }
    );
});



// rabbit-mq
var rabbitConfig = builder.Configuration
    .GetSection("Databases:RabbitMQ")
    .Get<RabbitMQConfigurationSection>()
    ?? throw new InvalidOperationException("RabbitMQ configuration section is missing.");

rabbitConfig.Validate();
builder.Services.AddRabbitMqCore(rabbitConfig);



// common services
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();

// internal services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();



// workers
builder.Services.AddHostedService<NotificationWorker>();



// run
var host = builder.Build();
await host.RunAsync();
