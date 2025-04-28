using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starting HTTP server to receive policy JSON...");
        StartHttpServer();
    }
    static string GetLocalIPv4Address()
    {
        try
        {
            // Get all network interfaces
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(i => i.OperationalStatus == OperationalStatus.Up && 
                            i.NetworkInterfaceType != NetworkInterfaceType.Loopback);
        
            foreach (var ni in networkInterfaces)
            {
                // Get IP properties
                var ipProps = ni.GetIPProperties();
            
                // Find the first IPv4 address that isn't loopback
                var ipAddress = ipProps.UnicastAddresses
                    .FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork && 
                                        !IPAddress.IsLoopback(ip.Address))?.Address;
            
                if (ipAddress != null)
                {
                    return ipAddress.ToString();
                }
            }
        }
        catch
        {
            // Fall through to return null
        }
    
        return null;
    }
    static void StartHttpServer()
    {
        HttpListener listener = new HttpListener();
        string localIp = GetLocalIPv4Address();
        listener.Prefixes.Add($"http://{localIp}:5000/"); // Listen on localhost, port 5000
        listener.Start();
        Console.WriteLine($"HTTP server started. Listening for POST requests on http://{localIp}:5000/");

        while (true)
        {
            try
            {
                // Wait for an incoming request
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;

                if (request.HttpMethod == "POST")
                {
                    // Read the request body
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    string policyJson = reader.ReadToEnd();

                    Console.WriteLine("Received policy JSON:");
                    Console.WriteLine(policyJson);

                    // Process the policy JSON
                    ProcessPolicy(policyJson);

                    // Send a response
                    HttpListenerResponse response = context.Response;
                    string responseString = "Policy processed successfully.";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
                else
                {
                    // Handle unsupported HTTP methods
                    HttpListenerResponse response = context.Response;
                    response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error handling request: {ex.Message}");
            }
        }
    }

    static void ProcessPolicy(string policyJson)
    {
        try
        {
            var policy = JsonConvert.DeserializeObject<Dictionary<string, string>>(policyJson);

            if (policy != null)
            {
                if (policy.TryGetValue("Firewall", out string? firewallValue) && firewallValue == "Enabled")
                {
                    EnableFirewall();
                }
                else
                {
                    Console.WriteLine("Firewall policy not enabled or not specified.");
                }

                if (policy.TryGetValue("Encryption", out string? encryptionValue) && encryptionValue == "Enabled")
                {
                    EncryptFolder(@"C:\DiplomFolder");
                }
                else
                {
                    Console.WriteLine("Encryption policy not enabled or not specified.");
                }

                if (policy.TryGetValue("Antivirus", out string? antivirusValue) && antivirusValue == "Enabled")
                {
                    EnableAntivirus();
                }
                else
                {
                    Console.WriteLine("Antivirus policy not enabled or not specified.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error processing policy: {ex.Message}");
        }
    }

    static void EnableFirewall()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "advfirewall set allprofiles state on",
                Verb = "runas",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var process = Process.Start(psi);
            process?.WaitForExit();

            if (process?.ExitCode == 0)
            {
                Console.WriteLine("✅ Firewall enabled successfully.");
            }
            else
            {
                Console.WriteLine($"❌ Failed to enable firewall. Exit code: {process?.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ An error occurred: {ex.Message}");
        }
    }

    static void EncryptFolder(string folderPath)//"C:\DiplomFolder"
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"The folder '{folderPath}' does not exist.");
                return;
            }

            File.Encrypt(folderPath);
            Console.WriteLine($"The folder '{folderPath}' has been encrypted successfully.");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine($"Access denied. Please run the program as Administrator to encrypt the folder '{folderPath}'.");
        }
        catch (PlatformNotSupportedException)
        {
            Console.WriteLine("Encryption is not supported on this platform. This feature requires NTFS file system.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while encrypting the folder: {ex.Message}");
        }
    }

    static void EnableAntivirus()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "Set-MpPreference -DisableRealtimeMonitoring $false",
                Verb = "runas",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var process = Process.Start(psi);
            process?.WaitForExit();

            if (process?.ExitCode == 0)
            {
                Console.WriteLine("✅ Windows Defender enabled successfully.");
            }
            else
            {
                Console.WriteLine($"❌ Failed to enable Windows Defender. Exit code: {process?.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error enabling Windows Defender: {ex.Message}");
        }
    }
}
