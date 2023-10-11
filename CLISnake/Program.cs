using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Timer = System.Timers.Timer;

namespace CLISnake;
// Note: actual namespace depends on the project name.

internal class Program
{
    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(nint hConsoleHandle, uint dwMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle(int nStdHandle);

    private const int StdOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 4;

    private static ConsoleKeyInfo _bufferedInput =
        new((char)ConsoleKey.RightArrow, ConsoleKey.RightArrow, false, false, false);

    private static int _eventualSnakeLength;
    private static readonly Random Random = new();
    private static readonly BlockType[,] Board = new BlockType[BoardWidth, BoardHeight];
    private static List<(int X, int Y)> _snake = new();
    private static bool _playerIsAlive = true;
    private static int _lagCounter;

    private const int BoardWidth = 20;
    private const int BoardHeight = 15;
    private const int GameSpeed = 70;
    private const int AppleWorth = 5;

    private static readonly string[] Blocks =
    {
        "[37;44m  ", // Empty (Blue)
        "[37;42m  ", // Snake (Green)
        "[37;41m  ", // Apple (Red)
        "[37;43m  ", // Snake head (Yellow)
        "[37;40m\n" // End of line
    };

    private enum BlockType
    {
        Empty,
        Snake,
        Apple,
        SnakeHead
    }

    private static void EnableANSI()
    {
        nint handle = GetStdHandle(StdOutputHandle);
        GetConsoleMode(handle, out uint mode);
        mode |= EnableVirtualTerminalProcessing;
        SetConsoleMode(handle, mode);
    }

    public static void Main()
    {
        EnableANSI();

        Console.CursorVisible = false;
        Console.WriteLine("Press any key to start...");
        Console.ReadKey(true);
        Console.SetCursorPosition(0, 0);
        Stopwatch s = new();

        while (true)
        {
            ResetGame();
            while (_playerIsAlive)
            {
                s.Start();
                if (Console.KeyAvailable)
                    _bufferedInput = Console.ReadKey(true);

                UpdateBoard();
                RenderBoard();
                s.Stop();
                _lagCounter = (int)s.ElapsedMilliseconds;
                s.Reset();
                Thread.Sleep(Math.Max(0, GameSpeed - _lagCounter));
            }

            Console.SetCursorPosition(0, 0);
            Console.WriteLine("Game Over!");
            Console.WriteLine($"Score: {_eventualSnakeLength}");
            Console.WriteLine($"Length: {_snake.Count}");
            Console.WriteLine("Press any key to restart...");
            Thread.Sleep(200); // Give the user time to stop pressing keys
            while(Console.KeyAvailable) 
                Console.ReadKey(true); // Flush input buffer
            Console.ReadKey(true);
        }
    }

    private static void ResetGame()
    {
        for (int y = 0; y < BoardHeight; y++)
        for (int x = 0; x < BoardWidth; x++)
            Board[x, y] = BlockType.Empty;

        _snake = new() { (BoardWidth / 2, BoardHeight / 2) };
        _eventualSnakeLength = 1;
        
        for (int i = 0; i < _snake.Count; i++)
            Board[_snake[i].X, _snake[i].Y] = BlockType.Snake;
        
        GenerateApple();

        _playerIsAlive = true;
    }

    private static void RenderBoard()
    {
        StringBuilder board = new();
        for (int y = 0; y < BoardHeight; y++)
        {
            for (int x = 0; x < BoardWidth; x++)
                board.Append(Blocks[(int)Board[x, y]]);

            board.Append(Blocks[4]);
        }

        string infoText = $"<<Score: {_eventualSnakeLength} | Length: {_snake.Count} | Lag: {_lagCounter}>>";
        infoText = infoText.PadLeft(infoText.Length + (BoardWidth - infoText.Length) / 2);
        Console.SetCursorPosition(0, 0);
        board.Append(infoText);
        Console.WriteLine(board.ToString());
    }

    private static void UpdateBoard()
    {
        switch (_bufferedInput.Key)
        {
            case ConsoleKey.UpArrow or ConsoleKey.W:
                if (_snake.Count <= 1)
                    _snake.Add((_snake[^1].X, _snake[^1].Y - 1));
                else if (_snake[^2].Y != _snake[^1].Y - 1)
                    _snake.Add((_snake[^1].X, _snake[^1].Y - 1));
                else
                    _snake.Add((_snake[^1].X, _snake[^1].Y + 1));
                break;
            case ConsoleKey.DownArrow or ConsoleKey.S:
                if (_snake.Count <= 1)
                    _snake.Add((_snake[^1].X, _snake[^1].Y + 1));
                else if (_snake[^2].Y != _snake[^1].Y + 1)
                    _snake.Add((_snake[^1].X, _snake[^1].Y + 1));
                else
                    _snake.Add((_snake[^1].X, _snake[^1].Y - 1));
                break;
            case ConsoleKey.LeftArrow or ConsoleKey.A:
                if (_snake.Count <= 1)
                    _snake.Add((_snake[^1].X - 1, _snake[^1].Y));
                else if (_snake[^2].X != _snake[^1].X - 1)
                    _snake.Add((_snake[^1].X - 1, _snake[^1].Y));
                else
                    _snake.Add((_snake[^1].X + 1, _snake[^1].Y));
                break;
            case ConsoleKey.RightArrow or ConsoleKey.D:
                if (_snake.Count <= 1)
                    _snake.Add((_snake[^1].X + 1, _snake[^1].Y));
                else if (_snake[^2].X != _snake[^1].X + 1)
                    _snake.Add((_snake[^1].X + 1, _snake[^1].Y));
                else
                    _snake.Add((_snake[^1].X - 1, _snake[^1].Y));
                break;
        }

        if (_snake[^1].X is < 0 or >= BoardWidth || _snake[^1].Y is < 0 or >= BoardHeight)
        {
            _playerIsAlive = false;
            return;
        }

        if (Board[_snake[^1].X, _snake[^1].Y] == BlockType.Apple)
        {
            _eventualSnakeLength += AppleWorth;
            GenerateApple();
        }
        else if (_snake.Count > _eventualSnakeLength)
        {
            Board[_snake[0].X, _snake[0].Y] = BlockType.Empty;
            _snake.RemoveAt(0);
        }

        if (Board[_snake[^1].X, _snake[^1].Y] == BlockType.Snake)
        {
            _playerIsAlive = false;
            return;
        }

        for (int i = 0; i < _snake.Count; i++)
            Board[_snake[i].X, _snake[i].Y] = _snake.Count - 1 == i ? BlockType.SnakeHead : BlockType.Snake;
    }

    private static void GenerateApple()
    {
        while (true)
        {
            int x = Random.Next(0, BoardWidth);
            int y = Random.Next(0, BoardHeight);

            if (Board[x, y] == BlockType.Empty)
                Board[x, y] = BlockType.Apple;
            else
                continue;

            break;
        }
    }
}