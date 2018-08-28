using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace UltrasonicSensorTest
{
    public class Program
    {

        public static void Main()
        {
            //creating instance for class HC_SR04
            HC_SR04 mUS = new HC_SR04(Pins.GPIO_PIN_D4, Pins.GPIO_PIN_D5);
            while (true)
            {
                Debug.Print(mUS.Ping().ToString()); //distance(in mm) is printed on Output debugg console
                Thread.Sleep(1000);
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
}
