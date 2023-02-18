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
        bool isProgramRunning = false;

        private int xMin = 10;
        private int xStep = 10;
        private int xMax = 400;
        private int yMin = 10;
        private int yStep = 10;
        private int yMax = 1050;
        private int xPosition = 0;
        private int yPosition = 0;

        string distanceMM = "0";
        string building = "";

        public static SerialPort SerialPortCNC = new SerialPort();
        public static SerialPort SerialPortArduino = new SerialPort();

        private String SerialMessageCNC = "";

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
            
            // If the butter is not null
            if (_buffer != null)
            {
                // Add to messages received so far. 
                // This is ok for small UAT as it grows in size
                SerialMessageCNC = encoding.GetString(_buffer);

                if (SerialMessageCNC.Contains("<Home|")) { MessageBox.Show("Home");}
                if (SerialMessageCNC.Contains("<Idle|")) { MessageBox.Show("Idle"); }
                if (SerialMessageCNC.Contains("<Jog|")) { MessageBox.Show("Jog"); }
                if (SerialMessageCNC.Contains("MPos:"))
                {
                    int xpos = SerialMessageCNC.IndexOf("MPos:", 0) +5;
                    int ypos = SerialMessageCNC.IndexOf(",", xpos);
                    int yend = SerialMessageCNC.IndexOf(",", ypos+1);
                    string xPos = SerialMessageCNC.Substring(xpos, ypos - xpos);
                    string yPos = SerialMessageCNC.Substring(ypos+1, yend - ypos -1);
                }

                // Send to mainform to show
                GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", SerialMessageCNC));
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
                        distanceMM = building + '\0';

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
                // Pause for 2 seconds. Wait for head to move to new location
                //DateTime _desired = DateTime.Now.AddSeconds(2);
                //while (DateTime.Now < _desired)
                //{
                //    System.Windows.Forms.Application.DoEvents();
                //}

                // Send to mainform to show
                GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", toolStripStatusLabel1.Text));

                // Save the current reading to disk
                myFile.Write(xPosition.ToString() + "," + yPosition.ToString() + "," + toolStripStatusLabel1.Text + "\n");
                myFile.Flush();

                if (xPosition == xMin && yPosition == yMin)
                {
                    xPosition += xStep;

                    // Create the CNCscanner instruction
                    string locInstruction = "$J=G90X" + xPosition.ToString() + "Y" + yPosition.ToString() + "F1917\n";

                    // Send Serial command
                    SerialPortCNC.Write(locInstruction);

                }
                else
                {
                    // Increase the position
                    if (xPosition == xMax)
                    {
                        // Keep xPosition fixed
                        xPosition = xMin;
                        yPosition += yStep;

                        // Create the CNCscanner instruction
                        string locInstruction = "$J=G90X" + xPosition.ToString() + "Y" + yPosition.ToString() + "F1917\n";

                        // Send Serial command
                        SerialPortCNC.Write(locInstruction);

                        // Pause for 8 seconds. Wait for head to move to new location
                        //_desired = DateTime.Now.AddSeconds(8);
                        //while (DateTime.Now < _desired)
                        //{
                        //    System.Windows.Forms.Application.DoEvents();
                        //}
                    }
                    else
                    {
                        // Normal horizontal update
                        xPosition += xStep;

                        // Create the CNCscanner instruction
                        string locInstruction = "$J=G90X" + xPosition.ToString() + "Y" + yPosition.ToString() + "F1917\n";

                        // Send Serial command
                        SerialPortCNC.Write(locInstruction);

                    }
                }

                if (yPosition > yMax)
                {
                    MessageBox.Show("Finished");
                    isProgramRunning = false;
                    return;
                }

                // Send to mainform to show
                GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "$J=G90X" + xPosition.ToString() + "Y" + yPosition.ToString() + "F1917     "));
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
                if (e.sCont == "btnProgram") { btnConnect.Invoke(new MethodInvoker(delegate { btnProgram.Text = e.sMsg; })); }

                if (e.sCont == "textBoxMessages") { textBoxMessages.Invoke(new MethodInvoker(delegate { textBoxMessages.AppendText(e.sMsg + '\n'); })); }
                if (e.sCont == "toolStripStatusLabel1") { toolStripStatusLabel1.Text = e.sMsg; }
            }
            catch
            {

            }

            Application.DoEvents();
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
            SerialPortCNC.Write("$J=G91X20F1917\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "$J = G91X20F1917     "));
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
            SerialPortCNC.Write("$J=G91X-20F1917\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "$J = G91X-20F1917     "));
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
            SerialPortCNC.Write("$J=G91Y20F1917\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "$J = G91Y20F1917     "));
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
            SerialPortCNC.Write("$J=G91Y-20F1917\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "$J = G91Y-20F1917     "));
        }


        private void btnHome_Click(object sender, EventArgs e)
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
            SerialPortCNC.Write("$H\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "$H     "));
        }

        private void btnFarAway_Click(object sender, EventArgs e)
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
            SerialPortCNC.Write("$J=G90X390Y370F1917\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBoxMessages", "$J=G90X390Y370F1917     "));
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

        private void button1_Click(object sender, EventArgs e)
        {
            // If the com port has been closed, do nothing
            if (!SerialPortArduino.IsOpen)
            {
                return;
            }

            // Take a reading
            // Send Serial command
            SerialPortArduino.Write("r");
        }

        private void btnProgram_Click(object sender, EventArgs e)
        {
            // This is the program
            isProgramRunning = true;

            // Turn on timer
            if (timer1.Enabled)
            {
                // Turn off timer
                timer1.Enabled = false;

                // Change the button text to "disconnect"
                GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("btnProgram", "Program"));
            }
            else
            {
                // Turn on timer
                timer1.Enabled = true;

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
                string locInstruction = "$J=G90X" + xPosition.ToString() + "Y" + yPosition.ToString() + "F1917\n";

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
            // Move the head if required
            MoveCNC();
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
    }
}
