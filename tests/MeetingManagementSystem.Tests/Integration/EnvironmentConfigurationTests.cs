using Microsoft.Extensions.Configuration;
using Xunit;

namespace MeetingManagementSystem.Tests.Integration
{
    public class EnvironmentConfigurationTests
    {
        [Theory]
        [InlineData("Development")]
        [InlineData("Staging")]
        [InlineData("Production")]
        public void Configuration_ShouldLoadForEnvironment(string environment)
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Act
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var emailSettings = configuration.GetSection("EmailSettings");
            var meetingSettings = configuration.GetSection("MeetingSettings");

            // Assert
            Assert.NotNull(connectionString);
            Assert.NotNull(emailSettings);
            Assert.NotNull(meetingSettings);
        }

        [Fact]
        public void ProductionConfiguration_ShouldHaveSecureSettings()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Production.json", optional: true)
                .Build();

            // Act
            var emailSettings = configuration.GetSection("EmailSettings");
            var enableSsl = emailSettings.GetValue<bool>("EnableSsl");
            var smtpPort = emailSettings.GetValue<int>("SmtpPort");

            // Assert
            Assert.True(enableSsl, "SSL should be enabled in production");
            Assert.True(smtpPort == 587 || smtpPort == 465, "SMTP port should be secure (587 or 465)");
        }

        [Fact]
        public void Configuration_ShouldHaveRequiredEmailSettings()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            // Act
            var emailSettings = configuration.GetSection("EmailSettings");
            var smtpServer = emailSettings.GetValue<string>("SmtpServer");
            var smtpPort = emailSettings.GetValue<int>("SmtpPort");
            var fromAddress = emailSettings.GetValue<string>("FromAddress");

            // Assert
            Assert.NotNull(smtpServer);
            Assert.True(smtpPort > 0);
            Assert.NotNull(fromAddress);
        }

        [Fact]
        public void Configuration_ShouldHaveRequiredMeetingSettings()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            // Act
            var meetingSettings = configuration.GetSection("MeetingSettings");
            var defaultDuration = meetingSettings.GetValue<int>("DefaultMeetingDurationMinutes");
            var maxParticipants = meetingSettings.GetValue<int>("MaxParticipants");
            var maxDocumentSize = meetingSettings.GetValue<int>("MaxDocumentSizeMB");

            // Assert
            Assert.True(defaultDuration > 0);
            Assert.True(maxParticipants > 0);
            Assert.True(maxDocumentSize > 0);
        }

        [Fact]
        public void Configuration_ShouldHaveRequiredDocumentSettings()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            // Act
            var documentSettings = configuration.GetSection("DocumentSettings");
            var uploadPath = documentSettings.GetValue<string>("UploadPath");
            var allowedFileTypes = documentSettings.GetSection("AllowedFileTypes").Get<string[]>();

            // Assert
            Assert.NotNull(uploadPath);
            Assert.NotNull(allowedFileTypes);
            Assert.NotEmpty(allowedFileTypes);
        }

        [Fact]
        public void Configuration_ShouldHaveLoggingSettings()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            // Act
            var loggingSection = configuration.GetSection("Logging");
            var logLevel = loggingSection.GetSection("LogLevel");

            // Assert
            Assert.NotNull(loggingSection);
            Assert.NotNull(logLevel);
        }

        [Fact]
        public void ProductionConfiguration_ShouldHaveSerilogSettings()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Production.json", optional: true)
                .Build();

            // Act
            var serilogSection = configuration.GetSection("Serilog");
            var writeTo = serilogSection.GetSection("WriteTo");

            // Assert
            Assert.NotNull(serilogSection);
            Assert.NotNull(writeTo);
        }

        [Fact]
        public void EnvironmentVariables_ShouldOverrideConfiguration()
        {
            // Arrange
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "TestConnectionString");
            
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            // Act
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Assert
            Assert.Equal("TestConnectionString", connectionString);

            // Cleanup
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
        }

        [Fact]
        public void Configuration_ShouldValidateConnectionString()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            // Act
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Assert
            Assert.NotNull(connectionString);
            Assert.Contains("Host=", connectionString);
            Assert.Contains("Database=", connectionString);
            Assert.Contains("Username=", connectionString);
        }

        [Fact]
        public void ProductionConfiguration_ShouldHaveHealthCheckSettings()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Production.json", optional: true)
                .Build();

            // Act
            var healthChecks = configuration.GetSection("HealthChecks");
            var enabled = healthChecks.GetValue<bool>("Enabled");

            // Assert
            Assert.NotNull(healthChecks);
            Assert.True(enabled);
        }
    }
}
