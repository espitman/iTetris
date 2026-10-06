using System;
using System.Collections.Generic;

namespace iTetris.Core
{
    public sealed class CrystalBreakerRules
    {
        public const float Wall = 11.85f, Ceiling = 6.7f, PaddleY = -6f, Radius = .27f;
        public const float BrickHalfWidth = 1.04f, BrickHalfHeight = .42f;
        public sealed class Ball { public float X, Y, VX, VY; }
        public sealed class Brick { public float X, Y; public int HP, Color; public int MaxHP = 1; }
        public sealed class Gift { public float X, Y; public bool Wide; }
        public readonly List<Ball> Balls = new List<Ball>();
        public readonly List<Brick> Bricks = new List<Brick>();
        public readonly List<Gift> Gifts = new List<Gift>();
        public int Score, Stage = 1, Lives = 3, Combo;
        public bool Waiting = true, Over, Won;
        public float Paddle, WideTime;
        public float HalfWidth => WideTime > 0 ? 4.3f : 3.3f;
        public event Action<float, float, int> Broken;
        int destroyed;

        public CrystalBreakerRules() { LoadStage(); }
        public void SetPaddle(float x)
        {
            Paddle = Math.Clamp(x, -Wall + HalfWidth, Wall - HalfWidth);
            if (Waiting && Balls.Count > 0) { Balls[0].X = Paddle; Balls[0].Y = PaddleY + .65f; }
        }
        public void Launch()
        {
            if (!Waiting || Over) return;
            Waiting = false; Balls[0].VX = 3; Balls[0].VY = 7 + Stage;
        }
        void LoadStage()
        {
            Bricks.Clear(); Gifts.Clear(); WideTime = 0; Combo = 0;
            int[,] colors = { {0,2,0,2,1,3,2,0}, {1,0,1,3,0,1,0,1}, {3,2,0,0,1,2,3,0}, {0,1,3,2,3,0,0,2} };
            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 8; col++)
                {
                    if ((row == 1 && (col == 1 || col == 5)) || (row == 2 && (col == 2 || col == 6)) || (row == 3 && col == 3)) continue;
                    float offset = row == 1 ? -.22f : row == 2 ? .1f : row == 3 ? -.12f : 0;
                    Bricks.Add(new Brick { X = (col - 3.5f) * 2.3f + offset, Y = 5.2f - row * 1.02f,
                        MaxHP = Stage > 2 && row == 0 ? 2 : 1, HP = Stage > 2 && row == 0 ? 2 : 1, Color = (colors[row,col] + (Stage-1)/3) % 4 });
                }
            SetPaddle(Paddle); Serve();
        }
        void Serve() { Balls.Clear(); Balls.Add(new Ball { X = Paddle, Y = PaddleY + .65f }); Waiting = true; Combo = 0; }
        public void Power(bool wide)
        {
            if (wide) { WideTime = 15; SetPaddle(Paddle); }
            else
            {
                if (Waiting) Launch();
                int count = Math.Min(3, Balls.Count);
                for (int i = 0; i < count && Balls.Count < 6; i++)
                {
                    var b = Balls[i];
                    Balls.Add(new Ball { X = b.X, Y = b.Y, VX = -b.VY * .6f, VY = Math.Abs(b.VY) * .8f });
                }
            }
        }
        public void Tick(float dt)
        {
            if (Over || Waiting) return;
            dt = Math.Clamp(dt, 0, .1f);
            int steps = (int)Math.Ceiling(dt / .003f);
            float s = dt / Math.Max(1, steps);
            for (int n = 0; n < steps; n++) Step(s);
        }
        void Step(float dt)
        {
            if (Over || Waiting) return;
            WideTime = Math.Max(0, WideTime - dt); SetPaddle(Paddle);
            for (int i = Gifts.Count - 1; i >= 0; i--)
            {
                var g = Gifts[i]; g.Y -= 3 * dt;
                if (g.Y <= PaddleY + .35f && g.Y >= PaddleY - .35f && Math.Abs(g.X - Paddle) < HalfWidth + .3f)
                { Power(g.Wide); Gifts.RemoveAt(i); }
                else if (g.Y < -8) Gifts.RemoveAt(i);
            }
            for (int k = Balls.Count - 1; k >= 0; k--)
            {
                var b = Balls[k]; float oldX = b.X, oldY = b.Y;
                b.X += b.VX * dt; b.Y += b.VY * dt;
                if (Math.Abs(b.X) > Wall - Radius) { b.X = Math.Clamp(b.X, -Wall + Radius, Wall - Radius); b.VX = -b.VX; }
                if (b.Y > Ceiling - Radius) { b.Y = Ceiling - Radius; b.VY = -Math.Abs(b.VY); }
                float contact = PaddleY + .35f + Radius;
                if (b.VY < 0 && oldY >= contact && b.Y <= contact && Math.Abs(b.X - Paddle) <= HalfWidth + Radius)
                {
                    float t = Math.Clamp((b.X - Paddle) / HalfWidth, -1, 1), speed = 8 + Stage;
                    b.Y = contact; b.VX = t * speed * .82f; b.VY = (float)Math.Sqrt(speed * speed - b.VX * b.VX);
                    Combo = 0;
                }
                for (int j = Bricks.Count - 1; j >= 0; j--)
                {
                    var brick = Bricks[j];
                    if (Math.Abs(b.X - brick.X) > BrickHalfWidth + Radius || Math.Abs(b.Y - brick.Y) > BrickHalfHeight + Radius) continue;
                    if (Math.Abs(oldX - brick.X) >= BrickHalfWidth + Radius) { b.X = oldX; b.VX = -b.VX; }
                    else { b.Y = oldY; b.VY = -b.VY; }
                    if (--brick.HP == 0)
                    {
                        Bricks.RemoveAt(j); Combo++; Score += 100 * Stage * Math.Min(Combo, 4); destroyed++;
                        Broken?.Invoke(brick.X, brick.Y, brick.Color);
                        if (destroyed % 9 == 0) Gifts.Add(new Gift { X = brick.X, Y = brick.Y, Wide = (destroyed / 9) % 2 == 1 });
                    }
                    break;
                }
                if (b.Y < -8) Balls.RemoveAt(k);
            }
            if (Bricks.Count == 0)
            {
                if (Stage == 5) { Over = true; Won = true; }
                else { Stage++; LoadStage(); }
                return;
            }
            if (Balls.Count == 0) { Lives--; Gifts.Clear(); if (Lives == 0) Over = true; else Serve(); }
        }
    }
}
