using System;
using Zene.Structs;
using Zene.Graphics;
using Zene.Windowing;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;

namespace Balls
{
    public class PhysicsManager
    {
        public PhysicsManager(Vector2 size)
        {
            SetFrameSize(size);
            _newPos = 0;
            SetFrame(size, 0);
        }
        
        public static floatv Gravity { get; set; } = 1000;
        public static Vector2 MaxSize { get; set; } = (1000, 1000);
        
        public const floatv GridSize = 10;
        private floatv _gsR = 1f / 10;
        private floatv _gs = 10;
        public Vector2 FrameSize { get; set; }
        private Vector2 _hfs;
        public Vector2 Centre => _bounds.Location;
        private Grid _grid;
        
        private Box _bounds;
        private Vector2 _newPos;
        private FastList<Ball> _balls = new FastList<Ball>(100);
        private FastList<Bond> _bonds = new FastList<Bond>(50);
        
        public Fixed F;
        
        public double Time { get; private set; }
        public int Count => (int)_balls.Length;
        
        public ReadOnlySpan<Ball> Span => _balls.AsSpan();
        
        public void SetFrameSize(Vector2 size) => FrameSize = size;
        private void SetFrame(Vector2 size, Vector2 pos)
        {
            // FrameSize = size;
            _bounds = new Box(pos, size);
            _hfs = size * 0.5f;
            Vector2 gs = size / GridSize;
            // return;
            
            if (gs.X >= MaxSize.X)
            {
                gs.X = MaxSize.X;
                _gs = size.X / gs.X;
                _gsR = 1 / _gs;
                gs.Y = size.Y * _gsR;
            }
            if (gs.Y >= MaxSize.Y)
            {
                gs.Y = MaxSize.Y;
                _gs = size.Y / gs.Y;
                _gsR = 1 / _gs;
                gs.X = size.X * _gsR;
            }
            
            Vector2I vi = new Vector2I(
                Maths.Ceiling(gs.X),
                Maths.Ceiling(gs.Y));
            if (vi == _grid?.Size) { return; }
            
            _grid = new Grid(vi);
        }
        public void SetBoundPos(Vector2 pos) => _newPos = pos;
        
        public void ApplyPhysics(floatv dt, int subStep)
        {
            double t = Core.Time;
            
            dt /= subStep;
            
            Vector2 changeS = (FrameSize - _bounds.Size) / subStep;
            Vector2 changeP = (_newPos - _bounds.Location) / subStep;
            bool change = changeS != 0 || changeP != 0;
            
            Vector2 fDiff = (F.nl - F.Location) / subStep;
            
            for (int i = 0; i < subStep; i++)
            {
                if (change)
                {
                    SetFrame(_bounds.Size + changeS,
                        _bounds.Location + changeP);
                }
                F.Location += fDiff;
                ApplyPhysics(dt);
            }
            
            Time = Core.Time - t;
        }
        private void ApplyPhysics(floatv dt)
        {
            _grid.Clear();
            PreCollisionPhsyics();
            CalculateCollisions();
            
            // Program.ParallelFor(l, 1, i =>
            // Parallel.For(0, (int)_balls.Length, i =>
            // {
            //     Ball b = _balls[i];
            //     b.ApplyVerlet(dt);
            // });
            
            Span<Ball> span = _balls.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                Ball b = span[i];
                b.ApplyVerlet(dt);
            }
        }
        
