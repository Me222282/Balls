#pragma warning disable CS8981
global using floatv =
#if DOUBLE
    System.Double;
#else
    System.Single;
#endif

global using Maths =
#if DOUBLE
    System.Math;
#else
    System.MathF;
#endif
#pragma warning restore CS8981

using System;
using Zene.Structs;
using Zene.Graphics;
using Zene.Windowing;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

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
            : base(width, height, title, true)
        {
            _random = new Random();
            _phm = new PhysicsManager((width, height));
            
            floatv hw = width * 0.5f;
            floatv hh = height * 0.5f;
            
            for (int i = 0; i < 40; i++)
            {
                AddObject(_random.NextVector2(-hw, hw, -hh, hh));
            }
            
            TR = new TextRenderer();
            
            _drawable = new DrawObject<float, byte>(stackalloc float[]
            {
                0.5f, 0.5f, 1f, 1f,
                0.5f, -0.5f, 1f, 0f,
                -0.5f, -0.5f, 0f, 0f,
                -0.5f, 0.5f, 0f, 1f
            }, stackalloc byte[] { 0, 1, 2, 2, 3, 0 }, 4, 0, AttributeSize.D2, BufferUsage.DrawFrequent);
            _drawable.AddAttribute(ShaderLocation.TextureCoords, 2, AttributeSize.D2);
            
            _instanceData = new ArrayBuffer<DataLayout>(1, BufferUsage.DrawRepeated);
            _instanceData.InitData(_mCapacity);

            // Add instance reference
            _drawable.AddInstanceBuffer(_instanceData, 3, 0, 0, DataType.FloatV, AttributeSize.D2, 1);
            // _drawable.AddInstanceBuffer(_instanceData, 4, 0, 0, DataType.FloatV, AttributeSize.D2, 1);
            _drawable.AddInstanceBuffer(_instanceData, 5, 0, 2 * sizeof(floatv), DataType.FloatV, AttributeSize.D1, 1);
            _drawable.AddInstanceBuffer(_instanceData, 6, 0, 3 * sizeof(floatv), DataType.Float, AttributeSize.D3, 1);
            
            _shader = new Shader();
        }
        
        private Random _random;
        private PhysicsManager _phm;
        private ArrayBuffer<DataLayout> _instanceData;
        private DrawObject<float, byte> _drawable;
        private Shader _shader;
        private int _mCapacity = 2000;
        
        public static TextRenderer TR;
        private bool _paused = false;
        private bool _render = true;
        
        protected override void OnUpdate(FrameEventArgs e)
        {
            Vector2 offset = Location + (Size * 0.5);
            
            if (this[MouseButton.Middle])
            {
                _phm.RemoveAt(MouseLocation - (Size * 0.5) + (offset.X, -offset.Y));
            }
            else if (this[MouseButton.Right])
            {
                AddObject(MouseLocation - (Size * 0.5) + (offset.X, -offset.Y));
            }
            else if (this[MouseButton.Left])
            {
                if (_phm.F.Ball != null)
                {
                    Vector2 pos = MouseLocation - (Size * 0.5) + (offset.X, -offset.Y);
                    _phm.F.NewLocation(pos + _fixedDiff);
                }
            }
            
            base.OnUpdate(e);
            
            if (!_paused)
            {
                _phm.ApplyPhysics(1f / 60, 8);
                // _phm.ApplyPhysics(1d / 60d, 1);
            }
            
            e.Context.Framebuffer.Clear(BufferBit.Colour);
            e.Context.View = Matrix4.CreateTranslation(-_phm.Centre);
            e.Context.Model = Matrix.Identity;
            
            if (_render)
            {
                // _phm.IterateBalls(b =>
                // {
                //     e.Context.Render(b);
                // });
                FillInst(_phm.Span);
                e.Context.Shader = _shader;
                e.Context.Draw(_drawable, _phm.Count);
            }
            
            e.Context.View = Matrix.Identity;
            e.Context.Model = Matrix4.CreateScale(10d);
            TR.DrawCentred(e.Context, $"{_phm.Time * 1000d}\n{_phm.Count}", SampleFont.GetInstance(), 0, 0);
            // Vector2I gp = _phm.GetGridLocation(MouseLocation - (Size * 0.5) + (offset.X, -offset.Y));
            // TR.DrawCentred(e.Context, $"{gp}", SampleFont.GetInstance(), 0, 0);
        }
        
        private Vector2 _fixedDiff;
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            
            if (e.Button != MouseButton.Left) { return; }
            
            Vector2 offset = Location + (Size * 0.5);
            Vector2 search = e.Location - (Size * 0.5) + (offset.X, -offset.Y);
            Ball b = _phm.GetBall(search);
            if (b == null) { return; }
            
            _fixedDiff = b.Location - search;
            _phm.F = new Fixed(b.Location, b);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            
            if (e.Button != MouseButton.Left) { return; }
            
            _phm.F = new Fixed();
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
            
            DrawContext.Projection = Matrix4.CreateOrthographic(e.X, e.Y, 0, 1);
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
                _random.NextNumber(2, PhysicsManager.GridSize * 0.5f),
                _random.NextVector2(-5, 5),
                GenColour()
            ));
        }
        public void AddBond(Vector2 location)
        {
            Vector2 vel = _random.NextNumber(-5, 5);
            Colour3 c = GenColour();
            floatv r = _random.NextNumber(2, PhysicsManager.GridSize * 0.5f);
            floatv dist = _random.NextNumber(2 * r, 3 * r);
            
            Vector2 dir = _random.NextVector2(-1, 1).Normalised();
            Vector2 a = location + (dir * dist * 0.5f);
            Vector2 b = location - (dir * dist * 0.5f);
            
            _phm.AddBond(new Bond(dist, 
                new Ball(a, r, vel, c),
                new Ball(b, r, vel, c), 1d));
        }
        public void AddString(Vector2 location)
        {
            Vector2 vel = _random.NextVector2(-5, 5);
            Colour3 c = GenColour();
            floatv e = _random.NextNumber(0.01f, 0.7f);
            floatv r = _random.NextNumber(2, PhysicsManager.GridSize * 0.5f);
            floatv dist = _random.NextNumber(2 * r, 2.5f * r);
            int cp = _random.Next(2, 6);
            
            Span<Ball> span = new Ball[cp + 1];
            span[0] = new Ball(location, r, vel, c);
            Vector2 dir = _random.NextVector2(-1, 1).Normalised();
            
            for (int i = 1; i < cp + 1; i++)
            {
                location += dir * dist;
                span[i] = new Ball(location, r, vel, c);
            }
            
            _phm.AddString(new String(span, dist, e));
        }
        public void AddObject(Vector2 location)
        {
            // AddBall(location);
            
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
            => Colour3.FromHsl(_random.NextNumber(0, 360), 1, 0.5f);
        
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            if (e[Keys.Space])
            {
                _paused = !_paused;
                return;
            }
            if (e[Keys.R])
            {
                _render = !_render;
                return;
            }
        }
        
        private void FillInst(ReadOnlySpan<Ball> bs)
        {
            Span<DataLayout> block = stackalloc DataLayout[bs.Length];
            
            for (int i = 0; i < bs.Length; i++)
            {
                Ball b = bs[i];
                block[i] = new DataLayout(b.Location, b.Radius, b.Colour);
            }
            
            _instanceData.EditData(0, block);
        }
        
        private struct DataLayout
        {
            public DataLayout(Vector2 l, floatv r, ColourF3 c)
            {
                Location = l;
                Radius = r;
                Colour = c;
            }
            
            public Vector2 Location;
            // private Vector2 _oldPos;
            public floatv Radius;
            public ColourF3 Colour;
        }
    }
}
