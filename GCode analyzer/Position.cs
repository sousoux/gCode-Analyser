using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Martin.GCode
{
    class Position : ICloneable, IEquatable<Position>
    {
        internal double X { get; set; }
        internal double Y { get; set; }
        internal double Z { get; set; }
        internal double E { get; set; }

        internal static Position FromCommand(GCode code, Position current)
        {
            if (code.Command != GCodeCommand.G1)
                throw new ApplicationException("Only valid for G1 commands");
            Position newPos = (Position)current.Clone();
            foreach (GCodeParameter parm in code.Parameters)
            {
                switch (parm.Type)
                {
                    case GCodeParameterType.X:
                        newPos.X = Convert.ToDouble(parm.Value);
                        break;
                    case GCodeParameterType.Y:
                        newPos.Y = Convert.ToDouble(parm.Value);
                        break;
                    case GCodeParameterType.Z:
                        newPos.Z = Convert.ToDouble(parm.Value);
                        break;
                    case GCodeParameterType.E:
                        newPos.E = Convert.ToDouble(parm.Value);
                        break;
                    default:
                        throw new ApplicationException("Unexpected parameter");
                }
            }
            return newPos;
        }

        public static Position operator -(Position a, Position b)
        {
            return new Position() { X = a.X - b.X, Y = a.Y - b.Y, Z = a.Z - b.Z, E = a.E - b.E };
        }

        public static Position operator *(Position a, double b)
        {
            return new Position() { X = a.X * b, Y = a.Y * b, Z = a.Z * b, E = a.E * b };
        }

        public static Position operator +(Position a, Position b)
        {
            return new Position() { X = a.X + b.X, Y = a.Y + b.Y, Z = a.Z + b.Z, E = a.E + b.E };
        }

        public static Position CrossProduct(Position v1, Position v2)
        {
            return
            (
               new Position()
               {
                   X = v1.Y * v2.Z - v1.Z * v2.Y,
                   Y = v1.Z * v2.X - v1.X * v2.Z,
                   Z = v1.X * v2.Y - v1.Y * v2.X,
               }
            );
        }

        public double DistanceFromLine(Position start, Position end)
        {
            return CrossProduct(this - start, this - end).Magnitude / (end - start).Magnitude;
        }

        public double SquaredDistanceFromLine(Position start, Position end)
        {
            return CrossProduct(this - start, this - end).SquaredMagnitude / (end - start).SquaredMagnitude;
        }

        public static double DotProduct(Position v1, Position v2)
        {
            return
            (
               v1.X * v2.X +
               v1.Y * v2.Y +
               v1.Z * v2.Z
            );
        }

        public static bool operator !=(Position a, Position b)
        {
            return !a.Equals(b);
        }

        public static bool operator ==(Position a, Position b)
        {
            return a.Equals(b);
        }

        internal double SquaredMagnitude
        {
            get { return (Math.Pow(X, 2) + Math.Pow(Y, 2) + Math.Pow(Z, 2)); }
        }

        internal double Magnitude
        {
            get { return Math.Sqrt(SquaredMagnitude); }
        }

        internal double Distance(Position b)
        {
            return (b - this).Magnitude;
        }

        public override int GetHashCode()
        {
            return (X+Y+Z+E).GetHashCode();
        }

        public object Clone()
        {
            return new Position() { X = this.X, Y = this.Y, Z = this.Z, E = this.E };
        }

        public override bool Equals(Object other)
        {
            return Equals((Position)other);
        }

        public bool Equals(Position other)
        {
            return (this.X == other.X && this.Y == other.Y && this.Z == other.Z && this.E == other.E);
        }
    }
}