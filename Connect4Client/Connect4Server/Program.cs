using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Connect4Server
{
    class Program
    {
        private static TcpListener listener;
        private static TcpClient player1;
        private static TcpClient player2;
        private static string[,] grid = new string[6, 7];  // Game grid
        private static bool isPlayer1Turn = true;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting Connect Four Server...");
            listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();

            Console.WriteLine("Waiting for Player 1 to connect...");
            player1 = listener.AcceptTcpClient();
            SendPlayerColor(player1, "Red");
            Console.WriteLine("Player 1 (Red) connected!");

            Console.WriteLine("Waiting for Player 2 to connect...");
            player2 = listener.AcceptTcpClient();
            SendPlayerColor(player2, "Yellow");
            Console.WriteLine("Player 2 (Yellow) connected!");

            // Start threads for handling players
            Thread player1Thread = new Thread(() => HandlePlayer(player1, "Red"));
            player1Thread.Start();

            Thread player2Thread = new Thread(() => HandlePlayer(player2, "Yellow"));
            player2Thread.Start();
        }

        // Send the player's color when they connect
        private static void SendPlayerColor(TcpClient client, string color)
        {
            NetworkStream stream = client.GetStream();
            byte[] message = Encoding.ASCII.GetBytes(color);
            stream.Write(message, 0, message.Length);
        }

        // Handle player moves
        private static void HandlePlayer(TcpClient player, string playerColor)
        {
            NetworkStream stream = player.GetStream();
            byte[] buffer = new byte[256];

            while (true)
            {
                try
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    if (message.StartsWith("MOVE"))
                    {
                        int column = int.Parse(message.Split(' ')[1]);
                        ProcessMove(column, playerColor);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error with {playerColor}: " + ex.Message);
                    break;
                }
            }
        }

        // Process the player's move, update the grid, and notify both players
        private static void ProcessMove(int column, string playerColor)
        {
            // Find the first available row in the column
            for (int row = 5; row >= 0; row--)
            {
                if (grid[row, column] == null)
                {
                    grid[row, column] = playerColor;

                    // Send grid update to both players
                    BroadcastGridUpdate(row, column, playerColor);

                    // Switch turns and notify players whose turn it is
                    isPlayer1Turn = !isPlayer1Turn;
                    NotifyTurn();

                    return;
                }
            }

            // If the column is full, do nothing (you may want to add error handling)
        }

        // Send the updated grid to both players
        private static void BroadcastGridUpdate(int row, int col, string color)
        {
            string message = $"GRID_UPDATE {row} {col} {color}";
            byte[] messageBytes = Encoding.ASCII.GetBytes(message);

            try
            {
                player1.GetStream().Write(messageBytes, 0, messageBytes.Length);
                player2.GetStream().Write(messageBytes, 0, messageBytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error broadcasting grid update: " + ex.Message);
            }
        }

        // Notify both players whose turn it is
        private static void NotifyTurn()
        {
            string currentTurnColor = isPlayer1Turn ? "Red" : "Yellow";
            string message = $"TURN {currentTurnColor}";
            byte[] messageBytes = Encoding.ASCII.GetBytes(message);

            try
            {
                player1.GetStream().Write(messageBytes, 0, messageBytes.Length);
                player2.GetStream().Write(messageBytes, 0, messageBytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error notifying turn: " + ex.Message);
            }
        }
    }
}
