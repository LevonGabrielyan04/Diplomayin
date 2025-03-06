using Diplomayin.Models;
using Newtonsoft.Json;

namespace Diplomayin.Helpers
{
    // ComplianceHelper.cs
    public static class ComplianceHelper
    {
        public static bool CheckCompliance(Device device, Policy policy)
        {
            if (device.Configuration == null)
            {
                return false;
            }
            // Example: Check if device configuration meets policy requirements
            var deviceConfig = JsonConvert.DeserializeObject<Dictionary<string, string>>(device.Configuration);
            var policyRequirements = JsonConvert.DeserializeObject<Dictionary<string, string>>(policy.Requirements);

            foreach (var requirement in policyRequirements)
            {
                if (!deviceConfig.ContainsKey(requirement.Key) || deviceConfig[requirement.Key] != requirement.Value)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
