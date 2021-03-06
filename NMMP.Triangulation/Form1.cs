﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Topology;

namespace NMMP.Triangulation
{
    public partial class Form1 : Form
    {
        private IMesh _mesh;
        private int _figure = 1;
        private List<ISegment> _segments;
        private double fValue;
        private Dictionary<int, List<Condition>> conditions = new Dictionary<int, List<Condition>>();

        public Form1()
        {
            InitializeComponent();
            button1.Enabled = false;
        }

        private double _func(double x, double y) => fValue;

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
            var temp = tbSigma.Text.Replace("0", "0,0000001");
            var sigmas = temp.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Convert.ToDouble(s));
            var betas = tbBeta.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(b => Convert.ToDouble(b));
            var d = tbD.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(b => Convert.ToDouble(b));
            fValue = Convert.ToDouble(textBox2.Text.Replace('.', ','));
            GenerateConditions(sigmas.ToArray(), betas.ToArray(), d.ToArray());


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

            var aVect = new[]
            {
                Convert.ToDouble(tbA11.Text.Replace('.', ',')),
                Convert.ToDouble(tbA22.Text.Replace('.', ','))
            };
           
            var gen = new MatrixGenerator(_mesh, _func, 1, aVect, ntg);

            gen.FillMatrixes();

            DataToJsonWriter.Write(gen.Ke.Select(k => k.Storage).ToList(), "KE.json");
            DataToJsonWriter.Write(gen.Qe, "Qe.json");
            DataToJsonWriter.Write(gen.Me.Select(k => k.Storage).ToList(), "Me.json");
            DataToJsonWriter.Write(gen.ReLeft.Select(k => k.Storage).ToList(), "ReLeft.json");
            DataToJsonWriter.Write(gen.ReRight, "ReRight.json");

            DataToJsonWriter.WriteOne(gen.A.ToArray(), "A.json");
            DataToJsonWriter.WriteOne(gen.B.ToArray(), "B.json");

            var result = gen.A.Solve(gen.B);
            DataToJsonWriter.WriteOne(result, "x.json");

