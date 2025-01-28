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
        
        public double Gravity { get; set; } = 1000d;
        
        private readonly double _gsR = 1d / 10d;
        public double GridSize { get; } = 10d;
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
            size /= GridSize;
            // return;
            
            Vector2I vi = new Vector2I(
                Math.Ceiling(size.X),
                Math.Ceiling(size.Y));
            // if (vi == _grid?.Size) { return; }
            
            _grid = new Grid(vi);
            
            // // Fill grid with balls
            // int l = _balls.Count;
            // for (int i = 0; i < l; i++)
            // {
            //     Ball b = _balls[i];
                
            //     _grid.Add(b, GetGridLocation(b.Location));
            // }
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
                
                b.Acceleration = (0d, -Gravity);
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
            int l = _balls.Count;
            
            Program.ParallelFor(l, 1, a =>
            {
                Ball b1 = _balls[a];
                Box box = new Box(b1.Location, b1.Radius * 2d);
                
                Vector2I c1 = GetGridLocation((box.Left, box.Top));
                Vector2I c2 = GetGridLocation((box.Right, box.Top));
                Vector2I c3 = GetGridLocation((box.Left, box.Bottom));
                Vector2I c4 = GetGridLocation((box.Right, box.Bottom));
                
                Iterate(b1, c1);
                if (c2 != c1)
                {
                    Iterate(b1, c2);
                }
                if (c3 != c2 && c3 != c1)
                {
                    Iterate(b1, c3);
                }
                if (c4 != c3 && c4 != c2 && c4 != c1)
                {
                    Iterate(b1, c4);
                }
                
                // for (int b = a + 1; b < l; b++)
                // {
                //     Ball b2 = _balls[b];
                    
                //     ResolveCollision(b1, b2);
                // }
            });
        }
        private void Iterate(Ball b1, Vector2I p)
        {
            if (!_grid.Contains(p)) { return; }
            FastList<Ball> bs = _grid[p.X, p.Y];
            
            ReadOnlySpan<Ball> span = bs.AsSpan();
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
            
            //a.Location -= offset;
            SetLocation(a, a.Location - (offset * massRatioA));
            //b.Location += offset;
            SetLocation(b, b.Location + (offset * massRatioB));
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
            => (gp * GridSize) - _hfs + _bounds.Location;
        
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
