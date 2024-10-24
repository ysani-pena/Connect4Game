using System;
using System.Drawing;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace Connect4Client
{
    public partial class Form1 : Form
    {
        private TcpClient client;
        private NetworkStream stream;
        private string[,] grid = new string[6, 7];  // Game grid (6 rows, 7 columns)
        private bool isMyTurn = false;  // Keep track of whose turn it is
        private string myPlayerColor = "";  // Either "Red" or "Yellow"
        private PictureBox[,] gridPictureBoxes = new PictureBox[6, 7];

        public Form1()
        {
            InitializeComponent();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ConnectToServer();

            gridPictureBoxes[0, 0] = pb_00;
            gridPictureBoxes[0, 1] = pb_01;
            gridPictureBoxes[0, 2] = pb_02;
            gridPictureBoxes[0, 3] = pb_03;
            gridPictureBoxes[0, 4] = pb_04;
            gridPictureBoxes[0, 5] = pb_05;
            gridPictureBoxes[0, 6] = pb_06;

            gridPictureBoxes[1, 0] = pb_10;
            gridPictureBoxes[1, 1] = pb_11;
            gridPictureBoxes[1, 2] = pb_12;
            gridPictureBoxes[1, 3] = pb_13;
            gridPictureBoxes[1, 4] = pb_14;
            gridPictureBoxes[1, 5] = pb_15;
            gridPictureBoxes[1, 6] = pb_16;

            gridPictureBoxes[2, 0] = pb_20;
            gridPictureBoxes[2, 1] = pb_21;
            gridPictureBoxes[2, 2] = pb_22;
            gridPictureBoxes[2, 3] = pb_23;
            gridPictureBoxes[2, 4] = pb_24;
            gridPictureBoxes[2, 5] = pb_25;
            gridPictureBoxes[2, 6] = pb_26;

            gridPictureBoxes[3, 0] = pb_30;
            gridPictureBoxes[3, 1] = pb_31;
            gridPictureBoxes[3, 2] = pb_32;
            gridPictureBoxes[3, 3] = pb_33;
            gridPictureBoxes[3, 4] = pb_34;
            gridPictureBoxes[3, 5] = pb_35;
            gridPictureBoxes[3, 6] = pb_36;

            gridPictureBoxes[4, 0] = pb_40;
            gridPictureBoxes[4, 1] = pb_41;
            gridPictureBoxes[4, 2] = pb_42;
            gridPictureBoxes[4, 3] = pb_43;
            gridPictureBoxes[4, 4] = pb_44;
            gridPictureBoxes[4, 5] = pb_45;
            gridPictureBoxes[4, 6] = pb_46;

            gridPictureBoxes[5, 0] = pb_50;
            gridPictureBoxes[5, 1] = pb_51;
            gridPictureBoxes[5, 2] = pb_52;
            gridPictureBoxes[5, 3] = pb_53;
            gridPictureBoxes[5, 4] = pb_54;
            gridPictureBoxes[5, 5] = pb_55;
            gridPictureBoxes[5, 6] = pb_56;


            // Start receiving updates from the server
            Thread receiveThread = new Thread(new ThreadStart(ReceiveUpdates));
            receiveThread.Start();
        }

        private void ConnectToServer()
        {
            try
            {
                // Connect to the server (replace IP and port with actual server details)
                client = new TcpClient("127.0.0.1", 5000);
                stream = client.GetStream();

                // You can add logic here to receive the player color assignment from the server
                // For example, receive whether you're Player 1 (Red) or Player 2 (Yellow)
                byte[] buffer = new byte[256];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                myPlayerColor = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();

                // Show player color
                lblPlayerColor.Text = "Your Color: " + myPlayerColor;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error connecting to server: " + ex.Message);
            }
        }

        private void ReceiveUpdates()
        {
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[256];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string serverMessage = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    // Here you will handle game updates from the server
                    // For example, update the grid and determine whose turn it is
                    if (serverMessage.StartsWith("GRID_UPDATE"))
                    {
                        // Parse the new grid state and update UI
                        Invoke((MethodInvoker)delegate
                        {
                            UpdateGridUI(serverMessage);
                        });
                    }
                    else if (serverMessage.StartsWith("TURN"))
                    {
                        // Update the turn indicator
                        isMyTurn = serverMessage.Contains(myPlayerColor);
                        Invoke((MethodInvoker)delegate
                        {
                            lblTurnIndicator.Text = isMyTurn ? "Your Turn!" : "Opponent's Turn";
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error receiving from server: " + ex.Message);
                    break;
                }
            }
        }

        private void UpdateGridUI(string serverMessage)
        {
            
            // Example serverMessage: "GRID_UPDATE row col Red"
            string[] parts = serverMessage.Split(' ');
            int row = int.Parse(parts[1]);
            int col = int.Parse(parts[2]);
            string color = parts[3];  // Either "Red" or "Yellow"

            // Update the PictureBox at the specified row and column
            if (color == "Red")
                gridPictureBoxes[row, col].BackColor = Color.Red;
            else if (color == "Yellow")
                gridPictureBoxes[row, col].BackColor = Color.Yellow;
        }

        private void ColumnButton_Click(object sender, EventArgs e)
        {
            if (!isMyTurn)
            {
                MessageBox.Show("It's not your turn!");
                return;
            }

            Button clickedButton = sender as Button;
            int column = int.Parse(clickedButton.Tag.ToString());  // Get the column number from the button's tag

            // Check if the column is full before sending the move
            bool columnIsFull = true;
            for (int row = 5; row >= 0; row--)
            {
                if (grid[row, column] == null)  // If there's an empty spot in the column
                {
                    columnIsFull = false;
                    break;
                }
            }

            if (columnIsFull)
            {
                MessageBox.Show("This column is full! Choose another column.");
                return;
            }

            // Send the move to the server
            SendMove(column);

            // Disable input temporarily while waiting for the server's response
            isMyTurn = false;
            lblTurnIndicator.Text = "Opponent's Turn";
        }


        private void SendMove(int column)
        {
            try
            {
                // Send the move (selected column) to the server
                byte[] moveData = System.Text.Encoding.ASCII.GetBytes("MOVE " + column);
                stream.Write(moveData, 0, moveData.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sending move: " + ex.Message);
            }
        }

        // Make sure to dispose of the stream and client properly when the form closes
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (stream != null)
                stream.Close();
            if (client != null)
                client.Close();
        }
    }
}