            WriteResultsToFile(_mesh, result.ToList(), @"C:\Users\Андрій\Documents\nmmf\SolvedResult.txt");

        }

        private void WriteResultsToFile(IMesh mesh, List<double> z, string path)
        {
            var i = 0;
            using (var sr = new StreamWriter(path))
            {
                foreach (var vert in mesh.Vertices)
                {
                    string toWrite = $"{vert.Y.ToString().Replace(',', '.')} {vert.X.ToString().Replace(',', '.')} {z[i++].ToString().Replace(',', '.')}";
                    sr.WriteLine(toWrite);
                }
            }

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

        private void GenerateConditions(double[] sigmaValue, double[] bethaValue, double[] dValues)
        {
            //var firstCondition = new Condition(Math.Pow(0.1, 6), sigmaValue[0], 2);
            //var secondCondition = new Condition(Math.Pow(0.1, 6), sigmaValue[0], Math.Pow(0.1, 6));
            //var thirdCondition = new Condition(bethaValue[0], sigmaValue[0], 1);
            //1
            var cond1 = new List<Condition>()
            {
                new Condition(bethaValue[0] == 0 ? Math.Pow(0.1, 6) : bethaValue[0], sigmaValue[0], dValues[0] == 0 ? Math.Pow(0.1, 6) : dValues[0]),
                new Condition(bethaValue[1] == 0 ? Math.Pow(0.1, 6) : bethaValue[1], sigmaValue[1], dValues[1] == 0 ? Math.Pow(0.1, 6) : dValues[1]),
                new Condition(bethaValue[2] == 0 ? Math.Pow(0.1, 6) : bethaValue[2], sigmaValue[2], dValues[2] == 0 ? Math.Pow(0.1, 6) : dValues[2])
            };                                                                                      
            //2                                                                                    
            var cond2 = new List<Condition>() {                                                    
                new Condition(bethaValue[0] == 0 ? Math.Pow(0.1, 6) : bethaValue[0], sigmaValue[0], dValues[0] == 0 ? Math.Pow(0.1, 6) : dValues[0]),
                new Condition(bethaValue[1] == 0 ? Math.Pow(0.1, 6) : bethaValue[1], sigmaValue[1], dValues[1] == 0 ? Math.Pow(0.1, 6) : dValues[1]),
                new Condition(bethaValue[2] == 0 ? Math.Pow(0.1, 6) : bethaValue[2], sigmaValue[2], dValues[2] == 0 ? Math.Pow(0.1, 6) : dValues[2]),
                new Condition(bethaValue[3] == 0 ? Math.Pow(0.1, 6) : bethaValue[3], sigmaValue[3], dValues[3] == 0 ? Math.Pow(0.1, 6) : dValues[3])
            };                                                                                      
            //3                                                                                     
            var cond3 = new List<Condition>()                                                       
            {                                                                                         
                new Condition(bethaValue[0] == 0 ? Math.Pow(0.1, 6) : bethaValue[0], sigmaValue[0], dValues[0] == 0 ? Math.Pow(0.1, 6) : dValues[0]),
                new Condition(bethaValue[1] == 0 ? Math.Pow(0.1, 6) : bethaValue[1], sigmaValue[1], dValues[1] == 0 ? Math.Pow(0.1, 6) : dValues[1]),
                new Condition(bethaValue[2] == 0 ? Math.Pow(0.1, 6) : bethaValue[2], sigmaValue[2], dValues[2] == 0 ? Math.Pow(0.1, 6) : dValues[2]),
                new Condition(bethaValue[3] == 0 ? Math.Pow(0.1, 6) : bethaValue[3], sigmaValue[3], dValues[3] == 0 ? Math.Pow(0.1, 6) : dValues[3])
            };                                                                                      
            //4                                                                                     
            var cond4 = new List<Condition>()                                                       
            {                                                                                         
                new Condition(bethaValue[0] == 0 ? Math.Pow(0.1, 6) : bethaValue[0], sigmaValue[0], dValues[0] == 0 ? Math.Pow(0.1, 6) : dValues[0]),
                new Condition(bethaValue[1] == 0 ? Math.Pow(0.1, 6) : bethaValue[1], sigmaValue[1], dValues[1] == 0 ? Math.Pow(0.1, 6) : dValues[1]),
                new Condition(bethaValue[2] == 0 ? Math.Pow(0.1, 6) : bethaValue[2], sigmaValue[2], dValues[2] == 0 ? Math.Pow(0.1, 6) : dValues[2])
            };
            //5
            var cond5 = new List<Condition>()
            {
                new Condition(bethaValue[0] == 0 ? Math.Pow(0.1, 6) : bethaValue[0], sigmaValue[0], dValues[0] == 0 ? Math.Pow(0.1, 6) : dValues[0]),
                new Condition(bethaValue[1] == 0 ? Math.Pow(0.1, 6) : bethaValue[1], sigmaValue[1], dValues[1] == 0 ? Math.Pow(0.1, 6) : dValues[1]),
                new Condition(bethaValue[2] == 0 ? Math.Pow(0.1, 6) : bethaValue[2], sigmaValue[2], dValues[2] == 0 ? Math.Pow(0.1, 6) : dValues[2]),
                new Condition(bethaValue[3] == 0 ? Math.Pow(0.1, 6) : bethaValue[3], sigmaValue[3], dValues[3] == 0 ? Math.Pow(0.1, 6) : dValues[3])
            };
            //6
            var cond6 = new List<Condition>()
            {
                new Condition(bethaValue[0] == 0 ? Math.Pow(0.1, 6) : bethaValue[0], sigmaValue[0], 1),
                new Condition(bethaValue[1] == 0 ? Math.Pow(0.1, 6) : bethaValue[1], sigmaValue[1], 1),
                new Condition(bethaValue[2] == 0 ? Math.Pow(0.1, 6) : bethaValue[2], sigmaValue[2], 1),
                new Condition(bethaValue[3] == 0 ? Math.Pow(0.1, 6) : bethaValue[3], sigmaValue[3], 1),
                new Condition(bethaValue[4] == 0 ? Math.Pow(0.1, 6) : bethaValue[4], sigmaValue[4], 1),
                new Condition(bethaValue[5] == 0 ? Math.Pow(0.1, 6) : bethaValue[5], sigmaValue[5], 1)
            };

            conditions = new Dictionary<int, List<Condition>>
            {
                {1, cond1},
                {2, cond2},
                {3, cond3},
                {4, cond4},
                {5, cond5},
                {6, cond6}
            };
        }

        private IEnumerable<Condition> GetNtgSegments()
        {
            var lines =
                        _segments.Select(segment => (
                            from el in _mesh.Segments
                            let frstVertex = el.GetVertex(0)
                            let secondVertex = el.GetVertex(1)
                            let firstFlag = ((frstVertex.X - segment.GetVertex(0).X) * (segment.GetVertex(1).Y - segment.GetVertex(0).Y)
                                             == (segment.GetVertex(1).X - segment.GetVertex(0).X) * (frstVertex.Y - segment.GetVertex(0).Y))
                            let secondFlag = ((secondVertex.X - segment.GetVertex(0).X) * (segment.GetVertex(1).Y - segment.GetVertex(0).Y)
                                              == (segment.GetVertex(1).X - segment.GetVertex(0).X) * (secondVertex.Y - segment.GetVertex(0).Y))
                            where firstFlag && secondFlag
                            select el)
                            .ToList())
                        .Select(segmentsToAdd => segmentsToAdd.Select(el => new Line(el.GetVertex(0), el.GetVertex(1))).ToList())
                        .ToList();


            List<Condition> conds;
            if (!conditions.TryGetValue(_figure, out conds)) return null;
            if (conds.Count != lines.Count) return conds;
            for (var i = 0; i < conds.Count; i++)
            {
                conds[i].Segments = lines[i];
            }
            return conds;
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

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
