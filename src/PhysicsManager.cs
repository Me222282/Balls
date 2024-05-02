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
        private struct Cell
        {
            public Cell(bool temp)
            {
                Objects = new List<Ball>(8);
            }
            
            public List<Ball> Objects;
            
            public void Add(Ball b) => Objects.Add(b);
            public void Remove(Ball b) => Objects.Remove(b);
        }
        
        private class Grid
        {
            public Grid(Vector2I size)
            {
                _grid = new Cell[size.Y, size.X];
            
                for (int x = 0; x < size.X; x++)
                {
                    for (int y = 0; y < size.Y; y++)
                    {
                        _grid[y, x] = new Cell(false);
                    }
                }
            }
            
            private Cell[,] _grid;
            
            public Vector2I Size => new Vector2I(_grid.GetLength(1), _grid.GetLength(0));
            
            //public Cell this[Vector2I pos] => _grid[pos.Y, pos.X];
            public Cell this[int x, int y]
            {
                get
                {
                    if (x >= _grid.GetLength(1) ||
                        y >= _grid.GetLength(0) ||
                        x < 0 ||
                        y < 0)
                    {
                        return default;
                    }
                    
                    return _grid[y, x];
                }
            }
            
            public void Add(Ball b, Vector2I pos)
            {
                if (b == null || pos.X >= _grid.GetLength(1) ||
                    pos.Y >= _grid.GetLength(0) ||
                    pos.X < 0 ||
                    pos.Y < 0)
                {
                    return;
                }
                
                _grid[pos.Y, pos.X].Add(b);
            }
            public void Remove(Ball b, Vector2I pos)
            {
                if (b == null || pos.X >= _grid.GetLength(1) ||
                    pos.Y >= _grid.GetLength(0) ||
                    pos.X < 0 ||
                    pos.Y < 0)
                {
                    return;
                }
                
                _grid[pos.Y, pos.X].Remove(b);
            }
        }
        
        public PhysicsManager(Vector2 size)
        {
            SetFrameSize(size);
        }
        
        public double Gravity { get; set; } = 1000d;
        
        public double GridSize { get; } = 10d;
        public Vector2 FrameSize { get; private set; }
        private Grid _grid;
        
        private Box _bounds;
        private List<Ball> _balls = new List<Ball>();
        
        internal Ball F => _balls[0];
        
        public double Time { get; private set; }
        public int Count => _balls.Count;
        
        public void SetFrameSize(Vector2 size)
        {
            FrameSize = size;
            _bounds = new Box(_bounds.Location, size);
            size /= GridSize;
            
            _grid = new Grid(new Vector2I(
                Math.Ceiling(size.X),
                Math.Ceiling(size.Y)));
            
            // Fill grid with balls
            int l = _balls.Count;
            for (int i = 0; i < l; i++)
            {
                Ball b = _balls[i];
                
                _grid.Add(b, GetGridLocation(b.Location));
            }
        }
        public void SetBoundPos(Vector2 pos)
        {
            _bounds.Centre = pos;
        }
        
        public void ApplyPhysics(double dt, int subStep)
        {
            double t = Core.Time;
            
            dt /= subStep;
            
            for (int i = 0; i < subStep; i++)
            {
                ApplyPhysics(dt);
            }
            
            Time = Core.Time - t;
        }
        public void ApplyPhysics(double dt)
        {
            PreCollisionPhsyics();
            CalculateCollisions();
            
            int l = _balls.Count;
            for (int i = 0; i < l; i++)
            {
                Ball b = _balls[i];
                b.ApplyVerlet(dt);
                continue;
                
                // Set grid position
                Vector2I gridPosOld = GetGridLocation(b.OldLocation);
                Vector2I gridPosNew = GetGridLocation(b.Location);
                
                if (gridPosOld == gridPosNew) { continue; }
                
                _grid.Remove(b, gridPosOld);
                _grid.Add(b, gridPosNew);
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
            
            //b.Location = l;
            SetLocation(b, l);
        }
        
        /*
        private void CalculateCollisions()
        {
            Vector2I gs = _grid.Size;
            int threads = Math.Min(gs.X / 3, Threads);
            int columnPerThread = gs.X / threads;
            
            // First pass
            Parallel.For(0, threads, i =>
            {
                int start = i * columnPerThread;
                int end = ((i + 1) * columnPerThread) - ((2 * columnPerThread) / 3);
                
                CollisionPass(start, end, gs.Y);
            });
            
            // Second pass
            Parallel.For(0, threads, i =>
            {
                int start = (i * columnPerThread) + (columnPerThread / 3);
                int end = ((i + 1) * columnPerThread) - (columnPerThread / 3);
                
                CollisionPass(start, end, gs.Y);
            });
            
            // Third pass
            Parallel.For(0, threads, i =>
            {
                int start = (i * columnPerThread) + ((2 * columnPerThread) / 3);
                int end = (i + 1) * columnPerThread;
                
                if (i + 1 == threads) { end = gs.X; }
                
                CollisionPass(start, end, gs.Y);
            });
        }
        private void CollisionPass(int start, int end, int gsy)
        {
            for (int x = start; x < end; x++)
            {
                for (int y = 0; y < gsy; y++)
                {
                    Cell c = _grid[x, y];
                    
                    // Iterate through all surrounding cells
                    ResolveCellCollisions(c, c);
                    ResolveCellCollisions(c, _grid[x - 1, y]);
                    ResolveCellCollisions(c, _grid[x + 1, y]);
                    ResolveCellCollisions(c, _grid[x - 1, y - 1]);
                    ResolveCellCollisions(c, _grid[x - 1, y + 1]);
                    ResolveCellCollisions(c, _grid[x + 1, y - 1]);
                    ResolveCellCollisions(c, _grid[x + 1, y + 1]);
                    ResolveCellCollisions(c, _grid[x, y - 1]);
                    ResolveCellCollisions(c, _grid[x, y + 1]);
                }
            }
        }
        private void ResolveCellCollisions(Cell a, Cell b)
        {
            if (a.Objects == null || b.Objects == null) { return; }
            
            foreach (Ball b1 in a.Objects)
            {
                foreach (Ball b2 in b.Objects)
                {
                    if (b1 == b2) { continue; }
                    if (b1 == null || b2 == null) { continue; }
                    
                    ResolveCollision(b1, b2);
                }
            }
        }*/
        private void CalculateCollisions()
        {
            int l = _balls.Count;
            
            // for (int a = 0; a < l; a++)
            // {
            //     Ball b1 = _balls[a];
                
            //     for (int b = a + 1; b < l; b++)
            //     {
            //         Ball b2 = _balls[b];
                    
            //         ResolveCollision(b1, b2);
            //     }
            // }
            // return;
            Program.ParallelFor(l, 8, a =>
            {
                Ball b1 = _balls[a];
                // Vector2I gp = GetGridLocation(b1.Location);
                
                // // All neighbouring lists
                // Iterate(b1, _grid[gp.X, gp.Y].Objects);
                // Iterate(b1, _grid[gp.X + 1, gp.Y].Objects);
                // Iterate(b1, _grid[gp.X - 1, gp.Y].Objects);
                // Iterate(b1, _grid[gp.X, gp.Y + 1].Objects);
                // Iterate(b1, _grid[gp.X, gp.Y - 1].Objects);
                // Iterate(b1, _grid[gp.X + 1, gp.Y + 1].Objects);
                // Iterate(b1, _grid[gp.X - 1, gp.Y + 1].Objects);
                // Iterate(b1, _grid[gp.X + 1, gp.Y - 1].Objects);
                // Iterate(b1, _grid[gp.X - 1, gp.Y - 1].Objects);
                
                for (int b = a + 1; b < l; b++)
                {
                    Ball b2 = _balls[b];
                    
                    ResolveCollision(b1, b2);
                }
            });
        }
        private void Iterate(Ball b1, List<Ball> bs)
        {
            if (bs is null) { return; }
            ReadOnlySpan<Ball> span = CollectionsMarshal.AsSpan<Ball>(bs);
            for (int b = 0; b < span.Length; b++)
            {
                Ball b2 = _balls[b];
                
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
            
            dist = Math.Sqrt(dist);
            double diff = dist - sumRadius;
            double scale = diff * 0.5 * 1d;
            //scale -= 0.01;
            if (dist == 0)
            {
                axis = (a.Velocity - b.Velocity).Normalised();
            }
            else
            {
                axis /= dist;
            }
            Vector2 offset = axis * scale;
            
            double massRatioA = a.Radius / sumRadius;
            double massRatioB = b.Radius / sumRadius;
            
            //a.Location -= offset;
            SetLocation(a, a.Location - (offset * massRatioA));
            //b.Location += offset;
            SetLocation(b, b.Location + (offset * massRatioB));
        }
        
        public void SetLocation(Ball b, Vector2 location)
        {
            // Vector2I gridPosOld = GetGridLocation(b.Location);
            // Vector2I gridPosNew = GetGridLocation(location);
            b.Location = location;
            
            // if (gridPosOld == gridPosNew) { return; }
            
            // _grid.Remove(b, gridPosOld);
            // _grid.Add(b, gridPosNew);
        }
        
        public Vector2I GetGridLocation(Vector2 location) => (Vector2I)((location - _bounds.Location + (FrameSize * 0.5)) / GridSize);
        public Vector2 GetLocation(Vector2I gp) => (gp * GridSize) - (FrameSize * 0.5) + _bounds.Location;
        
        public void AddBall(Ball b)
        {
            if (b.Radius > GridSize * 0.5)
            {
                b.Radius = GridSize * 0.5;
            }
            
            _balls.Add(b);
            // Vector2I gridPos = GetGridLocation(b.Location);
            // _grid.Add(b, gridPos);
        }
        public bool RemoveBall(Ball b)
        {
            if (!_balls.Remove(b))
            {
                return false;
            }
            
            // Vector2I gridPos = GetGridLocation(b.Location);
            // _grid.Remove(b, gridPos);
            
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
                    // Vector2I gridPos = GetGridLocation(b.Location);
                    // _grid.Remove(b, gridPos);
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
        public void IterateGrid(Action<List<Ball>, Vector2> action)
        {
            Vector2I s = _grid.Size;
            for (int x = 0; x < s.X; x++)
            {
                for (int y = 0; y < s.Y; y++)
                {
                    action(_grid[x, y].Objects, GetLocation((x, y)));
                }
            }
        }
    }
}
