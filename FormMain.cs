// Windows Application - CNCScanner
// Copyright (C) 2023 https://www.roboticboat.uk
// e115048c-fbe2-4a29-a233-06981f814288
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
// These Terms shall be governed and construed in accordance with the laws of 
// England and Wales, without regard to its conflict of law provisions.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
using System.IO.Ports;
using System.Threading;
using Util.EventMessages;


namespace CNCscanner
{
    public partial class FormMain : Form
    {
        // Global variables
        string[] lines;
        int ptr = 0;
        bool isProgramRunning = false;

        public static SerialPort SerialPortCNC = new SerialPort();
        public static SerialPort SerialPortArduino = new SerialPort();

        private String SerialMessageCNC = "";
        private String SerialMessageArduino = "";

        public FormMain()
        {
            InitializeComponent();

            // Setup message handler for the main form
            GlobalEventMessages obj1 = new GlobalEventMessages();
            GlobalEventMessages.TheEvent += new GlobalEventHandler(ShowOnScreen);

            // When Serial data is recieved through the CNCscanner port, call this method
            SerialPortCNC.DataReceived += new SerialDataReceivedEventHandler(CNCSerialDataReceived);
            SerialPortArduino.DataReceived += new SerialDataReceivedEventHandler(SerialPortArduinoDataReceived);

            //Setup the SerialPorts List
            foreach (string port in SerialPort.GetPortNames())
            {
                // Add the next port to the list
                comboBoxPorts.Items.Add(port);
                comboBoxDist.Items.Add(port);

                // Select this last found port
                comboBoxPorts.Text = port;
                comboBoxDist.Text = port;
            }

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Close the application
            Application.Exit();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
       
            // If the port is open, close it.
            if (SerialPortCNC.IsOpen)
            {
                try
                {
                    // Close in another thread to avoid main thread hanging
                    Thread CloseDown = new Thread(new ThreadStart(CloseCNCSerialOnExit));
                    CloseDown.Start();

                    // Change the button text to "connect"
                    GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("btnConnect", "Connect"));
                    GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBox2", "Disconnected"));
                }
                catch (Exception ex)
                {
                    // Update User with error message
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                // Set the port's settings to connect to the CNCscanner

                SerialPortCNC.BaudRate = 115200;                                           // BaudRate
                SerialPortCNC.DataBits = 8;                                                // DataBits
                SerialPortCNC.StopBits = (StopBits)Enum.Parse(typeof(StopBits), "One");    // StopBits
                SerialPortCNC.Parity = (Parity)Enum.Parse(typeof(Parity), "None");         // Parity
                SerialPortCNC.PortName = comboBoxPorts.Text;                               // Port Number

                try
                {
                    // Open the Serial port
                    SerialPortCNC.Open();

                    // Change the button text to "disconnect"
                    GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("btnConnect", "Disconnect"));
                    GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBox2", "Connected"));
                }
                catch (Exception ex)
                {
                    // Update User with error message
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void CNCSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // If the com port has been closed, do nothing
                if (!SerialPortCNC.IsOpen) return;

                // Read the Serial Port
                string data = SerialPortCNC.ReadExisting();

                // Add to messages received so far. 
                // This is ok for small UAT as it grows in size
                SerialMessageCNC += data;

                // Send to mainform to show
                GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBox2", SerialMessageCNC));
            }
            catch (Exception ex)
            {
                // Update User with error message
                MessageBox.Show(ex.Message);
            }

            // We are runnign a program, so want to issue the next command
            if (isProgramRunning)
            {
                // Pause for 1 second
                System.Threading.Thread.Sleep(1000);

                // Increase the pointer
                ptr++;

                // Split the location into its X and Y absolute position
                string[] location = lines[ptr].Split(',');

                // Create the CNCscanner instruction
                string locInstruction = "$J=G90X" + location[0] + ".00Y" + location[1] + ".00F1917\n";

                // If the com port has been closed, do nothing
                if (!SerialPortCNC.IsOpen)
                {
                    return;
                }

                // Send Serial command
                SerialPortCNC.Write(locInstruction);

            }
        }

        private static void CloseCNCSerialOnExit()
        {
            try
            {
                // Close the serial port
                SerialPortCNC.Close();
            }
            catch (Exception ex)
            {
                // Update User with error message
                MessageBox.Show(ex.Message);
            }
        }

        public void ShowOnScreen(object o, GlobalEventArgs e)
        {
            try
            {
                if (e.sCont == "btnConnect") { btnConnect.Invoke(new MethodInvoker(delegate { btnConnect.Text = e.sMsg; })); }
                if (e.sCont == "btnDistance") { btnConnect.Invoke(new MethodInvoker(delegate { btnDistance.Text = e.sMsg; })); }

                if (e.sCont == "textBox1") { textBox1.Invoke(new MethodInvoker(delegate { textBox1.Text = e.sMsg; })); }
                if (e.sCont == "textBox2") { textBox2.Invoke(new MethodInvoker(delegate { textBox2.Text = e.sMsg; })); }

                Application.DoEvents();
            }
            catch
            {
            }
        }

