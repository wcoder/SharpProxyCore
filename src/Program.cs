using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using NDesk.Options;

namespace SharpProxyCore
{
    class Program
    {
        const int MIN_PORT = 1;
        const int MAX_PORT = 65535;
        const string DEFAULT_EXTERNAL_PORT = "5000";
        const string DEFAULT_INTERNAL_PORT = "8887";

        static ProxyThread ProxyThreadListener = null;

        static void Main(string[] args)
        {
            var ipAddresses = GetLocalIPs().OrderBy(x => x).ToList();
            var externalPort = DEFAULT_EXTERNAL_PORT;
            var internalPort = DEFAULT_INTERNAL_PORT;
            var isRewriteHostHeaders = false; // IIS Express

            new OptionSet
            {
                { "i=|internal", intPort => internalPort = intPort },
                { "e=|external", extPort => externalPort = extPort },
            }.Parse(args);

            externalPort = FindAvailablePort(int.Parse(externalPort)).ToString();

            Start(externalPort, internalPort, isRewriteHostHeaders);

            ShowMessage($"Started:\nhttp://{ipAddresses[0]}:{externalPort} => http://127.0.0.1:{internalPort}");

            Console.ReadLine();
            Close();
        }

        static int FindAvailablePort(int port)
        {
            while (!IsPortAvailable(port))
            {
                port++;
            }
            return port;
        }

        static void Start(string externalPortStr, string internalPortStr, bool isRewriteHostHeaders)
        {
            int.TryParse(externalPortStr, out int externalPort);
            int.TryParse(internalPortStr, out int internalPort);

            if (!CheckPortRange(externalPort) ||
                !CheckPortRange(internalPort) ||
                externalPort == internalPort)
            {
                ShowError($"Ports must be between {MIN_PORT}-{MAX_PORT} and must not be the same.");
                return;
            }
            if (!IsPortAvailable(externalPort))
            {
                ShowError($"Port {externalPort} is not available, please select a different port.");
                return;
            }

            ProxyThreadListener = new ProxyThread(externalPort, internalPort, isRewriteHostHeaders);
        }

        static void Close()
        {
            ShowMessage("Proxy-connection closed!");
            ProxyThreadListener?.Stop();
        }

        static void ShowError(string msg) => Console.Error.WriteLine(msg);
        static void ShowMessage(string msg) => Console.WriteLine(msg);

        static bool CheckPortRange(int port) => !(port < MIN_PORT || port > MAX_PORT);

        static List<string> GetLocalIPs()
        {
            // Try to find our internal IP address...
            var myHost = Dns.GetHostName();
            var addresses = Dns.GetHostEntry(myHost).AddressList;
            var myIPs = new List<string>();
            var fallbackIP = "";

            for (int i = 0; i < addresses.Length; i++)
            {
                // Is this a valid IPv4 address?
                if (addresses[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    string thisAddress = addresses[i].ToString();
                    // Loopback is not our preference...
                    if (thisAddress == "127.0.0.1")
                        continue;
                    // 169.x.x.x addresses are self-assigned "private network" IP by Windows
                    if (thisAddress.StartsWith("169"))
                    {
                        fallbackIP = thisAddress;
                        continue;
                    }
                    myIPs.Add(thisAddress);
                }
            }
            if (myIPs.Count == 0 && !string.IsNullOrEmpty(fallbackIP))
            {
                myIPs.Add(fallbackIP);
            }

            return myIPs;
        }

        static bool IsPortAvailable(int port)
        {
            //http://stackoverflow.com/questions/570098/in-c-how-to-check-if-a-tcp-port-is-available

            // Evaluate current system tcp connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list, and if our port we would like to use
            // in our TcpClient is occupied, we will set isAvailable to false.
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            foreach (var tcpInfo in tcpConnInfoArray)
            {
                if (tcpInfo.LocalEndPoint.Port == port)
                    return false;
            }

            try
            {
                var listener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
                listener.Start();
                listener.Stop();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
