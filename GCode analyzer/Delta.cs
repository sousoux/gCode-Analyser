using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Martin.GCode
{
    class Delta
    {
        const double DELTA_HOMING_OFFSET = 128.0; // mm
        const double DELTA_DIAGONAL_ROD = 250.0; // mm
        const double DELTA_SEGMENTS_PER_SECOND = 200; // make delta curves from many straight lines
        const double DELTA_ZERO_OFFSET = -9; // prdouble surface is lower than bottom endstops

        const double SIN_60 = 0.8660254037844386;
        const double COS_60 = 0.5;
        const double DELTA_RADIUS = (175.0 - 33.0 - 18.0);
        const double DELTA_TOWER1_X = -SIN_60 * DELTA_RADIUS;
        const double DELTA_TOWER1_Y = -COS_60 * DELTA_RADIUS;
        const double DELTA_TOWER2_X = SIN_60 * DELTA_RADIUS;
        const double DELTA_TOWER2_Y = -COS_60 * DELTA_RADIUS;
        const double DELTA_TOWER3_X = 0.0;
        const double DELTA_TOWER3_Y = DELTA_RADIUS;

        internal int CandidateDeltaSurplus { get; set; }
        internal int JohanTotalSteps { get; set; }
        internal int CandidateTotalSteps { get; set; }
        internal int DistanceCalculations { get; set; }
        internal double MaxJohannError { get; set; }
        internal double MaxCandidateError { get; set; }
        internal int MaxDepth { get; set; }

        int deltaCalculations = 0;

        int feedmultiply = 100; // Not changed unless M220 received or G28 home
        double feedrate = 1500.0;

        Position current = new Position() { X = 0, Y = 0, Z = 0, E = 0 };
        Position deltaCurrent;

        public Delta()
        {
            deltaCurrent = DeltaPosition(current);
            CandidateDeltaSurplus = 0;
            MaxDepth = 0;
            MaxJohannError = MaxCandidateError = 0;
            JohanTotalSteps = CandidateTotalSteps = DistanceCalculations = 0;
        }

        /// <summary>
        /// Test the accuracy of the split up move
        /// </summary>
        /// <param name="candidate">Candidate to test</param>
        /// <returns>Maximum distance error found</returns>
        private double TestPartitionAccuracy(List<Position> candidate)
        {
            double maxdist = 0.0;
            for (int idx = 0; idx < candidate.Count - 1; idx++)
            {
                int steps = (int) Math.Max(1, candidate[idx].Distance(candidate[idx + 1]) / 0.01);
                Position difference = candidate[idx + 1] - candidate[idx];
                for (double s = 1; s < steps; s++)
                {
                    double testDist = Math.Abs(DeltaPosition(candidate[idx] + (difference * (s / Convert.ToDouble(steps)))).DistanceFromLine(DeltaPosition(candidate[idx]), DeltaPosition(candidate[idx + 1])));
                    if (testDist > maxdist) maxdist = testDist;
                }
            }
            return maxdist;
        }

        /// <summary>
        /// Johan's partition algorithm
        /// </summary>
        /// <param name="current">Current position</param>
        /// <param name="destination">Position to move to</param>
        /// <returns>List of true coordinate positions to follow. This could change to delta positions but is done like this to allow accuracy testing.</returns>
        private List<Position> JohannPartitionInSegments(Position current, Position destination)
        {
            List<Position> result = new List<Position>() { current };
            Position difference = destination - current;
            double cartesian_mm = difference.Magnitude;

            if (cartesian_mm < 0.000001)
            {
                cartesian_mm = Math.Abs(difference.E);
            }

            if (cartesian_mm < 0.000001)
            {
                return new List<Position>();
            }
            double seconds = 6000 * cartesian_mm / feedrate / feedmultiply;
            int steps = (int)Math.Max(1, (DELTA_SEGMENTS_PER_SECOND * seconds));

            Position fractionalDestination;

            for (int s = 1; s <= steps; s++)
            {
                double fraction = Convert.ToDouble(s) / Convert.ToDouble(steps);
                fractionalDestination = current + (difference * fraction);
                result.Add(fractionalDestination);
                deltaCalculations++;
            }
            return result;
        }

        /// <summary>
        /// Recursive partitioning algorithm. Split the line into two parts until desired accuracy is reached
        /// </summary>
        /// <param name="start">Start point</param>
        /// <param name="destination">Position to move to</param>
        /// <param name="squaredDistance">Maximum squared distance acceptable</param>
        /// <param name="depth">Instruments depth. Call with 1.</param>
        /// <returns>List of true coordinate positions to follow. This could change to delta positions but is done like this to allow accuracy testing.</returns>
        List<Position> PartitionInSegments(Position start, Position destination, double squaredDistance, int depth)
        {
            Position max;
            double distance = Split(start, destination, out max);
            if (distance <= squaredDistance)
            {
                if (depth > MaxDepth) MaxDepth = depth;
                return new List<Position>() { start, destination };
            }
            else
            {
                List<Position> left = PartitionInSegments(start, max, squaredDistance, depth + 1);
                List<Position> right = PartitionInSegments(max, destination, squaredDistance, depth + 1);
                right.RemoveAt(0);
                left.AddRange(right);
                return left;
            }
        }

        const int MAX_STEPS = 2;

        /// <summary>
        /// Splits the line into two parts. MAX_STEPS determines how many points along the line are examined. Testing has reveiled that there is 
        /// no real benefit to doing more than looking in the middle.
        /// </summary>
        /// <param name="start">Start point</param>
        /// <param name="destination">Position to move to</param>
        /// <param name="max">Returns the position of the split</param>
        /// <returns>The squared distance at the split point</returns>
        double Split(Position start, Position destination, out Position max)
        {
            Position difference = destination - start;
            double maxdistance = 0; max = null;
            for (double step = 1; step < MAX_STEPS; step++)
            {
                Position point = start + (difference * (step / MAX_STEPS));
                DistanceCalculations++;
                double distance = Math.Abs(DeltaPosition(point).SquaredDistanceFromLine(DeltaPosition(start), DeltaPosition(destination)));
                if (distance > maxdistance) { maxdistance = distance; max = point; }
            }

            return maxdistance;
        }

        private Position DeltaPosition(Position destination)
        {
            deltaCalculations++;
            Position delta = new Position();
            delta.X = Math.Sqrt(Math.Pow(DELTA_DIAGONAL_ROD, 2)
                     - Math.Pow(DELTA_TOWER1_X - destination.X, 2)
                     - Math.Pow(DELTA_TOWER1_Y - destination.Y, 2)
                     ) + DELTA_ZERO_OFFSET + destination.Z;
            delta.X = Math.Sqrt(Math.Pow(DELTA_DIAGONAL_ROD, 2)
                         - Math.Pow(DELTA_TOWER2_X - destination.X, 2)
                     - Math.Pow(DELTA_TOWER2_Y - destination.Y, 2)
                     ) + DELTA_ZERO_OFFSET + destination.Z;
            delta.Z = Math.Sqrt(Math.Pow(DELTA_DIAGONAL_ROD, 2)
                     - Math.Pow(DELTA_TOWER3_X - destination.X, 2)
                     - Math.Pow(DELTA_TOWER3_Y - destination.Y, 2)
                     ) + DELTA_ZERO_OFFSET + destination.Z;
            if (Double.IsNaN(delta.X) || Double.IsNaN(delta.Y) || Double.IsNaN(delta.Z))
                throw new ApplicationException("Impossible delta position");
            return delta;
        }

        internal void Translate(GCode code)
        {
            switch (code.Command)
            {
                case GCodeCommand.G1:
                    DeltaProcessG1(code);
                    break;
                case GCodeCommand.G28:
                    current.X = current.Y = current.Z = 0;
                    break;
                default:
                    return;

            }
        }

        private List<GCode> DeltaProcessG1(GCode code)
        {
            Position destination = (Position)current.Clone();
            foreach (GCodeParameter parm in code.Parameters)
            {
                switch (parm.Type)
                {
                    case GCodeParameterType.F:
                        feedrate = Convert.ToDouble(parm.Value);
                        break;
                    case GCodeParameterType.X:
                        destination.X = Convert.ToDouble(parm.Value);
                        break;
                    case GCodeParameterType.Y:
                        destination.X = Convert.ToDouble(parm.Value);
                        break;
                    case GCodeParameterType.Z:
                        destination.X = Convert.ToDouble(parm.Value);
                        break;
                    case GCodeParameterType.E:
                        destination.E = Convert.ToDouble(parm.Value);
                        break;
                    default:
                        throw new ApplicationException("Unknown parameter in G1");
                }
            }
            double precision = Math.Pow(0.05, 2); //mm squared
//            double precision = 0.001; //mm

            deltaCalculations = 0;
            List<Position> candidate = PartitionInSegments(current, destination, precision, 1);
            int candidateCalcs = deltaCalculations;
            deltaCalculations = 0;
            List<Position> johann = JohannPartitionInSegments(current, destination);

            CandidateDeltaSurplus += candidateCalcs - deltaCalculations;
            JohanTotalSteps += johann.Count;
            CandidateTotalSteps += candidate.Count;

            double maxdist = TestPartitionAccuracy(candidate);
            if (maxdist > MaxCandidateError) MaxCandidateError = maxdist;
            maxdist = TestPartitionAccuracy(johann);
            if (maxdist > MaxJohannError) MaxJohannError = maxdist;
            current = destination;
            return null;
        }

    }
}
