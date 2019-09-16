using System.Reflection;

namespace Lybecker.K8sFriendlyAspNetCore.Models
{
    public class HealthEndpointResponse
    {
        public HealthEndpointResponse()
        {
            AssemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version.ToString();
        }

        public string Message { get; set; }
        public string AssemblyVersion { get; }
    }
}