using System;                                   //for nativeeventhandler
using System.IO;                                //for StreamReader methods
using System.Net;                               //for httpwebrequest and ipaddress methods
using System.Threading;                         //for Thread method
using Microsoft.SPOT;                           //for Debug.Print methods
using Microsoft.SPOT.Hardware;                  //for OutPort,InterrupPort,etc
using SecretLabs.NETMF.Hardware.Netduino;       //for netduino hardware methods e.g Pins
using Microsoft.SPOT.Net.NetworkInformation;    //for networkinterface and related methods


namespace SmartBin
{
    public class Program
    {

        public static void Main()
        {
            OutputPort led = new OutputPort(Pins.ONBOARD_LED, false);
            App app = new App();
            app.Run();
            while (app.IsRunning)
            {
                led.Write(true); // turn on the LED
                Thread.Sleep(250); // sleep for 250ms
                led.Write(false); // turn off the LED
                Thread.Sleep(250); // sleep for 250ms

            }

            while (app.ipCheck == false) //keep looping if device not connected to network
            {
                app.Run();
                Thread.Sleep(1000);
            }

            HC_SR04 mUS = new HC_SR04(Pins.GPIO_PIN_D4, Pins.GPIO_PIN_D5);
            long initialLevel;
            initialLevel = mUS.Ping();
            byte cnt = 1;
            Debug.Print("Empty BinLevel: " + mUS.Ping().ToString() + "mm");
            long currentLevel;
            float fillPercent;
            while (true)
            {
                currentLevel = mUS.Ping();
                fillPercent = initialLevel - currentLevel;
                Debug.Print("Fill percentage of Bin is " + fillPercent.ToString() + "%");
                if (fillPercent > 80)
                {
                    if (cnt == 2)
                    {
                        Debug.Print("More than 80% Full");
                        Thread.Sleep(100);
                        //replace with your ClickSend API credential and mobile phone number
                        //app.MakeWebRequest("https://api-mapper.clicksend.com/http/v2/send.php?method=http&username=YOURUSERNAME&key=YOURAPIKEY&to=+61411111111&message=Please%20Empty%20WasteBin");  
                        app.MakeWebRequest("https://api-mapper.clicksend.com/http/v2/send.php?method=http&username=YOURUSERNAME&key=YOURAPIKEY&to=YOUNUMBERWITHCOUNTRYCODE&message=Please%20Empty%20WasteBin");
                        cnt = 0;
                    }

                }

                if (fillPercent > 50)
                {
                    if (cnt == 1)
                    {
                        Debug.Print("More than 50% Full");
                        Thread.Sleep(100);
                        //replace with your ClickSend API credential and mobile phone number
                        //app.MakeWebRequest("https://api-mapper.clicksend.com/http/v2/send.php?method=http&username=YOURUSERNAME&key=YOURAPIKEY&to=+61411111111&message=WasteBin%20is%20Half%20Filled");
                        app.MakeWebRequest("https://api-mapper.clicksend.com/http/v2/send.php?method=http&username=YOURUSERNAME&key=YOURAPIKEY&to=YOUNUMBERWITHCOUNTRYCODE&message=WasteBin%20is%20Half%20Filled");
                        cnt = 2;
                    }

                }

                if (fillPercent < 10)
                {
                    Debug.Print("Emptied!");
                    cnt = 1;
                }


                Thread.Sleep(5000);
            }
        }
    }

    public class HC_SR04
    {
        private OutputPort portOut;
        private InterruptPort interIn;
        private long beginTick;
        private long endTick;
        private long minTicks = 0;  // System latency, 

        //subtracted off ticks to find actual sound travel time

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pinTrig">Netduino pin connected to the HC-SR04 Trig pin</param>
        /// <param name="pinEcho">Netduino pin connected to the HC-SR04 Echo pin</param>
        public HC_SR04(Cpu.Pin pinTrig, Cpu.Pin pinEcho)
        {
            portOut = new OutputPort(pinTrig, false);
            interIn = new InterruptPort(pinEcho, false,

Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);
            interIn.OnInterrupt += new NativeEventHandler(interIn_OnInterrupt);
            minTicks = 4000L;

        }

