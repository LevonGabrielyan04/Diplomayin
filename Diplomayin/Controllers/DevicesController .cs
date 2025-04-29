//[   {     "Name": "Disk Cleanup",     "Frequency": "Weekly",     "NextExecution": "2023-10-30T10:00:00Z"   },   { "Name": "Software Update",     "Frequency": "Monthly",     "NextExecution": "2023-11-15T12:00:00Z"   } ]
//{   "Firewall": "Enabled",   "Encryption": "Enabled",   "Antivirus": "Enabled" }
using Diplomayin.Controllers;
using Diplomayin.Data;
using Diplomayin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.Web.Http;
using System.Net.NetworkInformation;
using System.Net;

// MaintenanceSchedule class
public class MaintenanceSchedule
{
    public string Name { get; set; } 
    public string Frequency { get; set; } 
    public DateTime NextExecution { get; set; }
}

[System.Web.Http.RoutePrefix("api/devices")]
public class DevicesApiController : ApiController
{
    private static List<Device> _devices = new List<Device>();

    // POST: api/devices/{id}/urgent-maintenance
    [System.Web.Http.HttpPost]
    [System.Web.Http.Route("{id}/urgent-maintenance")]
    public IHttpActionResult UrgentMaintenance(int id, [System.Web.Http.FromBody] UrgentMaintenanceRequest request)
    {
        var device = _devices.FirstOrDefault(d => d.Id == id);
        if (device == null)
        {
            return NotFound();
        }
        Console.WriteLine($"Urgent maintenance required for device {device.Name}: {request.Message}");

        return Ok();
    }
}

public class UrgentMaintenanceRequest
{
    public string Message { get; set; }
}

public class DevicesController : Controller
{
    private static List<Device> _devices = new List<Device>();
    private static List<Policy> _policies = PoliciesController._policies;

    // Base URL of the device management API
    private static readonly string ApiBaseUrl = "https://api.example.com/devices";

    // HTTP client for making API calls
    private static readonly HttpClient httpClient = new HttpClient();

    private readonly AppDbContext _context;

    public DevicesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Devices
    public async Task<IActionResult> Index()
    {
        string messages = CheckMaintenanceNotifications();
        ViewBag.AlertMessage = messages;
        MaintenanceService maintenanceService = new MaintenanceService();
        foreach (var item in _context.Devices)
        {
            item.MaintenanceTimes = maintenanceService.UpdateMaintenanceTimes(item.MaintenanceTimes);
            _context.Devices.Update(item);
            _context.SaveChanges();
        }
        return View(await _context.Devices.ToListAsync());
    }

    public class MaintenanceService
    {
        public string UpdateMaintenanceTimes(string jsonData)
        {
            // Deserialize the JSON string to a list of MaintenanceTask objects
            var tasks = JsonConvert.DeserializeObject<List<MaintenanceTask>>(jsonData);
        
            // Update each task's NextExecution based on its frequency
            foreach (var task in tasks)
            {
                switch (task.Frequency.ToLower())
                {
                    case "weekly":
                        task.NextExecution = DateTime.Now.AddDays(7);
                        break;
                    case "monthly":
                        task.NextExecution = DateTime.Now.AddMonths(1);
                        break;
                    // Add more frequency cases if needed
                }
            }
        
            // Serialize the updated list back to JSON
            return JsonConvert.SerializeObject(tasks, Formatting.Indented);
        }
        public class MaintenanceTask
        {
            public string Name { get; set; }
            public string Frequency { get; set; }
            public DateTime NextExecution { get; set; }
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        var device = await _context.Devices.FirstOrDefaultAsync(d => d.Id == id);
        
         _context.Devices.Remove(device);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }

