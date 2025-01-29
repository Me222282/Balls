using System;
using Zene.Structs;

namespace Balls
{
    public struct Bond
    {
        public Bond(double l, Ball a, Ball b, double e)
        {
            Length = l;
            Elasticity = e;
            A = a;
            B = b;
        }
        
        public double Length;
        public double Elasticity;
        public Ball A;
        public Ball B;
        
        public void Constrain()
        {
            // Vector2 axis = A.Location - B.Location;
            // double dist = axis.Length;
            // Vector2 normal = axis / dist;
            // Vector2 diff = (Length - dist) * 0.5 * normal;
            // A.Location += diff;
            // B.Location -= diff;
            Vector2 axis = A.Location - B.Location;
            double dist = axis.Length;
            Vector2 normal = axis / dist;
            Vector2 diff = (Length - dist) * 0.5 * normal * Elasticity;
            A.Location += diff;
            B.Location -= diff;
        }
    }
    public ref struct String
    {
        public String(Span<Ball> balls, double length, double e)
        {
            Balls = balls;
            Length = length;
            Elasticity = e;
        }
        
        public Span<Ball> Balls;
        public double Length;
        public double Elasticity;
    }
    public struct Fixed
    {
        public Fixed(Vector2 l, Ball b)
        {
            Location = l;
            Ball = b;
        }
        
        public Vector2 Location;
        internal Vector2 nl;
        public Ball Ball;
        
        public void NewLocation(Vector2 l) => nl = l;
    }
}