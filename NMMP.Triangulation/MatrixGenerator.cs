using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Topology;

namespace NMMP.Triangulation
{
    class MatrixGenerator
    {
        public List<DenseMatrix> Ke { get; private set; }
        public List<DenseMatrix> Me { get; private set; }
        public List<DenseVector> Re { get; private set; }
        public List<Vector<double>> Qe { get; private set; }

        private static readonly double[,] ME = { { 2, 1, 1 }, { 1, 2, 1 }, { 1, 1, 2 } };
        private readonly IEnumerable<ITriangle> _triangles;
        private readonly Func<double, double, double> _func;
        private readonly double _d;
        private readonly double[] _a;
        private readonly double _sigma;
        private readonly double _beta;
        private readonly IEnumerable<List<Line>> _ntg;


        public MatrixGenerator(IMesh mesh, Func<double, double, double> func, double d, double[] a, IEnumerable<List<Line>> ntg, double sigma, double beta)
        {
            _ntg = ntg;
            _sigma = sigma;
            _beta = beta;
            _triangles = mesh.Triangles;
            _func = func;
            _d = d;

            Me = new List<DenseMatrix>();
            Qe = new List<Vector<double>>();
            Re = new List<DenseVector>();
            Ke = new List<DenseMatrix>();

            if (a.Length != 2)
            {
                throw new Exception("Bad param a");
            }
            _a = a;
        }

        public void FillMatrixes()
        {
            GenerateMeMatrixes();
            GenerateQeMatrixes();
            GenerateKeMatrixes();
            GenerateReMatrixes();
        }

        private void GenerateKeMatrixes()
        {
            foreach (var triangle in _triangles)
            {
                var bIn2 = Math.Pow(GetTriangleSquare(triangle) * 2, 2);
                var pX = 0d;
                var pY = 0d;
                for (var i = 0; i < 3; i++)
                {
                    pX += triangle.GetVertex(i).X;
                    pY += triangle.GetVertex(i).Y;
                }
                pX *= 0.33;
                pY *= 0.33;
                var vp = new Vertex(pX, pY);

                var vj = triangle.GetVertex(1);
                var vm = triangle.GetVertex(2);

                var pjmCoeffs = GetCoefficients(vj, vm);
                var ipmCoeffs = GetCoefficients(vp, vm);
                var ijpCoeffs = GetCoefficients(vj, vp);

                var coefsMatrix = new[]
                {
                    pjmCoeffs,
                    ipmCoeffs,
                    ijpCoeffs
                };

                var A = new double[3, 3];
                for (var i = 0; i < 3; i++)
                {
                    for (var j = 0; j < 3; j++)
                    {
                        var beforeDivide = Math.Pow(_a[0], 2) * coefsMatrix[i][1] * coefsMatrix[j][1] +
                                  Math.Pow(_a[1], 2) * coefsMatrix[i][1] * coefsMatrix[j][2];
                        A[i, j] = beforeDivide / bIn2;
                    }
                }


                var matrixToAdd = DenseMatrix.OfArray(A);
                Ke.Add(matrixToAdd);
            }
        }

        private void GenerateQeMatrixes()
        {
            var j = 0;
            foreach (var triangle in _triangles)
            {
                var f = new double[3];
                for (var i = 0; i < 3; i++)
                {
                    var vertex = triangle.GetVertex(i);
                    f[i] = _func(vertex.X, vertex.Y);
                }
                var vector = Vector.Build.DenseOfArray(f);
                var result = Me[j++].Multiply(vector);
                Qe.Add(result);
            }
        }

        private void GenerateMeMatrixes()
        {
            foreach (var triangle in _triangles)
            {
                var square = GetTriangleSquare(triangle);
                var matrix = DenseMatrix.OfArray(ME);
                //TODO: ask is b = 2 * Sijm ? If yes =>  matrix.Multiply(square * _d / 48);
                matrix.Multiply(2 * square * _d / 24);
                Me.Add(matrix);
            }
        }

        private void GenerateReMatrixes()
        {
            var matrix = DenseMatrix.OfArray(new double[,] { { 1, 2 }, { 2, 1 } });
            var leftSide = _sigma / _beta * matrix;
            var rigthSide = _sigma / _beta * matrix;
            foreach (var side in _ntg)
            {
                foreach (var segment in side)
                {
                    var vector = new[] { _func(segment.Vertex1.X, segment.Vertex1.Y), _func(segment.Vertex2.X, segment.Vertex2.Y) };
                    var re = leftSide * (new DenseVector(vector)) - rigthSide * (new DenseVector(vector));
                    Re.Add(re);
                }
            }
        }

        #region Helpers
        private static double GetTriangleSquare(ITriangle triangle)
        {
            var matrixForSquare = new double[3, 3];
            for (var i = 0; i < 3; i++)
            {
                matrixForSquare[i, 0] = 1;
                matrixForSquare[i, 1] = triangle.GetVertex(i).X;
                matrixForSquare[i, 2] = triangle.GetVertex(i).Y;
            }
            var determinant = DenseMatrix.OfArray(matrixForSquare).Determinant();
            var square = 0.5 * determinant;
            return square;
        }

        private static double[] GetCoefficients(Point vj, Point vm)
        {
            var a = vj.X * vm.Y + (-1) * vm.X * vj.Y;
            var b = vj.Y + (-1) * vm.Y;
            var c = vm.X + (-1) * vj.X;

            var result = new[] { a, b, c };
            return result;
        }
        #endregion
    }
}
