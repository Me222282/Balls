using Zene.Structs;

namespace Balls
{
    // public struct Cell
    // {
    //     public Cell(bool temp)
    //     {
    //         Objects = new FastList<Ball>(8);
    //     }
        
    //     public FastList<Ball> Objects;
        
    //     public void Add(Ball b) => Objects.Add(b);
    //     public void Remove(Ball b) => Objects.Remove(b);
    // }
    
    public class Grid
    {
        public Grid(Vector2I size)
        {
            _grid = new FastList<Ball>[size.Y + 2, size.X + 2];
        
            for (int x = 0; x < size.X + 2; x++)
            {
                for (int y = 0; y < size.Y + 2; y++)
                {
                    _grid[y, x] = new FastList<Ball>(8);
                }
            }
        }
        
        private FastList<Ball>[,] _grid;
        
        public Vector2I Size => new Vector2I(_grid.GetLength(1), _grid.GetLength(0));
        
        // public Cell this[Vector2I pos] => _grid[pos.Y, pos.X];
        public FastList<Ball> this[int x, int y]
        {
            get
            {
                // if (x >= _grid.GetLength(1) ||
                //     y >= _grid.GetLength(0) ||
                //     x < 0 ||
                //     y < 0)
                // {
                //     return default;
                // }
                
                return _grid[y + 1, x + 1];
            }
        }
        
        public void Add(Ball b, Vector2I pos)
        {
            if (!Contains(pos)) { return; }
            
            _grid[pos.Y + 1, pos.X + 1].Add(b);
        }
        public void Remove(Ball b, Vector2I pos)
        {
            if (!Contains(pos)) { return; }
            
            _grid[pos.Y + 1, pos.X + 1].Remove(b);
        }
        
        public void Clear()
        {
            int h = _grid.GetLength(0);
            int w = _grid.GetLength(1);
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    _grid[i, j].Clear();
                }   
            }
        }
        
        public bool Contains(Vector2I pos)
        {
            return pos.X < (_grid.GetLength(1) - 1) &&
                pos.Y < (_grid.GetLength(0) - 1) &&
                pos.X >= -1 &&
                pos.Y >= -1;
        }
    }
}