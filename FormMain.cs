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
using System.Diagnostics;

namespace CNCscanner
{ 
    public partial class FormMain : Form
    {
        // Global variables
        bool isProgramRunning = false;

        private int xMin = 10;
        //private int xStep = 10;
        private int xMax = 400;
        private int yMin = 10;
        private int yStep = 10;
        private int yMax = 1050;
        private int xPosition = 0;
        private int yPosition = 0;
        private string CNCstatus = "";

        string distanceMM = "0";
        string building = "";
        string xPos = "";
        string yPos = "";

        public static SerialPort SerialPortCNC = new SerialPort();
        public static SerialPort SerialPortArduino = new SerialPort();

        private String SerialMessageCNC = "";

        Stopwatch timer = new Stopwatch();

        private StreamWriter myFile = new StreamWriter("Scan_" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".csv", true);

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
                comboArduino.Items.Add(port);

                // Select this last found port
                comboBoxPorts.Text = port;
                comboArduino.Text = port;
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
                    GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "Disconnected\r\n"));
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
                    GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "Connected\r\n"));
                }
                catch (Exception ex)
                {
                    // Update User with error message
                    MessageBox.Show(ex.Message);
                }
            }
        }

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
                    GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "Arduino disconnected\r\n"));
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

                SerialPortArduino.BaudRate = 38400;                                          // BaudRate
                SerialPortArduino.DataBits = 8;                                               // DataBits
                SerialPortArduino.StopBits = (StopBits)Enum.Parse(typeof(StopBits), "One");   // StopBits
                SerialPortArduino.Parity = (Parity)Enum.Parse(typeof(Parity), "None");        // Parity
                SerialPortArduino.PortName = comboArduino.Text;                               // Port Number

                try
                {
                    // Open the Serial port
                    SerialPortArduino.Open();

                    // Change the button text to "disconnect"
                    GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("btnDistance", "Disconnect"));
                    GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "Arduino connected\r\n"));
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
            // If the com port has been closed, do nothing
            if (!SerialPortCNC.IsOpen) return;

            // Just reconfirming the Serial port used.
            SerialPort port = (SerialPort)sender;

            // Allocate memory to receive the message
            byte[] _buffer = new byte[port.BytesToRead];

            // Read the Serial port
            port.Read(_buffer, 0, _buffer.Length);

            // Encoding standard
            Encoding encoding = ASCIIEncoding.GetEncoding(1252);
            
            // If the butter is not null.
            // This is not perfect. Only processing the recently received data and
            // not making a stream, thus some messages might be processed incomplete.
            if (_buffer != null)
            {
                // Add to messages received so far. 
                // This is ok for small UAT as it grows in size
                SerialMessageCNC = encoding.GetString(_buffer);

                // Update CNC status
                if (SerialMessageCNC.Contains("<Home|")) { CNCstatus = "Home";}
                if (SerialMessageCNC.Contains("<Idle|")) { CNCstatus = "Idle"; }
                if (SerialMessageCNC.Contains("<Jog|")) { CNCstatus = "Jog"; }

                if (SerialMessageCNC.Contains("MPos:"))
                {
                    int xpos = SerialMessageCNC.IndexOf("MPos:", 0) +5;
                    int ypos = SerialMessageCNC.IndexOf(",", xpos);
                    int yend = SerialMessageCNC.IndexOf(",", ypos+1);
                    if (xpos > -1 && ypos > -1 && yend > -1 && yend > ypos)
                    {
                        xPos = SerialMessageCNC.Substring(xpos, ypos - xpos);
                        yPos = SerialMessageCNC.Substring(ypos + 1, yend - ypos - 1);

                        long duration = timer.ElapsedMilliseconds;

                        // Update the User
                        GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", duration.ToString() + "," + xPos.ToString() + "," + yPos.ToString() + "," + distanceMM.ToString() + "\r\n"));

                        // Save the current reading to disk
                        myFile.Write(xPos.ToString() + "," + yPos.ToString() + "," + distanceMM.ToString() + "\n");
                        myFile.Flush();

                    }
                }
            }
        }

        private void SerialPortArduinoDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // If the com port has been closed, do nothing
            if (!SerialPortArduino.IsOpen) return;

            // Just reconfirming the Serial port used.
            SerialPort port = (SerialPort)sender;

            // Allocate memory to receive the message
            byte[] _buffer = new byte[port.BytesToRead];

            // Read the Serial port
            port.Read(_buffer, 0, _buffer.Length);

            // Encoding standard
            Encoding encoding = ASCIIEncoding.GetEncoding(1252);

            // If the butter is not null
            if (_buffer != null)
            {
                // Convert to string
                string sdata = encoding.GetString(_buffer);

                // Loop over the string
                for (int i = 0; i < sdata.Length; i++)
                {
                    // Do we have a starting char?
                    if (sdata[i] == '$')
                    {
                        building = "";
                    }
                    // Do we have the checksum?
                    else if (sdata[i] == '*')
                    {
                        // End if not checking the checksum
                        distanceMM = building;

                        // Update the User
                        GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("toolStripStatusLabel1", distanceMM));
                    }
                    else
                    {
                        // Just another char
                        building += sdata[i];
                    }
                }

            }

        }

        private void MoveCNC()
        {
            // We are runnign a program, so want to issue the next command
            if (isProgramRunning)
            {
                if (xPosition == xMin)
                {
                    // Move to the far-side
                    xPosition = xMax;

                    // Create the CNCscanner instruction
                    string locInstruction = "$J=G90X" + xPosition.ToString() + "Y" + yPosition.ToString() + "F500\n";

                    // Send Serial command
                    SerialPortCNC.Write(locInstruction);
                }
                else if (xPosition == xMax)
                {
                    // Move to the near-side
                    xPosition = xMin;
                    yPosition += yStep;

                    // Create the CNCscanner instruction
                    string locInstruction = "$J=G90X" + xPosition.ToString() + "Y" + yPosition.ToString() + "F500\n";

                    // Send Serial command
                    SerialPortCNC.Write(locInstruction);
                }
                else
                {
                    MessageBox.Show("Something went wrong");
                }

                if (yPosition > yMax)
                {
                    // Turn off timer
                    timer1.Enabled = false;
                    timer2.Enabled = false;

                    // Turn-off running flag
                    isProgramRunning = false;

                    // Stop the stopwatch
                    timer.Stop();
                    
                    // Update the User
                    MessageBox.Show("Finished");

                    return;
                }

                // Send to mainform to show
                GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "$J=G90X" + xPosition.ToString() + "Y" + yPosition.ToString() + "F500     "));
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
            if (e.sCont == "btnConnect") { btnConnect.Invoke(new MethodInvoker(delegate { btnConnect.Text = e.sMsg; })); }
            if (e.sCont == "btnDistance") { btnConnect.Invoke(new MethodInvoker(delegate { btnDistance.Text = e.sMsg; })); }
            if (e.sCont == "btnProgram") { btnConnect.Invoke(new MethodInvoker(delegate { btnProgram.Text = e.sMsg; })); }

            if (e.sCont == "textBoxMessages") { textBoxMessages.Invoke(new MethodInvoker(delegate { textBoxMessages.AppendText(e.sMsg + '\n'); })); }
            //if (e.sCont == "toolStripStatusLabel1") { toolStripStatusLabel1.Invoke(new MethodInvoker(delegate { toolStripStatusLabel2.Text = e.sMsg; })); }
            //if (e.sCont == "toolStripStatusLabel2") { toolStripStatusLabel2.Text = e.sMsg; }
           
            Application.DoEvents();
        }

        private void btnEast_Click(object sender, EventArgs e)
        {
            // If the com port has been closed, do nothing
            if (!SerialPortCNC.IsOpen) return;

            // Send Serial command
            SerialPortCNC.Write("$J=G91X20F500\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "$J = G91X20F500     "));
        }

        private void btnWest_Click(object sender, EventArgs e)
        {
            // If the com port has been closed, do nothing
            if (!SerialPortCNC.IsOpen) return;

            // Send Serial command
            SerialPortCNC.Write("$J=G91X-20F500\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "$J = G91X-20F500     "));
        }

        private void btnNorth_Click(object sender, EventArgs e)
        {
            // If the com port has been closed, do nothing
            if (!SerialPortCNC.IsOpen) return;

            // Send Serial command
            SerialPortCNC.Write("$J=G91Y20F500\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "$J = G91Y20F500     "));
        }

        private void btnSouth_Click(object sender, EventArgs e)
        {
            // If the com port has been closed, do nothing
            if (!SerialPortCNC.IsOpen) return;

            // Send Serial command
            SerialPortCNC.Write("$J=G91Y-20F500\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "$J = G91Y-20F500     "));
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            // If the com port has been closed, do nothing
            if (!SerialPortCNC.IsOpen) return;

            // Send Serial command
            SerialPortCNC.Write("$H\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "$H     "));
        }

        private void btnFarAway_Click(object sender, EventArgs e)
        {
            // If the com port has been closed, do nothing
            if (!SerialPortCNC.IsOpen) return;

            // Send Serial command
            SerialPortCNC.Write("$J=G90X390Y370F500\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "$J=G90X390Y370F500     "));
        }

        // ------------------------------------------------------------------------------------------------------------

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

        private void btnProgram_Click(object sender, EventArgs e)
        {
            // This is the program
            isProgramRunning = true;

            // Turn on timer
            if (timer1.Enabled)
            {
                // Turn off timers
                timer1.Enabled = false;
                timer2.Enabled = false;

                // Stop timer
                timer.Stop();

                // Change the button text to "disconnect"
                GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("btnProgram", "Program"));
            }
            else
            {
                // Turn on timers
                timer1.Enabled = true;
                timer2.Enabled = true;

                // Start the stopwatch
                timer.Start();

                // Change the button text to "disconnect"
                GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("btnProgram", "Stop"));
            }

            // Starting position
            xPosition = xMin;
            yPosition = yMin;

            // If the serial port is open
            if (SerialPortCNC.IsOpen)
            {
                // Create the CNCscanner instruction
                string locInstruction = "$J=G90X" + xPosition.ToString() + "Y" + yPosition.ToString() + "F500\n";

                // Send Serial command
                SerialPortCNC.Write(locInstruction);
            }

        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Close the open Serial ports. Start as different threads so the main form does not hang

            // If the port is open, close it.
            if (SerialPortCNC.IsOpen)
            {
                try
                {
                    // Close in another thread to avoid main thread hanging
                    Thread CloseDown = new Thread(new ThreadStart(CloseCNCSerialOnExit));
                    CloseDown.Start();
                }
                catch
                {
                }
            }

            // If the port is open, close it.
            if (SerialPortArduino.IsOpen)
            {
                try
                {
                    // Close in another thread to avoid main thread hanging
                    Thread CloseDown2 = new Thread(new ThreadStart(CloseSerialPortArduinoOnExit));
                    CloseDown2.Start();
                }
                catch
                {
                }
            }
        }

        bool CheckSum(string msg)
        {
            // Check the checksum

            // Length of the GPS message
            int len = msg.Length;

            // Message is too small
            if (len < 5) return false;

            // Does it contain the checksum, to check
            if (msg[len - 4] == '*')
            {
                // Read the checksum from the message
                int cksum = 16 * Hex2Dec(msg[len - 3]) + Hex2Dec(msg[len - 2]);

                // Loop over message characters
                for (int i = 0; i < len - 4; i++)
                {
                    cksum ^= msg[i];
                }

                // The final result should be zero
                if (cksum == 0)
                {
                    return true;
                }
            }

            return false;
        }

        // Convert HEX to DEC
        int Hex2Dec(char c)
        {

            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }
            else if (c >= 'A' && c <= 'F')
            {
                return (c - 'A') + 10;
            }
            else
            {
                return 0;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // If the CNC is not doing anything
            if (CNCstatus == "Idle")
            {
                // Move the head if required
                MoveCNC();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // If the com port has been closed, do nothing
            if (!SerialPortCNC.IsOpen) return;
            
            // Send Serial command
            SerialPortCNC.Write("?\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "?     "));
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            // Send Serial command to get status of the CNC
            SerialPortCNC.Write("?\n");
        }
    }
}
