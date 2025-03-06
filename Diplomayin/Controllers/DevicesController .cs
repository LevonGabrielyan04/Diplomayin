//[   {     "Name": "Disk Cleanup",     "Frequency": "Weekly",     "NextExecution": "2023-10-30T10:00:00Z"   },   { "Name": "Software Update",     "Frequency": "Monthly",     "NextExecution": "2023-11-15T12:00:00Z"   } ]
//{   "Firewall": "Enabled",   "Encryption": "Enabled",   "PasswordPolicy": "Strong" }
using Diplomayin.Controllers;
using Diplomayin.Data;
using Diplomayin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.Web.Http;

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
        CheckMaintenanceNotifications();
        return View(await _context.Devices.ToListAsync());
    }

    private void CheckMaintenanceNotifications()
    {
        foreach (var device in _context.Devices)
        {
            if (!string.IsNullOrEmpty(device.MaintenanceTimes))
            {
                var maintenanceSchedules = JsonConvert.DeserializeObject<List<MaintenanceSchedule>>(device.MaintenanceTimes);

                foreach (var schedule in maintenanceSchedules)
                {
                    if (DateTime.UtcNow >= schedule.NextExecution)
                    {
                        Console.WriteLine($"Maintenance required for device {device.Name}: {schedule.Name}");
                    }
                }
            }
        }
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
        var devices = await _context.Devices.ToListAsync();
        var policies = await _context.Policies.ToListAsync();

        foreach (var device in devices)
        {
            foreach (var policy in policies)
            {
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
        var deviceConfig = GetDeviceConfiguration(device).Result;

        var policyRequirements = JsonConvert.DeserializeObject<Dictionary<string, string>>(policy.Requirements);

        // Update device configuration to match policy requirements
        foreach (var requirement in policyRequirements)
        {
            deviceConfig[requirement.Key] = requirement.Value;
        }

        // Simulate writing the updated configuration back to the device
        SaveDeviceConfiguration(device, deviceConfig).Wait();

        device.Configuration = JsonConvert.SerializeObject(deviceConfig);
        device.IsCompliant = true;

        _context.Devices.Update(device);
        await _context.SaveChangesAsync();
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
}