using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TriangleNet.Geometry;
using TriangleNet.Meshing;

namespace NMMP.Triangulation
{
    public partial class Form1 : Form
    {
        private IMesh _mesh;
        private int _figure = 1;
        private List<ISegment> _segments;

        public Form1()
        {
            InitializeComponent();
            button1.Enabled = false;
        }

        private double _func(double x, double y) => 1d;

        private void DrawLines(Vertex[] vertices)
        {
            var graphics = panel1.CreateGraphics();
            graphics.Clear(Color.White);
            foreach (var e in _mesh.Edges)
            {
                var v0 = vertices[e.P0];
                var v1 = vertices[e.P1];
                graphics.DrawLine(Pens.Red, (float)v0.X * 100 + 100, (float)v0.Y * 100 + 100, (float)v1.X * 100 + 100, (float)v1.Y * 100 + 100);
            }
        }

        private void DrawPoints()
        {
            var graphics = panel1.CreateGraphics();
            foreach (var e in _mesh.Vertices)
            {
                graphics.FillEllipse(Brushes.Green, (float)e.X * 100 + 99, (float)e.Y * 99 + 99, 5, 5);
                graphics.DrawString($"{e.ID}", DefaultFont, Brushes.Black, (float)e.X * 100 + 100, (float)e.Y * 100 + 100);
            }
            foreach (var e in _mesh.Triangles)
            {

                var t1 = e.GetVertex(0);
                var t2 = e.GetVertex(1);
                var t3 = e.GetVertex(2);
                var avgX = (t1.X * 100 + t2.X * 100 + t3.X * 100 + 300) / 3;
                var avgY = (t1.Y * 100 + t2.Y * 100 + t3.Y * 100 + 300) / 3;
                graphics.FillEllipse(Brushes.Green, (float)avgX, (float)avgY, 5, 5);
                graphics.DrawString($"{e.ID}", DefaultFont, Brushes.Green, (float)avgX, (float)avgY);
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {

            var polygon = GeneratePolygone();
            var area = Convert.ToDouble(textBox1.Text.Replace('.', ','));
            var options = new ConstraintOptions() { ConformingDelaunay = true };
            var quality = new QualityOptions() { MinimumAngle = 30, MaximumArea = area };

            _mesh = polygon.Triangulate(options, quality);

            DrawLines(_mesh.Vertices.ToArray());
            DrawPoints();

            var nt = _mesh.Triangles.Select(el => new List<Vertex>()
                {
                    el.GetVertex(0),
                    el.GetVertex(1),
                    el.GetVertex(2)
                });

            var ct = _mesh.Vertices;
            var ntg = GetNtgSegments();

            DataToJsonWriter.Write(nt.ToList(), "NT.json");
            DataToJsonWriter.Write(ct.ToList(), "CT.json");
            DataToJsonWriter.Write(ntg.ToList(), "NTG.json");

            var sigma = Convert.ToDouble(tbSigma.Text.Replace('.', ',')); ;
            var beta = Convert.ToDouble(tbBeta.Text.Replace('.', ',')); ;
            var aVect = new double[]
            {
                Convert.ToDouble(tbA11.Text.Replace('.', ',')),
                Convert.ToDouble(tbA22.Text.Replace('.', ','))
        };
            var d = Convert.ToDouble(tbD.Text.Replace('.', ',')); ;
            MatrixGenerator gen = new MatrixGenerator(_mesh, _func, d, aVect, ntg, sigma, beta);

            gen.FillMatrixes();

        }
        #region RadioBtnChange

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            _figure = 1;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            _figure = 2;

        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            _figure = 3;

        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            _figure = 4;

        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            _figure = 5;

        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            _figure = 6;

        }
        #endregion

        private IEnumerable<List<Line>> GetNtgSegments()
        {
            var result =
                _segments.Select(segment => (from el in _mesh.Segments
                let frstVertex = el.GetVertex(0)
                let secondVertex = el.GetVertex(1)
                let firstFlag =
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    ((frstVertex.X - segment.GetVertex(0).X)*(segment.GetVertex(1).Y - segment.GetVertex(0).Y) ==
                     (segment.GetVertex(1).X - segment.GetVertex(0).X)*(frstVertex.Y - segment.GetVertex(0).Y))
                let secondFlag =
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    ((secondVertex.X - segment.GetVertex(0).X)*(segment.GetVertex(1).Y - segment.GetVertex(0).Y) ==
                     (segment.GetVertex(1).X - segment.GetVertex(0).X)*(secondVertex.Y - segment.GetVertex(0).Y))
                where firstFlag && secondFlag
                select el).ToList())
                .Select(segmentsToAdd => segmentsToAdd.Select(el => new Line(el.GetVertex(0), el.GetVertex(1))).ToList())
                .ToList();

            return result;
        }

        private Polygon GeneratePolygone()
        {
            var polygon = new Polygon();

            #region switch
            switch (_figure)
            {
                case 1:
                    {
                        polygon.Add(new Contour(new[]
                        {
                        new Vertex(0.0, 0.0),
                        new Vertex(1.0, 0.0),
                        new Vertex(0.0, 1.0),
                    }));
                    }
                    break;
                case 2:
                    {
                        polygon.Add(new Contour(new[]
                        {
                        new Vertex(0.0, 0.0),
                        new Vertex(0.0, 2.0),
                        new Vertex(1.0, 2.0),
                        new Vertex(1.0, 0.0),
                    }));
                    }
                    break;
                case 3:
                    {
                        polygon.Add(new Contour(new[]
                        {
                        new Vertex(1.0, 0.0),
                        new Vertex(2.0, 0.0),
                        new Vertex(0.0, 2.0),
                        new Vertex(0.0, 1.0),
                    }));
                    }
                    break;
                case 4:
                    {
                        polygon.Add(new Contour(new[]
                        {
                        new Vertex(0.0, 0.0),
                        new Vertex(3.0, 3.0),
                        new Vertex(6.0, 0.0),
                    }));
                    }
                    break;
                case 5:
                    {
                        polygon.Add(new Contour(new[]
                        {
                        new Vertex(0.0, 0.0),
                        new Vertex(0.0, 1.0),
                        new Vertex(4.0, 1.0),
                        new Vertex(4.0, 0.0),
                    }));
                    }
                    break;
                case 6:
                    {
                        polygon.Add(new Contour(new[]
                        {
                        new Vertex(0.0, 0.0),
                        new Vertex(3.0, 3.0),
                        new Vertex(6.0, 0.0),
                        new Vertex(4.0, 0.0),
                        new Vertex(3.0, 1.0),
                        new Vertex(2.0, 0.0),
                    }));
                    }
                    break;
            }
            #endregion
            _segments = polygon.Segments;
            return polygon;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                var area = double.Parse(textBox1.Text.Replace('.', ','));
                if (area > 0)
                {
                    button1.Enabled = true;
                    return;
                }
                button1.Enabled = false;
            }
            catch
            {
                button1.Enabled = false;
            }
        }
    }
}
