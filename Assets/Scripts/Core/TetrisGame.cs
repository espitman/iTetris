using System;
using System.Collections.Generic;

namespace iTetris.Core
{
    public enum PieceKind { I, J, L, O, S, T, Z }
    public readonly struct Cell
    {
        public readonly int X, Y;
        public Cell(int x, int y) { X = x; Y = y; }
    }
    public sealed class LockResult
    {
        public Cell[] Cells;
        public PieceKind Kind;
        public int[] ClearedRows;
        public int ScoreGained;
        public bool TSpin, BackToBack, PerfectClear;
        public int Combo;
    }
    // Pure C# rules: rendering and input never modify the settled board directly.
    public sealed class TetrisGame
    {
        public const int Width = 10, Height = 22, VisibleHeight = 20;
        public readonly int[,] Board = new int[Width, Height];
        public PieceKind Active { get; private set; }
        public PieceKind? Held { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Rotation { get; private set; }
        public int Score { get; private set; }
        public int Lines { get; private set; }
        public int Level => 1 + Lines / 10;
        public bool GameOver { get; private set; }
        public bool HoldUsed { get; private set; }
        public bool Grounded => !Fits(Active, X, Y - 1, Rotation);
        public float FallInterval => Math.Max(.045f, (float)Math.Pow(.8 - (Math.Min(Level,20) - 1) * .007, Math.Min(Level,20) - 1));
        public event Action<LockResult> Locked;
        public event Action Changed;
        public event Action Ended;
        readonly Queue<PieceKind> queue = new Queue<PieceKind>();
        readonly Random random;
        float gravity, lockTime;
        int resets, combo = -1;
        bool lastRotate, backToBack;
        public TetrisGame(int seed = -1) { random = seed < 0 ? new Random() : new Random(seed); Restart(); }

        static readonly Cell[][] SpawnCells = {
            new[] { new Cell(0,2), new Cell(1,2), new Cell(2,2), new Cell(3,2) },
            new[] { new Cell(0,2), new Cell(0,1), new Cell(1,1), new Cell(2,1) },
            new[] { new Cell(2,2), new Cell(0,1), new Cell(1,1), new Cell(2,1) },
            new[] { new Cell(1,2), new Cell(2,2), new Cell(1,1), new Cell(2,1) },
            new[] { new Cell(1,2), new Cell(2,2), new Cell(0,1), new Cell(1,1) },
            new[] { new Cell(1,2), new Cell(0,1), new Cell(1,1), new Cell(2,1) },
            new[] { new Cell(0,2), new Cell(1,2), new Cell(1,1), new Cell(2,1) }
        };
        public static Cell[] Shape(PieceKind kind, int rotation = 0)
        {
            var cells = new Cell[4];
            for (int i = 0; i < 4; i++)
            {
                int x = SpawnCells[(int)kind][i].X, y = SpawnCells[(int)kind][i].Y;
                for (int r = 0; r < (rotation & 3) && kind != PieceKind.O; r++)
                { int oldX = x; x = y; y = (kind == PieceKind.I ? 3 : 2) - oldX; }
                cells[i] = new Cell(x, y);
            }
            return cells;
        }
        public Cell[] ActiveCells(int? atY = null)
        {
            var cells = Shape(Active, Rotation);
            for (int i = 0; i < 4; i++) cells[i] = new Cell(cells[i].X + X, cells[i].Y + (atY ?? Y));
            return cells;
        }
        public PieceKind[] Next(int count = 3)
        {
            Refill(); var all = queue.ToArray(); var result = new PieceKind[Math.Min(count, all.Length)];
            Array.Copy(all, result, result.Length); return result;
        }
        void Refill()
        {
            while (queue.Count < 7)
            {
                var bag = new[] { PieceKind.I, PieceKind.J, PieceKind.L, PieceKind.O, PieceKind.S, PieceKind.T, PieceKind.Z };
                for (int i = 6; i > 0; i--) { int j = random.Next(i + 1); var t = bag[i]; bag[i] = bag[j]; bag[j] = t; }
                foreach (var p in bag) queue.Enqueue(p);
            }
        }
        public void Restart()
        {
            Array.Clear(Board, 0, Board.Length); queue.Clear(); Held = null; HoldUsed = false;
            Score = Lines = 0; GameOver = false; combo = -1; backToBack = false; Refill(); Spawn(queue.Dequeue());
        }
        void Spawn(PieceKind kind)
        {
            Active = kind; X = 3; Y = 17; Rotation = 0; gravity = lockTime = 0; resets = 0; lastRotate = false;
            Refill();
            if (!Fits(Active, X, Y, Rotation)) { GameOver = true; Ended?.Invoke(); }
            Changed?.Invoke();
        }
        public bool Fits(PieceKind kind, int x, int y, int rotation)
        {
            foreach (var c in Shape(kind, rotation))
            {
                int px = x + c.X, py = y + c.Y;
                if (px < 0 || px >= Width || py < 0 || py >= Height || Board[px, py] != 0) return false;
            }
            return true;
        }
        void ResetLock(bool wasGrounded)
        {
            if (wasGrounded && resets < 15) { lockTime = 0; resets++; }
        }
        public bool Move(int dx)
        {
            if (GameOver || !Fits(Active, X + dx, Y, Rotation)) return false;
            bool grounded = Grounded; X += dx; lastRotate = false; ResetLock(grounded); Changed?.Invoke(); return true;
        }
        static Cell[] KickTests(PieceKind kind, int from, int to)
        {
            // SRS offsets expressed with positive Y upwards.
            int key = from * 4 + to;
            if (kind == PieceKind.I)
            {
                switch (key)
                {
                    case 1: case 14: return K(0,0,-2,0,1,0,-2,-1,1,2);
                    case 4: case 11: return K(0,0,2,0,-1,0,2,1,-1,-2);
                    case 6: case 3: return K(0,0,-1,0,2,0,-1,2,2,-1);
                    case 9: case 12: return K(0,0,1,0,-2,0,1,-2,-2,1);
                }
            }
            switch (key)
            {
                case 1: case 9: return K(0,0,-1,0,-1,1,0,-2,-1,-2);
                case 4: case 6: return K(0,0,1,0,1,-1,0,2,1,2);
                case 11: case 3: return K(0,0,1,0,1,1,0,-2,1,-2);
                case 14: case 12: return K(0,0,-1,0,-1,-1,0,2,-1,2);
            }
            return K(0,0);
        }
        static Cell[] K(params int[] values)
        {
            var result = new Cell[values.Length / 2];
            for (int i = 0; i < result.Length; i++) result[i] = new Cell(values[i * 2], values[i * 2 + 1]);
            return result;
        }
        public bool Rotate(int direction)
        {
            if (GameOver) return false;
            int to = (Rotation + direction + 4) & 3;
            foreach (var offset in KickTests(Active, Rotation, to))
            {
                if (!Fits(Active, X + offset.X, Y + offset.Y, to)) continue;
                bool grounded = Grounded; X += offset.X; Y += offset.Y; Rotation = to;
                lastRotate = true; ResetLock(grounded); Changed?.Invoke(); return true;
            }
            return false;
        }
        public int GhostY()
        { int y = Y; while (Fits(Active, X, y - 1, Rotation)) y--; return y; }
        public bool SoftDrop()
        {
            if (GameOver || Grounded) return false;
            Y--; Score++; gravity = 0; lastRotate = false; Changed?.Invoke(); return true;
        }
        public void HardDrop()
        {
            if (GameOver) return;
            int target = GhostY(); Score += (Y - target) * 2;
            if (target != Y) lastRotate = false;
            Y = target; Lock();
        }
        public bool Hold()
        {
            if (GameOver || HoldUsed) return false;
            var old = Active;
            if (Held.HasValue) Spawn(Held.Value); else Spawn(queue.Dequeue());
            Held = old; HoldUsed = true; Changed?.Invoke(); return true;
        }
        public void Tick(float delta)
        {
            if (GameOver) return;
            delta = Math.Min(delta, .1f); gravity += delta;
            while (gravity >= FallInterval && !Grounded)
            { gravity -= FallInterval; Y--; Changed?.Invoke(); }
            if (Grounded) { lockTime += delta; if (lockTime >= .5f) Lock(); }
            else lockTime = 0;
        }
        bool Occupied(int x, int y) => x < 0 || x >= Width || y < 0 || y >= Height || Board[x,y] != 0;
        bool IsTSpin()
        {
            if (Active != PieceKind.T || !lastRotate) return false;
            int count = 0;
            foreach (var c in K(0,0,2,0,0,2,2,2)) if (Occupied(X+c.X, Y+c.Y)) count++;
            return count >= 3;
        }
        void Lock()
        {
            var cells = ActiveCells(); bool tSpin = IsTSpin();
            foreach (var c in cells) Board[c.X, c.Y] = (int)Active + 1;
            var cleared = new List<int>();
            for (int y = 0; y < Height; y++)
            { bool full = true; for (int x = 0; x < Width; x++) if (Board[x,y] == 0) { full = false; break; } if (full) cleared.Add(y); }
            int write = 0;
            for (int y = 0; y < Height; y++)
            {
                if (cleared.Contains(y)) continue;
                for (int x = 0; x < Width; x++) Board[x,write] = Board[x,y];
                write++;
            }
            for (int y = write; y < Height; y++) for (int x = 0; x < Width; x++) Board[x,y] = 0;
            int n = cleared.Count, gain = 0;
            bool difficult = n == 4 || (tSpin && n > 0), b2bBonus = difficult && backToBack;
            if (n > 0)
            {
                combo++;
                gain = (tSpin ? new[] {400,800,1200,1600}[Math.Min(n,3)] : new[] {0,100,300,500,800}[n]) * Level;
                if (b2bBonus) gain = gain * 3 / 2;
                gain += Math.Max(0, combo) * 50 * Level;
                backToBack = difficult;
            }
            else { combo = -1; if (tSpin) gain = 400 * Level; }
            bool perfect = n > 0;
            for (int y = 0; y < Height && perfect; y++) for (int x = 0; x < Width; x++) if (Board[x,y] != 0) perfect = false;
            if (perfect) gain += 2000 * Level;
            Score += gain; Lines += n;
            var result = new LockResult { Cells = cells, Kind = Active, ClearedRows = cleared.ToArray(), ScoreGained = gain, TSpin = tSpin, BackToBack = b2bBonus, PerfectClear = perfect, Combo = combo };
            Locked?.Invoke(result);
            bool topOut = false;
            for (int y = VisibleHeight; y < Height; y++) for (int x = 0; x < Width; x++) if (Board[x,y] != 0) topOut = true;
            if (topOut) { GameOver = true; Changed?.Invoke(); Ended?.Invoke(); return; }
            HoldUsed = false; Spawn(queue.Dequeue());
        }
    }
}
