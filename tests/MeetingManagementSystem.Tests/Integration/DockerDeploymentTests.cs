using Xunit;
using System.Diagnostics;

namespace MeetingManagementSystem.Tests.Integration
{
    public class DockerDeploymentTests : IDisposable
    {
        private readonly string _composeFile = "docker-compose.prod.yml";
        private bool _servicesStarted = false;

        [Fact]
        public async Task DockerCompose_ShouldBuildSuccessfully()
        {
            // Arrange
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker-compose",
                    Arguments = $"-f {_composeFile} build",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            // Act
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Assert
            Assert.Equal(0, process.ExitCode);
            Assert.DoesNotContain("ERROR", output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DockerCompose_ShouldStartAllServices()
        {
            // Arrange
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker-compose",
                    Arguments = $"-f {_composeFile} up -d",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            // Act
            process.Start();
            await process.WaitForExitAsync();
            _servicesStarted = true;

            // Wait for services to be ready
            await Task.Delay(TimeSpan.FromSeconds(30));

            // Check service status
            var psProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker-compose",
                    Arguments = $"-f {_composeFile} ps",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            psProcess.Start();
            var output = await psProcess.StandardOutput.ReadToEndAsync();
            await psProcess.WaitForExitAsync();

            // Assert
            Assert.Contains("postgres", output);
            Assert.Contains("app", output);
            Assert.Contains("Up", output);
        }

        [Fact]
        public async Task PostgresContainer_ShouldBeHealthy()
        {
            // Arrange
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "inspect --format='{{.State.Health.Status}}' meetingmanagement_postgres_prod",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            // Act
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Assert
            Assert.Contains("healthy", output.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AppContainer_ShouldBeHealthy()
        {
            // Arrange
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "inspect --format='{{.State.Health.Status}}' meetingmanagement_app_prod",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            // Act
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Assert
            Assert.Contains("healthy", output.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AppContainer_ShouldExposeCorrectPort()
        {
            // Arrange
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "port meetingmanagement_app_prod 5000",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            // Act
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Assert
            Assert.Contains("5000", output);
        }

        [Fact]
        public async Task Volumes_ShouldBeCreated()
        {
            // Arrange
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "volume ls",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            // Act
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Assert
            Assert.Contains("postgres_data", output);
            Assert.Contains("app_logs", output);
            Assert.Contains("app_uploads", output);
        }

        [Fact]
        public async Task Network_ShouldBeCreated()
        {
            // Arrange
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "network ls",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            // Act
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Assert
            Assert.Contains("meeting-network", output);
        }

        public void Dispose()
        {
            if (_servicesStarted)
            {
                // Cleanup: Stop services
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker-compose",
                        Arguments = $"-f {_composeFile} down",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
            }
        }
    }
}
