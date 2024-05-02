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
        
        static void Main(string[] args)
        {
            Core.Init();
            
            Window w = new Program(800, 500, "WORK");
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
                //_phm.ApplyPhysics(1d / 60d);
            }
            
            e.Context.Framebuffer.Clear(BufferBit.Colour);
            e.Context.View = Matrix4.CreateTranslation(-offset.X, offset.Y);
            e.Context.Model = Matrix.Identity;
            
            
            _phm.IterateBalls(b =>
            {
                e.Context.Render(b);
            });
            /*_phm.IterateGrid((g, l) =>
            {
                for (int i = 0; i < g.Count; i++)
                {
                    e.Context.DrawEllipse(new Box(l, g[i].Radius), g[i].Colour);
                }
            });*/
            
            e.Context.View = Matrix.Identity;
            e.Context.Model = Matrix4.CreateScale(10d);
            TR.DrawCentred(e.Context, $"{_phm.Time * 1000d}\n{_phm.Count}", SampleFont.GetInstance(), 0, 0);
        }
        
        /*
        private Vector2 Reflect(Vector2 dir, Radian lineAngle)
        {
            Radian dirA = Math.Atan2(dir.Y, dir.X);
            Radian newA = (lineAngle * 2d) - dirA;
            
            return (
                Math.Cos(newA),
                Math.Sin(newA)
            );
        }
        private void ClampToBounds(Ball b)
        {
            double r = b.Radius;
            Vector2 l = b.Location;
            
            if (l.X + r > _bounds.Right)
            {
                b.Velocity = (-b.Velocity.X * Friction, b.Velocity.Y);
                //l.X = Fold(l.X, _bounds.Right);
                l.X = _bounds.Right - r;
            }
            if (l.X - r < _bounds.Left)
            {
                b.Velocity = (-b.Velocity.X * Friction, b.Velocity.Y);
                //l.X = Fold(l.X, _bounds.Left);
                l.X = _bounds.Left + r;
            }
            if (l.Y + r > _bounds.Top)
            {
                b.Velocity = (b.Velocity.X, -b.Velocity.Y * Friction);
                //l.Y = Fold(l.Y, _bounds.Top);
                l.Y = _bounds.Top - r;
            }
            if (l.Y - r < _bounds.Bottom)
            {
                b.Velocity = (b.Velocity.X, -b.Velocity.Y * Friction);
                //l.Y = Fold(l.Y, _bounds.Bottom);
                l.Y = _bounds.Bottom + r;
            }
            
            b.Location = l;
        }
        
        private void ResolveCollision(Ball a, Ball b)
        {
            double dist = a.Location.Distance(b.Location);
            double diff = dist - (a.Radius + b.Radius);
            double scale = (diff * 0.5) / dist;
            scale -= 0.01;
            
            Vector2 between = a.Location - b.Location;
            Vector2 offset = between * scale;
            
            Radian lineAngle = Math.Atan2(between.Y, between.X);
            // Perpendicular angle
            lineAngle += 0.5 * Math.PI;
            
            Vector2 aDir = Reflect(a.Velocity, lineAngle);
            Vector2 bDir = Reflect(b.Velocity, lineAngle);
            
            //double momentum = (a.Velocity.Length * a.Mass) + (b.Velocity.Length * b.Mass);
            //double magnitude = momentum * Friction * 0.5;
            
            if (HeadAway(a, b))
            {
                aDir = -aDir;
            }
            if (HeadAway(b, a))
            {
                bDir = -bDir;
            }
            
            a.Location -= offset;
            //a.Velocity = -a.Velocity * Friction;
            //a.Velocity = aDir * (magnitude / a.Mass);
            a.Velocity = aDir * a.Velocity.Length * Friction;
            b.Location += offset;
            //b.Velocity = -b.Velocity * Friction;
            //b.Velocity = bDir * (magnitude / b.Mass);
            b.Velocity = bDir * b.Velocity.Length * Friction;
        }
        private static double Fold(double value, double fold) => fold - (value - fold);
        private static bool HeadAway(Ball a, Ball b)
        {
            return a.Location.SquaredDistance(b.Location) <
                (a.Location + a.Velocity).SquaredDistance(b.Location);
        }
        */
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
                _random.NextDouble(2d, 10d),
                _random.NextVector2(-5d, 5d),
                _random.NextColourF()
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
