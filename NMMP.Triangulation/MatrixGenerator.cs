using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using TriangleNet.Geometry;
using TriangleNet.Meshing;

namespace NMMP.Triangulation
{
    class MatrixGenerator
    {
        public List<DenseMatrix> Ke { get; private set; }
        public List<DenseMatrix> Me { get; private set; }
        public List<Matrix<double>> ReLeft { get; private set; }
        public List<Vector<double>> ReRight { get; private set; }
        public List<Vector<double>> Qe { get; private set; }

        public DenseMatrix A { get; set; }
        public DenseVector B { get; set; }

        private static readonly double[,] ME = { { 2, 1, 1 }, { 1, 2, 1 }, { 1, 1, 2 } };
        private readonly IEnumerable<ITriangle> _triangles;
        private readonly Func<double, double, double> _func;
        private readonly double _d;
        private readonly double[] _a;

        private readonly IEnumerable<Condition> _ntg;
        private readonly double Uc = 1;
        private readonly int _vertexesCount;


        public MatrixGenerator(IMesh mesh, Func<double, double, double> func, double d, double[] a, IEnumerable<Condition> ntg)
        {
            _ntg = ntg;
            _vertexesCount = mesh.Vertices.Count;
            _triangles = mesh.Triangles;
            _func = func;
            _d = d;

            Me = new List<DenseMatrix>();
            Qe = new List<Vector<double>>();
            ReLeft = new List<Matrix<double>>();
            ReRight = new List<Vector<double>>();
            Ke = new List<DenseMatrix>();

            if (a.Length != 2)
            {
                throw new Exception("Bad param a");
            }
            _a = a;
        }

        public void FillMatrixes()
        {
            GenerateMe();
            GenerateQe();
            GenerateKe();
            GenerateRe();

            SummMatrixes();

            var result = A.Solve(B);
        }

        private void SummMatrixes()
        {
            var a = new double[_vertexesCount, _vertexesCount];
            var b = new double[_vertexesCount];
            var triangleIndex = 0;
            foreach (var triangle in _triangles)
            {
                for (var i = 0; i < 3; i++)
                {
                    var ai = triangle.GetVertexID(i);
                    for (var j = 0; j < 3; j++)
                    {
                        var aj = triangle.GetVertexID(j);
                        var temp = Ke[triangleIndex][i, j] + Me[triangleIndex][i, j];
                        a[ai, aj] += (j >= 2 || i >= 2)
                            ? temp
                            : temp + ReLeft[triangleIndex][i, j];
                    }
                    var tempValue = Qe[triangleIndex][i];
                    b[ai] += (i >= 2) 
                        ? tempValue
                        : tempValue + ReRight[triangleIndex][i];

                }
                triangleIndex++;
            }
            A = DenseMatrix.OfArray(a);
            B = DenseVector.OfArray(b);
        }


        private void GenerateKe()
        {
            foreach (var triangle in _triangles)
            {
                var b = GetTriangleSquare(triangle) * 2;

                var vi = triangle.GetVertex(0);
                var vj = triangle.GetVertex(1);
                var vm = triangle.GetVertex(2);

                var pjmCoeffs = GetCoefficients(vj, vm);
                var ipmCoeffs = GetCoefficients(vm, vi);
                var ijpCoeffs = GetCoefficients(vi, vj);

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
                                           Math.Pow(_a[1], 2) * coefsMatrix[i][2] * coefsMatrix[j][2];
                        A[i, j] = beforeDivide / (2 * b);// : (-1)*beforeDivide/(2*b);
                    }
                }


                var matrixToAdd = DenseMatrix.OfArray(A);
                Ke.Add(matrixToAdd);
            }
        }

        private void GenerateQe()
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

        private void GenerateMe()
        {
            foreach (var triangle in _triangles)
            {
                var square = GetTriangleSquare(triangle);
                var matrix = DenseMatrix.OfArray(ME);
                var res = matrix * (2d * square * _d / 24d);
                Me.Add(res);
            }
        }


        private void GenerateRe()
        {
            var matrix = DenseMatrix.OfArray(new double[,] { { 1, 2 }, { 2, 1 } });
            foreach (var side in _ntg)
            {
                var leftSide = side.Sigma / side.Beta * matrix;
                var rigthSide = side.Sigma / side.Beta * matrix;

                var vector = new[] { Uc * side.UcCof, Uc * side.UcCof };
                foreach (var segment in side.Segments)
                {
                    var length = Math.Sqrt(Math.Pow(segment.Vertex1.X - segment.Vertex2.X, 2) +
                                           Math.Pow(segment.Vertex1.Y - segment.Vertex2.Y, 2));

                    var reLeft = (leftSide * length / 6);
                    var reRight = (rigthSide * length / 6) * (new DenseVector(vector));
                    ReLeft.Add(reLeft);
                    ReRight.Add(reRight);
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