        private void btnEast_Click(object sender, EventArgs e)
        {
            // Want to send command to CNCscanner
            // Move East 1.0 points
            // $J=G91X1.0F1917

            // If the com port has been closed, do nothing
            if (!SerialPortCNC.IsOpen)
            {
                return;
            }

            // Send Serial command
            SerialPortCNC.Write("$J=G91X1.0F1917\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBox1", "$J = G91X1.0F1917\n"));
        }

        private void btnWest_Click(object sender, EventArgs e)
        {
            // Want to send command to CNCscanner
            // Move West 1.0 points
            // $J=G91X-1.0F1917

            // If the com port has been closed, do nothing
            if (!SerialPortCNC.IsOpen)
            {
                return;
            }

            // Send Serial command
            SerialPortCNC.Write("$J=G91X-1.0F1917\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBox1", "$J = G91X-1.0F1917\n"));
        }

        private void btnNorth_Click(object sender, EventArgs e)
        {
            // Want to send command to CNCscanner
            // Move North 1.0 points
            // $J=G91Y1.0F1917

            // If the com port has been closed, do nothing
            if (!SerialPortCNC.IsOpen)
            {
                return;
            }

            // Send Serial command
            SerialPortCNC.Write("$J=G91Y1.0F1917\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBox1", "$J = G91Y1.0F1917\n"));
        }

        private void btnSouth_Click(object sender, EventArgs e)
        {
            // Want to send command to CNCscanner
            // Move South 1.0 points
            // $J=G91Y-1.0F1917

            // If the com port has been closed, do nothing
            if (!SerialPortCNC.IsOpen)
            {
                return;
            }

            // Send Serial command
            SerialPortCNC.Write("$J=G91Y-1.0F1917\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBox1", "$J = G91Y-1.0F1917\n"));
        }


        // ------------------------------------------------------------------------------------------------------------

        private void btnDistance_Click(object sender, EventArgs e)
        {
            // If the port is open, close it.
            if (SerialPortArduino.IsOpen)
            {
                try
                {
                    // Close in another thread to avoid main thread hanging
                    Thread CloseDown = new Thread(new ThreadStart(CloseSerialPortArduinoOnExit));
                    CloseDown.Start();

                    // Change the button text to "connect"
                    GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("btnDistance", "Connect"));
                    GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBox2", "Arduino disconnected"));
                }
                catch (Exception ex)
                {
                    // Update User with error message
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                // Set the port's settings to connect to the CNCscanner

                SerialPortArduino.BaudRate = 9600;                                             // BaudRate
                SerialPortArduino.DataBits = 8;                                                // DataBits
                SerialPortArduino.StopBits = (StopBits)Enum.Parse(typeof(StopBits), "One");    // StopBits
                SerialPortArduino.Parity = (Parity)Enum.Parse(typeof(Parity), "None");         // Parity
                SerialPortArduino.PortName = comboBoxPorts.Text;                               // Port Number

                try
                {
                    // Open the Serial port
                    SerialPortArduino.Open();

                    // Change the button text to "disconnect"
                    GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("btnDistance", "Disconnect"));
                    GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBox2", "btnDistance connected"));
                }
                catch (Exception ex)
                {
                    // Update User with error message
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private static void CloseSerialPortArduinoOnExit()
        {
            try
            {
                // Close the serial port
                SerialPortArduino.Close();
            }
            catch (Exception ex)
            {
                // Update User with error message
                MessageBox.Show(ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Take a reading
            // Send Serial command
            SerialPortArduino.Write("\n");
        }

        private void SerialPortArduinoDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // If the com port has been closed, do nothing
                if (!SerialPortArduino.IsOpen) return;

                // Read the Serial Port
                string data = SerialPortArduino.ReadExisting();

                // Add to messages received so far. 
                // This is ok for small UAT as it grows in size
                SerialMessageArduino += data;

                // Send to mainform to show
                GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBox2", SerialMessageArduino));
            }
            catch (Exception ex)
            {
                // Update User with error message
                MessageBox.Show(ex.Message);
            }
        }

        private void btnProgram_Click(object sender, EventArgs e)
        {
            // This is the program
            isProgramRunning = true;

            // Read the file as one string.
            lines = System.IO.File.ReadAllLines(@"C:\Users\Paul\source\repos\CNCscanner\bin\Debug\CNCscanner1.txt");

            // Reset the pointer
            ptr = 0;

            // Split the location into its X and Y absolute position
            string[] location = lines[ptr].Split(',');

            // Create the CNCscanner instruction
            string locInstruction = "$J=G90X" + location[0] + ".00Y" + location[1] + ".00F1917\n";

            // If the com port has been closed, do nothing
            if (!SerialPortCNC.IsOpen)
            {
                return;
            }

            // Send Serial command
            SerialPortCNC.Write(locInstruction);

        }
    }
}
