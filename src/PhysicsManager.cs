using System;
using Zene.Structs;
using Zene.Graphics;
using Zene.Windowing;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Balls
{
    public class PhysicsManager
    {
        public PhysicsManager(Vector2 size)
        {
            SetFrameSize(size);
            _newPos = 0d;
            SetFrame(size, 0d);
        }
        
        public static double Gravity { get; set; } = 1000d;
        public static Vector2 MaxSize { get; set; } = (100d, 100d);
        
        public const double GridSize = 10d;
        private double _gsR = 1d / 10d;
        public double _gs = 10d;
        public Vector2 FrameSize { get; set; }
        private Vector2 _hfs;
        public Vector2 Centre => _bounds.Centre;
        public Grid _grid;
        
        private Box _bounds;
        private Vector2 _newPos;
        private List<Ball> _balls = new List<Ball>();
        
        internal Ball F => _balls[0];
        
        public double Time { get; private set; }
        public int Count => _balls.Count;
        
        public void SetFrameSize(Vector2 size) => FrameSize = size;
        private void SetFrame(Vector2 size, Vector2 pos)
        {
            // FrameSize = size;
            _bounds = new Box(pos, size);
            _hfs = size * 0.5d;
            Vector2 gs = size / GridSize;
            // return;
            
            if (gs.X >= MaxSize.X)
            {
                gs.X = MaxSize.X;
                _gs = size.X / gs.X;
                _gsR = 1d / _gs;
                gs.Y = size.Y * _gsR;
            }
            if (gs.Y >= MaxSize.Y)
            {
                gs.Y = MaxSize.Y;
                _gs = size.Y / gs.Y;
                _gsR = 1d / _gs;
                gs.X = size.X * _gsR;
            }
            
            Vector2I vi = new Vector2I(
                Math.Ceiling(gs.X),
                Math.Ceiling(gs.Y));
            if (vi == _grid?.Size) { return; }
            
            _grid = new Grid(vi);
        }
        public void SetBoundPos(Vector2 pos) => _newPos = pos;
        
        public void ApplyPhysics(double dt, int subStep)
        {
            double t = Core.Time;
            
            dt /= subStep;
            
            Vector2 changeS = (FrameSize - _bounds.Size) / subStep;
            Vector2 changeP = (_newPos - _bounds.Centre) / subStep;
            bool change = changeS != 0 || changeP != 0;
            
            for (int i = 0; i < subStep; i++)
            {
                if (change)
                {
                    SetFrame(_bounds.Size + changeS,
                        _bounds.Centre + changeP);
                }
                ApplyPhysics(dt);
            }
            
            Time = Core.Time - t;
        }
        private void ApplyPhysics(double dt)
        {
            _grid.Clear();
            PreCollisionPhsyics();
            CalculateCollisions();
            
            int l = _balls.Count;
            // Program.ParallelFor(l, 1, i =>
            // // Parallel.For(0, l, i =>
            // {
            //     Ball b = _balls[i];
            //     b.ApplyVerlet(dt);
            // });
            
            for (int i = 0; i < l; i++)
            {
                Ball b = _balls[i];
                b.ApplyVerlet(dt);
            }
        }
        
        private void PreCollisionPhsyics()
        {
            int l = _balls.Count;
            for (int i = 0; i < l; i++)
            {
                Ball b = _balls[i];
                
                // b.Acceleration = (0d, -Gravity);
                ClipToBounds(b);
                
                _grid.Add(b, GetGridLocation(b.Location));
            }
        }
        private void ClipToBounds(Ball b)
        {
            double r = b.Radius;
            Vector2 l = b.Location;
            
            if (l.X + r > _bounds.Right)
            {
                l.X = _bounds.Right - r;
            }
            if (l.X - r < _bounds.Left)
            {
                l.X = _bounds.Left + r;
            }
            if (l.Y + r > _bounds.Top)
            {
                l.Y = _bounds.Top - r;
            }
            if (l.Y - r < _bounds.Bottom)
            {
                l.Y = _bounds.Bottom + r;
            }
            
            if (l == b.Location) { return; }
            
            b.Location = l;
        }
        
        private unsafe void CalculateCollisions()
        {
            Vector2I size = _grid.Size;
            Program.ParallelFor(size.Y - 1, 1, y =>
            // Parallel.For(0, size.Y - 1, y =>
            {
                y = size.Y - 1 - y;
                ReadOnlySpan<Ball> a = new ReadOnlySpan<Ball>(),
                    b = _grid[0, y].AsSpan(), c,
                    d = new ReadOnlySpan<Ball>(),
                    e = _grid[0, y - 1].AsSpan(), f;//,
                    // g = new ReadOnlySpan<Ball>(),
                    // h = _grid[0, y - 2].AsSpan(), i;
                for (int x = 1; x < size.X; x++)
                {
                    c = _grid[x, y].AsSpan();
                    f = _grid[x, y - 1].AsSpan();
                    // i = _grid[x, y - 2].AsSpan();
                    
                    for (int j = 0; j < e.Length; j++)
                    {
                        Ball b1 = e[j];
                        
                        Iterate(b1, a);
                        Iterate(b1, b);
                        Iterate(b1, c);
                        Iterate(b1, d);
                        Iterate(b1, e);
                        Iterate(b1, f);
                        // Iterate(b1, g);
                        // Iterate(b1, h);
                        // Iterate(b1, i);
                    }
                    
                    a = b;
                    b = c;
                    d = e;
                    e = f;
                    // g = h;
                    // h = i;
                }
                
                for (int j = 0; j < e.Length; j++)
                {
                    Ball b1 = e[j];
                    
                    Iterate(b1, a);
                    Iterate(b1, b);
                    Iterate(b1, d);
                    Iterate(b1, e);
                    // Iterate(b1, g);
                    // Iterate(b1, h);
                }
            });
        }
        private void Iterate(Ball b1, Vector2I p)
        {
            if (!_grid.Contains(p)) { return; }
            Iterate(b1, _grid[p.X, p.Y].AsSpan());
        }
        private void Iterate(Ball b1, ReadOnlySpan<Ball> span)
        {
            for (int b = 0; b < span.Length; b++)
            {
                Ball b2 = span[b];
                
                if (b1 == b2) { continue; }
                
                ResolveCollision(b1, b2);
            }
        }
        private void ResolveCollision(Ball a, Ball b)
        {
            double sumRadius = a.Radius + b.Radius;
            Vector2 axis = a.Location - b.Location;
            double dist = axis.SquaredLength;
            
            if (dist >= (sumRadius * sumRadius)) { return; }
            
            if (dist == 0)
            {
                axis = (a.Velocity - b.Velocity).Normalised();
            }
            else
            {
                dist = Math.Sqrt(dist);
                axis /= dist;
            }
            double diff = dist - sumRadius;
            double scale = diff * 0.5;
            //scale -= 0.01;
            Vector2 offset = axis * scale;
            
            double inv = 1d / sumRadius;
            double massRatioA = a.Radius * inv;
            double massRatioB = b.Radius * inv;
            
            a.Location -= offset * massRatioA;
            // SetLocation(a, a.Location - (offset * massRatioA));
            b.Location += offset * massRatioB;
            // SetLocation(b, b.Location + (offset * massRatioB));
        }
        
        public void SetLocation(Ball b, Vector2 location)
        {
            Vector2I gridPosOld = GetGridLocation(b.Location);
            Vector2I gridPosNew = GetGridLocation(location);
            b.Location = location;
            
            if (gridPosOld == gridPosNew) { return; }
            
            _grid.Remove(b, gridPosOld);
            _grid.Add(b, gridPosNew);
        }
        
        public Vector2I GetGridLocation(Vector2 location)
            => (Vector2I)((location - _bounds.Location + _hfs) * _gsR);
        public Vector2 GetLocation(Vector2I gp)
            => (gp * _gs) - _hfs + _bounds.Location;
        
        public void AddBall(Ball b)
        {
            if (b.Radius > GridSize * 0.5)
            {
                b.Radius = GridSize * 0.5;
            }
            
            _balls.Add(b);
            Vector2I gridPos = GetGridLocation(b.Location);
            _grid.Add(b, gridPos);
        }
        public bool RemoveBall(Ball b)
        {
            if (!_balls.Remove(b))
            {
                return false;
            }
            
            Vector2I gridPos = GetGridLocation(b.Location);
            _grid.Remove(b, gridPos);
            
            return true;
        }
        public void RemoveAt(Vector2 framePos)
        {
            int l = _balls.Count;
            for (int i = 0; i < l; i++)
            {
                Ball b = _balls[i];
                
                // Inside circle
                if (b.Location.SquaredDistance(framePos) < (b.Radius * b.Radius))
                {
                    _balls.RemoveAt(i);
                    Vector2I gridPos = GetGridLocation(b.Location);
                    _grid.Remove(b, gridPos);
                    return;
                }
            }
        }
        
        public void IterateBalls(Action<Ball> action)
        {
            int l = _balls.Count;
            for (int i = 0; i < l; i++)
            {
                action(_balls[i]);
            }
        }
        // public void IterateGrid(Action<List<Ball>, Vector2> action)
        // {
        //     Vector2I s = _grid.Size;
        //     for (int x = 0; x < s.X; x++)
        //     {
        //         for (int y = 0; y < s.Y; y++)
        //         {
        //             action(_grid[x, y].Objects, GetLocation((x, y)));
        //         }
        //     }
        // }
    }
}
