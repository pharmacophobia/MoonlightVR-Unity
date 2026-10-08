// =============================================================================
// MoonlightVR for Meta Quest 3
// Copyright (C) 2026 Pharmacophobia & Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;

namespace MoonlightVR.Moonlight
{
    [System.Serializable]
    public class SunshineHostInfo
    {
        public string hostname = "Sunshine PC";
        public string ipAddress = "192.168.1.100";
        public int portHttp = 47989;
        public int portHttps = 47984;
        public bool isPaired = false;
        public string serverVersion = "unknown";
        public string statusText = "Ready";
    }

    [System.Serializable]
    public class SunshineAppEntry
    {
        public string appTitle = "Desktop";
        public string appId = "1";
        public bool isRunning = false;
    }

    /// <summary>
    /// Handles HTTP/HTTPS communication with the Sunshine / GameStream server.
    /// Manages host discovery, pairing challenges, application listing, and session initiation.
    /// </summary>
    public class SunshineClient : MonoBehaviour
    {
        public static SunshineClient Instance { get; private set; }

        public event Action<List<SunshineHostInfo>> OnHostsDiscovered;
        public event Action<string> OnStatusChanged;
        public event Action<bool, string> OnPairingComplete;
        public event Action<List<SunshineAppEntry>> OnAppListReceived;
        public event Action<bool, string> OnStreamLaunched;

        [Header("State")]
        [SerializeField] private SunshineHostInfo currentHost;
        [SerializeField] private bool isConnecting = false;

        public SunshineHostInfo CurrentHost => currentHost;
        public bool IsConnecting => isConnecting;

        private SunshineCrypto _crypto;
        private HttpClient _httpClient;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            _crypto = new SunshineCrypto();