        /// <summary>
        /// Trigger a sensor reading
        /// 
        /// </summary>
        /// <returns>Number of mm to the object</returns>
        public long Ping()
        {
            // Reset Sensor
            portOut.Write(true);
            Thread.Sleep(1);

            // Start Clock
            endTick = 0L;
            beginTick = System.DateTime.Now.Ticks;
            // Trigger Sonic Pulse
            portOut.Write(false);

            // Wait 1/20 second (this could be set as a variable instead of constant)
            Thread.Sleep(50);

            if (endTick > 0L)
            {
                // Calculate Difference
                long elapsed = endTick - beginTick;

                // Subtract out fixed overhead (interrupt lag, etc.)
                elapsed -= minTicks;
                if (elapsed < 0L)
                {
                    elapsed = 0L;
                }

                // Return elapsed ticks
                return elapsed * 10 / 636;
                ;
            }

            // Sonic pulse wasn't detected within 1/20 second
            return -1L;
        }

        /// <summary>
        /// This interrupt will trigger when detector receives back reflected sonic pulse       
        /// </summary>
        /// <param name="data1">Not used</param>
        /// <param name="data2">Not used</param>
        /// <param name="time">Transfer to endTick to calculated sound pulse travel time</param>
        void interIn_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            // Save the ticks when pulse was received back
            endTick = time.Ticks;
        }
    }

    public class App
    {
        NetworkInterface[] _interfaces;


        //public bool IsRunning { get; set; }
        bool _IsRunning;
        public bool IsRunning
        {
            get { return _IsRunning; }
            set { _IsRunning = value; }
        }

        bool _ipCheck;
        public bool ipCheck
        {
            get { return _ipCheck; }
            set { _ipCheck = value; }
        }

        public void Run()
        {
            this.IsRunning = true;
            Debug.Print("Initializing the networks...");
            bool goodToGo = InitializeNetwork();
            this.ipCheck = goodToGo;

            if (goodToGo)
            {
                //replace with your ClickSend API credential and mobile phone number
                //MakeWebRequest("https://api-mapper.clicksend.com/http/v2/send.php?method=http&username=YOURUSERNAME&key=YOURAPIKEY&to=+61411111111&message=Device%20Ready");
                MakeWebRequest("https://api-mapper.clicksend.com/http/v2/send.php?method=http&username=YOURUSERNAME&key=YOURAPIKEY&to=YOUNUMBERWITHCOUNTRYCODE&message=Device%20Ready");
            }

            this.IsRunning = false;
        }


        protected bool InitializeNetwork()
        {
            if (Microsoft.SPOT.Hardware.SystemInfo.SystemID.SKU == 3)
            {
                Debug.Print("Wireless tests run only on Device");
                return false;
            }

            Debug.Print("Getting all the network interfaces.");
            _interfaces = NetworkInterface.GetAllNetworkInterfaces();

            // debug output
            ListNetworkInterfaces();

            // loop through each network interface
            foreach (var net in _interfaces)
            {

                // debug out
                ListNetworkInfo(net);

                switch (net.NetworkInterfaceType)
                {
                    case (NetworkInterfaceType.Ethernet):
                        Debug.Print("Found Ethernet Interface");
                        break;
                    case (NetworkInterfaceType.Wireless80211):
                        Debug.Print("Found 802.11 WiFi Interface");
                        break;
                    case (NetworkInterfaceType.Unknown):
                        Debug.Print("Found Unknown Interface");
                        break;
                }

                // check for an IP address, try to get one if it's empty
                return CheckIPAddress(net);
            }

            // if we got here, should be false.
            return false;
        }

        public void MakeWebRequest(string url)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Method = "GET";

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                Debug.Print("this is what we got from " + url + ": " + result);
            }

        }


        protected bool CheckIPAddress(NetworkInterface net)
        {
            int timeout = 10000; // timeout, in milliseconds to wait for an IP. 10,000 = 10 seconds

            // check to see if the IP address is empty (0.0.0.0). IPAddress.Any is 0.0.0.0.
            if (net.IPAddress == IPAddress.Any.ToString())
            {
                Debug.Print("No IP Address");

                if (net.IsDhcpEnabled)
                {
                    Debug.Print("DHCP is enabled, attempting to get an IP Address");

                    // ask for an IP address from DHCP [note this is a static, not sure which network interface it would act on]
                    int sleepInterval = 10;
                    int maxIntervalCount = timeout / sleepInterval;
                    int count = 0;
                    while (IPAddress.GetDefaultLocalAddress() == IPAddress.Any && count < maxIntervalCount)
                    {
                        Debug.Print("Sleep while obtaining an IP");
                        Thread.Sleep(10);
                        count++;
                    };

                    // if we got here, we either timed out or got an address, so let's find out.
                    if (net.IPAddress == IPAddress.Any.ToString())
                    {
                        Debug.Print("Failed to get an IP Address in the alotted time.");
                        return false;
                    }

                    Debug.Print("Got IP Address: " + net.IPAddress.ToString());
                    return true;

                    //NOTE: this does not work, even though it's on the actual network device. [shrug]
                    // try to renew the DHCP lease and get a new IP Address
                    //net.RenewDhcpLease ();
                    //while (net.IPAddress == "0.0.0.0") {
                    //	Thread.Sleep (10);
                    //}

                }
                else
                {
                    Debug.Print("DHCP is not enabled, and no IP address is configured, bailing out.");
                    return false;
                }
            }
            else
            {
                Debug.Print("Already had IP Address: " + net.IPAddress.ToString());
                return true;
            }

        }

        protected void ListNetworkInterfaces()
        {
            foreach (var net in _interfaces)
            {
                switch (net.NetworkInterfaceType)
                {
                    case (NetworkInterfaceType.Ethernet):
                        Debug.Print("Found Ethernet Interface");
                        break;
                    case (NetworkInterfaceType.Wireless80211):
                        Debug.Print("Found 802.11 WiFi Interface");
                        break;
                    case (NetworkInterfaceType.Unknown):
                        Debug.Print("Found Unknown Interface");
                        break;

                }
            }
        }

        protected void ListNetworkInfo(NetworkInterface net)
        {
            Debug.Print("MAC Address: " + BytesToHexString(net.PhysicalAddress));
            Debug.Print("DHCP enabled: " + net.IsDhcpEnabled.ToString());
            Debug.Print("Dynamic DNS enabled: " + net.IsDynamicDnsEnabled.ToString());
            Debug.Print("IP Address: " + net.IPAddress.ToString());
            Debug.Print("Subnet Mask: " + net.SubnetMask.ToString());
            Debug.Print("Gateway: " + net.GatewayAddress.ToString());

            if (net is Wireless80211)
            {
                var wifi = net as Wireless80211;
                Debug.Print("SSID:" + wifi.Ssid.ToString());
            }

        }

        private static string BytesToHexString(byte[] bytes)
        {
            string hexString = string.Empty;

            // Create a character array for hexidecimal conversion.
            const string hexChars = "0123456789ABCDEF";

            // Loop through the bytes.
            for (byte b = 0; b < bytes.Length; b++)
            {
                if (b > 0)
                    hexString += "-";

                // Grab the top 4 bits and append the hex equivalent to the return string.        
                hexString += hexChars[bytes[b] >> 4];

                // Mask off the upper 4 bits to get the rest of it.
                hexString += hexChars[bytes[b] & 0x0F];
            }

            return hexString;
        }

    }
}
