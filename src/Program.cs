using System;
using Zene.Structs;
using Zene.Graphics;
using Zene.Windowing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Balls
{
    class Program : Window
    {
        public static void ParallelFor(int range, int threads, Action<int> action)
        {
            Span<int> lengths = stackalloc int[threads];
            int baseSize = range / threads;
            int extras = range % threads;
            for (int i = 0; i < threads; i++)
            {
                lengths[i] = baseSize;
                if (extras > 0)
                {
                    lengths[i]++;
                    extras--;
                }
            }
            
            Task[] tasks = new Task[threads];
            
            int current = lengths[0];
            for (int i = 1; i < threads; i++)
            {
                int c = current;
                int l = lengths[i] + c;
                current = l;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = c; j < l; j++)
                    {
                        action(j);
                    }
                });
            }
            
            for (int j = 0; j < lengths[0]; j++)
            {
                action(j);
            }
            
            for (int i = 1; i < threads; i++)
            {
                tasks[i].Wait();
            }
        }
        
        public static DrawContext DC;
        
        static void Main(string[] args)
        {
            Core.Init();
            
            Window w = new Program(800, 500, "WORK");
            DC = w.DrawContext;
            w.RunMultithread();
            w.Dispose();
            
            Core.Terminate();
        }
        
        public Program(int width, int height, string title)
            : base(width, height, title, 4.3, true)
        {
            _random = new Random();
            _phm = new PhysicsManager((width, height));
            
            double hw = width * 0.5;
            double hh = height * 0.5;
            
            for (int i = 0; i < 40; i++)
            {
                AddObject(_random.NextVector2(-hw, hw, -hh, hh));
            }
            
            TR = new TextRenderer();
        }
        
        private Random _random;
        private PhysicsManager _phm;
        
        public static TextRenderer TR;
        private bool _paused = false;
        
        protected override void OnUpdate(FrameEventArgs e)
        {
            Vector2 offset = Location + (Size * 0.5);
            
            if (this[MouseButton.Right])
            {
                _phm.RemoveAt(MouseLocation - (Size * 0.5) + (offset.X, -offset.Y));
            }
            else if (this[MouseButton.Left])
            {
                AddObject(MouseLocation - (Size * 0.5) + (offset.X, -offset.Y));
            }
            
            base.OnUpdate(e);
            
            if (!_paused)
            {
                _phm.ApplyPhysics(1d / 60d, 8);
                // _phm.ApplyPhysics(1d / 60d, 1);
            }
            
            e.Context.Framebuffer.Clear(BufferBit.Colour);
            e.Context.View = Matrix4.CreateTranslation(-_phm.Centre);
            e.Context.Model = Matrix.Identity;
            
            _phm.IterateBalls(b =>
            {
                e.Context.Render(b);
            });
            
            e.Context.View = Matrix.Identity;
            e.Context.Model = Matrix4.CreateScale(10d);
            TR.DrawCentred(e.Context, $"{_phm.Time * 1000d}\n{_phm.Count}", SampleFont.GetInstance(), 0, 0);
            // Vector2I gp = _phm.GetGridLocation(MouseLocation - (Size * 0.5) + (offset.X, -offset.Y));
            // TR.DrawCentred(e.Context, $"{gp}", SampleFont.GetInstance(), 0, 0);
        }
        
        protected override void OnSizeChange(VectorIEventArgs e)
        {
            base.OnSizeChange(e);
            
            Actions.Push(() =>
            {
                Vector2 offset = Location + (e.Value * 0.5);
                offset.Y = -offset.Y;
                _phm.SetBoundPos(offset);
                _phm.SetFrameSize(e.Value);
            });
            
            DrawContext.Projection = Matrix4.CreateOrthographic(e.X, e.Y, 0d, 1d);
        }
        protected override void OnWindowMove(VectorIEventArgs e)
        {
            base.OnWindowMove(e);
            
            Vector2 offset = e.Value + (Size * 0.5);
            offset.Y = -offset.Y;
            
            Actions.Push(() =>
            {
                _phm.SetBoundPos(offset);
            });
        }
        public void AddBall(Vector2 location)
        {
            _phm.AddBall(new Ball(
                location,
                _random.NextDouble(2d, PhysicsManager.GridSize * 0.5),
                _random.NextVector2(-5d, 5d),
                GenColour()
            ));
        }
        public void AddBond(Vector2 location)
        {
            Vector2 vel = _random.NextVector2(-5d, 5d);
            Colour3 c = GenColour();
            double r = _random.NextDouble(2d, PhysicsManager.GridSize * 0.5);
            double dist = _random.NextDouble(2d * r, 3d * r);
            
            Vector2 dir = _random.NextVector2(-1d, 1d).Normalised();
            Vector2 a = location + (dir * dist * 0.5);
            Vector2 b = location - (dir * dist * 0.5);
            
            _phm.AddBond(new Bond(dist, 
                new Ball(a, r, vel, c),
                new Ball(b, r, vel, c), 1d));
        }
        public void AddString(Vector2 location)
        {
            Vector2 vel = _random.NextVector2(-5d, 5d);
            Colour3 c = GenColour();
            double e = _random.NextDouble(0.01, 0.7);
            double r = _random.NextDouble(2d, PhysicsManager.GridSize * 0.5);
            double dist = _random.NextDouble(2d * r, 2.5 * r);
            int cp = _random.Next(2, 6);
            
            Span<Ball> span = new Ball[cp + 1];
            span[0] = new Ball(location, r, vel, c);
            Vector2 dir = _random.NextVector2(-1d, 1d).Normalised();
            
            for (int i = 1; i < cp + 1; i++)
            {
                location += dir * dist;
                span[i] = new Ball(location, r, vel, c);
            }
            
            _phm.AddString(new String(span, dist, e));
        }
        public void AddObject(Vector2 location)
        {
            int i = _random.Next(0, 3);
            switch (i)
            {
                case 0:
                    AddBall(location);
                    return;
                case 1:
                    AddBond(location);
                    return;
                case 2:
                    AddString(location);
                    return;
            }
        }
        
        public Colour3 GenColour()
            => Colour3.FromHsl(_random.NextDouble(0d, 360d), 1d, 0.5);
        
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            if (e[Keys.Space])
            {
                _paused = !_paused;
                return;
            }
        }
    }
}