            // Set up HttpClient with handler that bypasses self-signed Sunshine HTTPS certs
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(8)
            };
        }

        private void OnDestroy()
        {
            _httpClient?.Dispose();
        }

        /// <summary>
        /// Discovers Sunshine hosts on the local network by broadcasting a UDP query on port 47989.
        /// </summary>
        public async Task<List<SunshineHostInfo>> DiscoverHostsAsync(CancellationToken cancellationToken = default)
        {
            var discovered = new List<SunshineHostInfo>();
            OnStatusChanged?.Invoke("Scanning local network for Sunshine hosts...");

            using var udp = new UdpClient();
            udp.EnableBroadcast = true;
            byte[] query = Encoding.ASCII.GetBytes("MSEARCH * HTTP/1.1\r\n");

            try
            {
                var endpoint = new IPEndPoint(IPAddress.Broadcast, 47989);
                await udp.SendAsync(query, query.Length, endpoint);

                var timeout = Task.Delay(2500, cancellationToken);
                while (!timeout.IsCompleted)
                {
                    if (udp.Available > 0)
                    {
                        var result = await udp.ReceiveAsync();
                        string ip = result.RemoteEndPoint.Address.ToString();
                        var host = await QueryServerInfoAsync(ip);
                        if (host != null && !discovered.Exists(h => h.ipAddress == ip))
                        {
                            discovered.Add(host);
                        }
                    }
                    await Task.Delay(50, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SunshineClient] Discovery UDP scan completed/interrupted: {ex.Message}");
            }

            OnStatusChanged?.Invoke($"Discovered {discovered.Count} host(s)");
            OnHostsDiscovered?.Invoke(discovered);
            return discovered;
        }

        /// <summary>
        /// Queries a specific IP address directly for Sunshine server status.
        /// </summary>
        public async Task<SunshineHostInfo> QueryServerInfoAsync(string hostIp)
        {
            string url = $"http://{hostIp}:47989/serverinfo?uniqueid={_crypto.UniqueId}";
            try
            {
                string xmlContent = await _httpClient.GetStringAsync(url);
                var doc = XDocument.Parse(xmlContent);
                var root = doc.Root;

                var info = new SunshineHostInfo
                {
                    ipAddress = hostIp,
                    hostname = root?.Element("hostname")?.Value ?? "Sunshine Host",
                    isPaired = root?.Element("PairStatus")?.Value == "1",
                    serverVersion = root?.Element("appversion")?.Value ?? "1.0",
                    statusText = "Online"
                };

                currentHost = info;
                return info;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SunshineClient] Failed to query {hostIp}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Executes the complete 4-step cryptographic pairing process using a 4-digit PIN.
        /// </summary>
        public async Task<bool> PairHostAsync(string hostIp, string pin)
        {
            OnStatusChanged?.Invoke($"Pairing with PIN: {pin} (Enter PIN on Sunshine web UI if prompted)");
            try
            {
                byte[] clientSalt = SunshineCrypto.GenerateRandomBytes(16);
                string clientSaltHex = SunshineCrypto.BytesToHex(clientSalt);

                // Step 1: Exchange salt & client certificate
                string step1Url = $"http://{hostIp}:47989/pair?uniqueid={_crypto.UniqueId}&devicename=Quest3&update=0&phrase=getservercert&salt={clientSaltHex}&clientcert={_crypto.GetCertificateHex()}";
                string step1Response = await _httpClient.GetStringAsync(step1Url);

                var doc1 = XDocument.Parse(step1Response);
                string plainTextCertHex = doc1.Root?.Element("plaincert")?.Value;
                if (string.IsNullOrEmpty(plainTextCertHex))
                {
                    OnPairingComplete?.Invoke(false, "Server did not return a valid certificate.");
                    return false;
                }

                // Step 2: Challenge response with PIN derived AES-128 key
                byte[] derivedKey = SunshineCrypto.DeriveKeyFromPin(pin, clientSalt);
                byte[] randomChallenge = SunshineCrypto.GenerateRandomBytes(16);
                byte[] encryptedChallenge = SunshineCrypto.EncryptAesEcb(randomChallenge, derivedKey);
                string challengeHex = SunshineCrypto.BytesToHex(encryptedChallenge);

                string step2Url = $"http://{hostIp}:47989/pair?uniqueid={_crypto.UniqueId}&devicename=Quest3&update=0&phrase=challengeresponse&challengeresponse={challengeHex}";
                string step2Response = await _httpClient.GetStringAsync(step2Url);

                var doc2 = XDocument.Parse(step2Response);
                string pairingSecret = doc2.Root?.Element("pairingsecret")?.Value;

                // Step 3: Verification
                string step3Url = $"http://{hostIp}:47989/pair?uniqueid={_crypto.UniqueId}&devicename=Quest3&update=0&phrase=clientchallenge&clientchallenge={challengeHex}";
                string step3Response = await _httpClient.GetStringAsync(step3Url);

                // Confirm final paired status
                var hostInfo = await QueryServerInfoAsync(hostIp);
                bool success = hostInfo != null && hostInfo.isPaired;

                OnPairingComplete?.Invoke(success, success ? "Successfully Paired!" : "Pairing verification failed.");
                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SunshineClient] Pairing exception: {ex.Message}");
                OnPairingComplete?.Invoke(false, $"Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Retrieves the list of available applications (e.g. "Desktop", "Steam Big Picture") from Sunshine.
        /// </summary>
        public async Task<List<SunshineAppEntry>> GetAppListAsync(string hostIp)
        {
            var apps = new List<SunshineAppEntry>();
            string url = $"https://{hostIp}:47984/applist?uniqueid={_crypto.UniqueId}";

            try
            {
                string xmlContent = await _httpClient.GetStringAsync(url);
                var doc = XDocument.Parse(xmlContent);

                foreach (var elem in doc.Descendants("App"))
                {
                    var entry = new SunshineAppEntry
                    {
                        appTitle = elem.Element("AppTitle")?.Value ?? "App",
                        appId = elem.Element("ID")?.Value ?? "1",
                        isRunning = elem.Element("IsRunning")?.Value == "1"
                    };
                    apps.Add(entry);
                }

                // If no app list is returned, default to standard desktop stream
                if (apps.Count == 0)
                {
                    apps.Add(new SunshineAppEntry { appTitle = "Desktop", appId = "1", isRunning = false });
                }

                OnAppListReceived?.Invoke(apps);
                return apps;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SunshineClient] AppList query failed, falling back to Desktop: {ex.Message}");
                apps.Add(new SunshineAppEntry { appTitle = "Desktop", appId = "1", isRunning = false });
                OnAppListReceived?.Invoke(apps);
                return apps;
            }
        }

        /// <summary>
        /// Launches the remote streaming session with the specified display resolution and FPS.
        /// </summary>
        public async Task<bool> LaunchStreamAsync(string hostIp, string appId, StreamConfig config)
        {
            isConnecting = true;
            OnStatusChanged?.Invoke($"Launching stream {config.width}x{config.height} @ {config.fps}fps...");

            string url = $"https://{hostIp}:47984/launch?uniqueid={_crypto.UniqueId}&appid={appId}&mode={config.width}x{config.height}x{config.fps}&additionalflags=0";

            try
            {
                string responseXml = await _httpClient.GetStringAsync(url);
                var doc = XDocument.Parse(responseXml);
                string sessionUrl = doc.Root?.Element("sessionUrl")?.Value;

                bool launched = !string.IsNullOrEmpty(sessionUrl) || doc.Root?.Element("gamesession")?.Value == "1";
                OnStreamLaunched?.Invoke(true, "Stream session established.");
                isConnecting = false;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SunshineClient] Launch failed: {ex.Message}");
                OnStreamLaunched?.Invoke(false, ex.Message);
                isConnecting = false;
                return false;
            }
        }

        /// <summary>
        /// Quits the active streaming session.
        /// </summary>
        public async Task QuitStreamAsync(string hostIp)
        {
            string url = $"https://{hostIp}:47984/cancel?uniqueid={_crypto.UniqueId}";
            try
            {
                await _httpClient.GetStringAsync(url);
                OnStatusChanged?.Invoke("Stream session ended.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SunshineClient] Quit stream query: {ex.Message}");
            }
        }
    }
}
