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
            
            Core.Terminate();
        }
        
        public Program(int width, int height, string title)
            : base(width, height, title, 4.3, true)
        {
            _random = new Random();
            _phm = new PhysicsManager((width, height));
            
            double hw = width * 0.5;
            double hh = height * 0.5;
            
            for (int i = 0; i < 100; i++)
            {
                AddBall(_random.NextVector2(-hw, hw, -hh, hh));
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
                AddBall(MouseLocation - (Size * 0.5) + (offset.X, -offset.Y));
            }
            
            base.OnUpdate(e);
            
            if (!_paused)
            {
                _phm.ApplyPhysics(1d / 60d, 4);
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
                _random.NextDouble(2d, _phm.GridSize * 0.5),
                _random.NextVector2(-5d, 5d),
                _random.NextColour3()
            ));
        }
        
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
