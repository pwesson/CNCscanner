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
        public static SerialPort CNCscanner = new SerialPort();

        private String CNCserialMessage = "";

        public FormMain()
        {
            InitializeComponent();

            // Setup message handler for the main form
            GlobalEventMessages obj1 = new GlobalEventMessages();
            GlobalEventMessages.TheEvent += new GlobalEventHandler(ShowOnScreen);

            // When Serial data is recieved through the CNCscanner port, call this method
            CNCscanner.DataReceived += new SerialDataReceivedEventHandler(SerialDataReceived);

            //Setup the SerialPorts List
            foreach (string port in SerialPort.GetPortNames())
            {
                // Add the next port to the list
                comboBoxPorts.Items.Add(port);

                // Select this last found port
                comboBoxPorts.Text = port;
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
            if (CNCscanner.IsOpen)
            {
                try
                {
                    // Close in another thread to avoid main thread hanging
                    Thread CloseDown = new Thread(new ThreadStart(CloseSerialOnExit));
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

                CNCscanner.BaudRate = 115200;                                           // BaudRate
                CNCscanner.DataBits = 8;                                                // DataBits
                CNCscanner.StopBits = (StopBits)Enum.Parse(typeof(StopBits), "One");    // StopBits
                CNCscanner.Parity = (Parity)Enum.Parse(typeof(Parity), "None");         // Parity
                CNCscanner.PortName = comboBoxPorts.Text;                               // Port Number

                try
                {
                    // Open the Serial port
                    CNCscanner.Open();

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

        private void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // If the com port has been closed, do nothing
                if (!CNCscanner.IsOpen) return;

                // Read the Serial Port
                string data = CNCscanner.ReadExisting();

                // Add to messages received so far. 
                // This is ok for small UAT as it grows in size
                CNCserialMessage += data;

                // Send to mainform to show
                GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBox2", CNCserialMessage));
            }
            catch (Exception ex)
            {
                // Update User with error message
                MessageBox.Show(ex.Message);
            }
        }

        private static void CloseSerialOnExit()
        {
            try
            {
                // Close the serial port
                CNCscanner.Close();
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
            if (!CNCscanner.IsOpen)
            {
                return;
            }

            // Send Serial command
            CNCscanner.Write("$J=G91X1.0F1917\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBox1", "$J = G91X1.0F1917\n"));
        }

        private void btnWest_Click(object sender, EventArgs e)
        {
            // Want to send command to CNCscanner
            // Move West 1.0 points
            // $J=G91X-1.0F1917

            // If the com port has been closed, do nothing
            if (!CNCscanner.IsOpen)
            {
                return;
            }

            // Send Serial command
            CNCscanner.Write("$J=G91X-1.0F1917\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBox1", "$J = G91X-1.0F1917\n"));
        }

        private void btnNorth_Click(object sender, EventArgs e)
        {
            // Want to send command to CNCscanner
            // Move North 1.0 points
            // $J=G91Y1.0F1917

            // If the com port has been closed, do nothing
            if (!CNCscanner.IsOpen)
            {
                return;
            }

            // Send Serial command
            CNCscanner.Write("$J=G91Y1.0F1917\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBox1", "$J = G91Y1.0F1917\n"));
        }

        private void btnSouth_Click(object sender, EventArgs e)
        {
            // Want to send command to CNCscanner
            // Move South 1.0 points
            // $J=G91Y-1.0F1917

            // If the com port has been closed, do nothing
            if (!CNCscanner.IsOpen)
            {
                return;
            }

            // Send Serial command
            CNCscanner.Write("$J=G91Y-1.0F1917\n");

            // Send to mainform to show
            GlobalEventMessages.OnGlobalEvent(new GlobalEventArgs("textBox1", "$J = G91Y-1.0F1917\n"));
        }
    }
}