        private void PreCollisionPhsyics()
        {
            Span<Bond> bonds = _bonds.AsSpan();
            for (int i = 0; i < bonds.Length; i++)
            {
                Bond b = bonds[i];
                b.Constrain();
            }
            
            if (F.Ball != null)
            {
                F.Ball.Location = F.Location;
            }
            
            Span<Ball> span = _balls.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                Ball b = span[i];
                
                // b.Acceleration = (0d, -Gravity);
                ClipToBounds(b);
                
                _grid.Add(b, GetGridLocation(b.Location));
            }
        }
        private void ClipToBounds(Ball b)
        {
            floatv r = b.Radius;
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
            // Program.ParallelFor(size.Y - 1, 1, z =>
            // Parallel.For(0, size.Y - 1, z =>
            // // for (int z = 0; z < size.Y - 1; z++)
            // {
            //     int y = size.Y - 1 - z;
            //     ReadOnlySpan<Ball> a = new ReadOnlySpan<Ball>(),
            //         b = _grid[0, y].AsSpan(), c,
            //         d = new ReadOnlySpan<Ball>(),
            //         e = _grid[0, y - 1].AsSpan(), f;//,
            //         // g = new ReadOnlySpan<Ball>(),
            //         // h = _grid[0, y - 2].AsSpan(), i;
            //     for (int x = 1; x < size.X; x++)
            //     {
            //         c = _grid[x, y].AsSpan();
            //         f = _grid[x, y - 1].AsSpan();
            //         // i = _grid[x, y - 2].AsSpan();
                    
            //         for (int j = 0; j < e.Length; j++)
            //         {
            //             Ball b1 = e[j];
                        
            //             Iterate(b1, a);
            //             Iterate(b1, b);
            //             Iterate(b1, c);
            //             Iterate(b1, d);
            //             Iterate(b1, e);
            //             Iterate(b1, f);
            //             // Iterate(b1, g);
            //             // Iterate(b1, h);
            //             // Iterate(b1, i);
            //         }
                    
            //         a = b;
            //         b = c;
            //         d = e;
            //         e = f;
            //         // g = h;
            //         // h = i;
            //     }
                
            //     for (int j = 0; j < e.Length; j++)
            //     {
            //         Ball b1 = e[j];
                    
            //         Iterate(b1, a);
            //         Iterate(b1, b);
            //         Iterate(b1, d);
            //         Iterate(b1, e);
            //         // Iterate(b1, g);
            //         // Iterate(b1, h);
            //     }
            // });
            
            // Parallel.For(1, size.X, x =>
            for (int x = 1; x < size.X; x++)
            {
                // int x = z;
                ReadOnlySpan<Ball> a = new ReadOnlySpan<Ball>(),
                    b = _grid[x, 0].AsSpan(), c,
                    d = new ReadOnlySpan<Ball>(),
                    e = _grid[x - 1, 0].AsSpan(), f;
                for (int y = 1; y < size.Y; y++)
                {
                    c = _grid[x, y].AsSpan();
                    f = _grid[x - 1, y].AsSpan();
                    
                    for (int j = 0; j < e.Length; j++)
                    {
                        Ball b1 = e[j];
                        
                        Iterate(b1, a);
                        Iterate(b1, b);
                        Iterate(b1, c);
                        Iterate(b1, d);
                        Iterate(b1, e);
                        Iterate(b1, f);
                    }
                    
                    a = b;
                    b = c;
                    d = e;
                    e = f;
                }
                
                for (int j = 0; j < e.Length; j++)
                {
                    Ball b1 = e[j];
                    
                    Iterate(b1, a);
                    Iterate(b1, b);
                    Iterate(b1, d);
                    Iterate(b1, e);
                }
            }//);
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
            floatv sumRadius = a.Radius + b.Radius;
            Vector2 axis = a.Location - b.Location;
            floatv dist = axis.SquaredLength;
            
            if (dist >= (sumRadius * sumRadius)) { return; }
            
            if (dist == 0)
            {
                axis = (a.Velocity - b.Velocity).Normalised();
            }
            else
            {
                dist = Maths.Sqrt(dist);
                axis /= dist;
            }
            floatv diff = dist - sumRadius;
            floatv scale = diff * 0.5f;
            Vector2 offset = axis * scale;
            
            floatv inv = 1 / sumRadius;
            floatv massRatioA = a.Radius * inv;
            floatv massRatioB = b.Radius * inv;
            
            a.Location -= offset * massRatioA;
            // SetLocation(a, a.Location - (offset * massRatioA));
            b.Location += offset * massRatioB;
            // SetLocation(b, b.Location + (offset * massRatioB));
        }
        
        // public void SetLocation(Ball b, Vector2 location)
        // {
        //     Vector2I gridPosOld = GetGridLocation(b.Location);
        //     Vector2I gridPosNew = GetGridLocation(location);
        //     b.Location = location;
            
        //     if (gridPosOld == gridPosNew) { return; }
            
        //     _grid.Remove(b, gridPosOld);
        //     _grid.Add(b, gridPosNew);
        // }
        
        public Vector2I GetGridLocation(Vector2 location)
            => (Vector2I)((location - _bounds.Location + _hfs) * _gsR);
        public Vector2 GetLocation(Vector2I gp)
            => (gp * _gs) - _hfs + _bounds.Location;
        
        public void AddBall(Ball b)
        {
            if (b.Radius > GridSize * 0.5f)
            {
                b.Radius = GridSize * 0.5f;
            }
            
            _balls.Add(b);
        }
        // public bool RemoveBall(Ball b) => _balls.Remove(b);
        public void RemoveAt(Vector2 framePos)
        {
            Span<Ball> span = _balls.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                Ball b = span[i];
                
                // Inside circle
                if (b.Location.SquaredDistance(framePos) < (b.Radius * b.Radius))
                {
                    _balls.RemoveAt(i);
                    int j = IsInBond(b);
                    if (j < 0) { return; }
                    _bonds.RemoveAt(j);
                    return;
                }
            }
        }
        public int IsInBond(Ball ball)
        {
            Span<Bond> bonds = _bonds.AsSpan();
            for (int i = 0; i < bonds.Length; i++)
            {
                Bond b = bonds[i];
                if (b.A == ball || b.B == ball) { return i; }
            }
            
            return -1;
        }
        public void AddBond(Bond b)
        {
            AddBall(b.A);
            AddBall(b.B);
            _bonds.Add(b);
        }
        public void AddString(String c)
        {
            Ball lb = c.Balls[0];
            for (int i = 1; i < c.Balls.Length; i++)
            {
                Ball b = c.Balls[i];
                AddBond(new Bond(c.Length, lb, b, c.Elasticity));
                lb = b;
            }
        }
        public Ball GetBall(Vector2 framePos)
        {
            Span<Ball> span = _balls.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                Ball b = span[i];
                
                // Inside circle
                if (b.Location.SquaredDistance(framePos) < (b.Radius * b.Radius))
                {
                    return b;
                }
            }
            
            return null;
        }
        
        public void IterateBalls(Action<Ball> action)
        {
            Span<Ball> span = _balls.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                action(span[i]);
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