    [Microsoft.AspNetCore.Mvc.HttpGet("Devices/Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var device = await _context.Devices.FirstOrDefaultAsync(d => d.Id == id);
        return View(device);
    }

            [Microsoft.AspNetCore.Mvc.HttpPost]
        [ValidateAntiForgeryToken]
     public async Task<IActionResult> EditPost(Device device)
     {
            if (!ModelState.IsValid)
            {
                return View("Edit", device);
            }
         _context.Devices.Update(device);
         await _context.SaveChangesAsync();
         return RedirectToAction("Index");
     }
     


    private string CheckMaintenanceNotifications()
    {
        string messages = "";
        foreach (var device in _context.Devices)
        {
            if (!string.IsNullOrEmpty(device.MaintenanceTimes))
            {
                var maintenanceSchedules = JsonConvert.DeserializeObject<List<MaintenanceSchedule>>(device.MaintenanceTimes);

                foreach (var schedule in maintenanceSchedules)
                {
                    if (DateTime.UtcNow >= schedule.NextExecution)
                    {
                        //Console.WriteLine($"Maintenance required for device {device.Name}: {schedule.Name}");
                        messages += $"Maintenance required for device {device.Name}: {schedule.Name}";
                    }
                }
            }
        }
        return messages;
    }
    // GET: Devices/Create
    public ActionResult Create()
    {
        return View();
    }

    // POST: Devices/Create
    [Microsoft.AspNetCore.Mvc.HttpPost]
    public async Task<IActionResult> Create(Device device)
    {
        //if (ModelState.IsValid)
        {
            device.Id = _devices.Count + 1;

            // Initialize Configuration with an empty JSON object if it is null
            if (string.IsNullOrEmpty(device.Configuration))
            {
                device.Configuration = "{}"; // Empty JSON object
            }

            _context.Devices.Add(device);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        return View(device);
    }

    // Enforce policies on all devices
    public async Task<IActionResult> EnforcePolicies()
    {
        //        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Devices");

        //await _context.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence WHERE name = 'Devices'");

                        
        var devices = _context.Devices.ToList();
        var policies = _policies;
        foreach (var device in devices)
        {
            foreach (var policy in policies)
            {
                string url = $"http://{device.IPAddress}:5000";
                var content = new StringContent(JsonConvert.SerializeObject(policy), Encoding.UTF8, "application/json");
                
                try { 
                    httpClient.PostAsync(url, content);
                } catch (Exception ex) { }

                // Make the device compliant with the policy
                await MakeDeviceCompliant(device, policy);

            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    // Helper method to make a device compliant with a policy
    private async Task MakeDeviceCompliant(Device device, Policy policy)
    {
        try
        {
            device.IsCompliant = true;

            // Update the device in the database
            _context.Devices.Update(device);
            await _context.SaveChangesAsync();
            // Deserialize the policy requirements
            var policyRequirements = JsonConvert.DeserializeObject<Dictionary<string, string>>(policy.Requirements);

            // Log the policy being applied
            Console.WriteLine($"Applying policy to device {device.Name} at {device.IPAddress}: {JsonConvert.SerializeObject(policyRequirements)}");

            // Send the policy to the device via a POST request
            string url = $"http://{device.IPAddress}:5000";
            var content = new StringContent(JsonConvert.SerializeObject(policyRequirements), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(url, content);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error applying policy to device {device.Name}: {ex.Message}");
        }
    }


    // Simulate reading the device's configuration from an API
    private async Task<Dictionary<string, string>> GetDeviceConfiguration(Device device)
    {
        string url = $"{ApiBaseUrl}/{device.Id}/config";

        try
        {
            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode(); 

            string json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error fetching device configuration: {ex.Message}");
            return new Dictionary<string, string>();
        }
    }

    // Simulate writing the device's configuration to an API
    private async Task SaveDeviceConfiguration(Device device, Dictionary<string, string> config)
    {
        string url = $"{ApiBaseUrl}/{device.Id}/config";
        string json = JsonConvert.SerializeObject(config);

        try
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PutAsync(url, content);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error updating device configuration: {ex.Message}");
        }
    }
    private string GetLocalSubnet()
{
    foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
    {
        if (networkInterface.OperationalStatus == OperationalStatus.Up &&
            (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
             networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
        {
            var ipProperties = networkInterface.GetIPProperties();
            foreach (var unicastAddress in ipProperties.UnicastAddresses)
            {
                if (unicastAddress.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    var ipAddress = unicastAddress.Address;
                    var subnetMask = unicastAddress.IPv4Mask;

                    if (ipAddress != null && subnetMask != null)
                    {
                        // Calculate the subnet
                        var subnetBytes = ipAddress.GetAddressBytes();
                        var maskBytes = subnetMask.GetAddressBytes();
                        var subnetAddress = new byte[subnetBytes.Length];

                        for (int i = 0; i < subnetBytes.Length; i++)
                        {
                            subnetAddress[i] = (byte)(subnetBytes[i] & maskBytes[i]);
                        }

                        return string.Join(".", subnetAddress.Take(3)); // Return the first three octets
                    }
                }
            }
        }
    }

    return null; // Subnet could not be determined
}
    //await DiscoverDevices("192.168.1");
    public async Task<IActionResult> DiscoverDevices()
    {
        //await _context.Database.ExecuteSqlRawAsync("DELETE FROM Devices");
        //return BadRequest("Unable to detect the local subnet."); ;
        var subnet = GetLocalSubnet();
    if (string.IsNullOrEmpty(subnet))
    {
        return BadRequest("Unable to detect the local subnet.");
    }

    var discoveredDevices = new List<Device>();
    var pingTasks = new List<Task<(string ipAddress, bool isReachable)>>();

    // Generate all possible IP addresses in the subnet and start pinging them in parallel
    for (int i = 1; i < 255; i++) // Assuming a /24 subnet
    {
        string ipAddress = $"{subnet}.{i}";
        pingTasks.Add(PingDeviceAsync(ipAddress));
    }

    // Wait for all ping tasks to complete
    var pingResults = await Task.WhenAll(pingTasks);

    string exeFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Files", "example.exe"); // Adjust the path as needed


    // Process the results of the ping operations
    foreach (var result in pingResults)
    {
        if (result.isReachable)
        {
            var existingDevice = await _context.Devices.FirstOrDefaultAsync(d => d.IPAddress == result.ipAddress);
            if (existingDevice != null)
            {
                Console.WriteLine($"Device with IP {result.ipAddress} already exists. Skipping...");
                continue;
            }
            var device = new Device
            {
                Name = $"Device-{result.ipAddress.Split('.').Last()}",
                OperatingSystem = "Unknown",
                Configuration = "[]",
                IsCompliant = false,
                MaintenanceTimes = "[]",
                IPAddress = result.ipAddress
            };

            discoveredDevices.Add(device);

            //await SendExeToDevice(result.ipAddress, exeFilePath);
        }
    }

    // Add discovered devices to the database
    if (discoveredDevices.Any())
    {
        _context.Devices.AddRange(discoveredDevices);
        await _context.SaveChangesAsync();
    }

    //return Json(discoveredDevices);
    //return View(await _context.Devices.ToListAsync());
    return RedirectToAction("Index");
}

    private async Task SendExeToDevice(string deviceIpAddress, string filePath)
{
    try
    {
        // Ensure the file exists
        if (!System.IO.File.Exists(filePath))
        {
            Console.WriteLine($"❌ File not found: {filePath}");
            return;
        }

        // Create the URL for the device's endpoint
        string url = $"http://{deviceIpAddress}:5000/upload"; // Assuming the device listens on port 5000 and has an /upload endpoint

        // Read the file content
        var fileContent = new ByteArrayContent(await System.IO.File.ReadAllBytesAsync(filePath));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        // Create the multipart form data content
        var formData = new MultipartFormDataContent
        {
            { fileContent, "file", System.IO.Path.GetFileName(filePath) }
        };

        // Send the POST request
        HttpResponseMessage response = await httpClient.PostAsync(url, formData);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"✅ Successfully sent the file to device at {deviceIpAddress}.");
        }
        else
        {
            Console.WriteLine($"❌ Failed to send the file to device at {deviceIpAddress}. Status Code: {response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Error sending file to device at {deviceIpAddress}: {ex.Message}");
    }
}

private async Task<(string ipAddress, bool isReachable)> PingDeviceAsync(string ipAddress)
{
    using (var ping = new Ping())
    {
        try
        {
            var reply = await ping.SendPingAsync(ipAddress, 100); // Timeout: 100ms
            return (ipAddress, reply.Status == IPStatus.Success);
        }
        catch
        {
            return (ipAddress, false);
        }
    }
}


    private async Task<bool> PingDevice(string ipAddress)
    {
        using (var ping = new Ping())
        {
            try
            {
                var reply = await ping.SendPingAsync(ipAddress, 100); // Timeout: 100ms
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }
    }
}