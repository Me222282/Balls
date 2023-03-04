using System;
using Zene.Structs;
using Zene.Graphics;
using Zene.Windowing;

namespace Balls
{
    public class Ball : IRenderable
    {
        public Ball(Vector2 location, double radius)
            : this(location, radius, Vector2.Zero, ColourF.White)
        {
            
        }
        public Ball(Vector2 location, double radius, ColourF colour)
            : this(location, radius, Vector2.Zero, colour)
        {
            
        }
        public Ball(Vector2 location, double radius, Vector2 velocity)
            : this(location, radius, velocity, ColourF.White)
        {
            
        }
        public Ball(Vector2 location, double radius, Vector2 velocity, ColourF colour)
        {
            Colour = colour;
            Location = location;
            Velocity = velocity;
            Radius = radius;
        }
        
        public ColourF Colour { get; set; }
        public Vector2 Location { get; set; }
        public Vector2 Velocity { get; set; }
        public double Radius { get; set; }
        public double Mass { get; set; } = 1d;
        
        public void OnRender(IDrawingContext context)
        {
            DrawManager dm = (DrawManager)context;
            
            context.DrawEllipse(new Box(Location, Radius * 2d), Colour);
        }
    }
}
