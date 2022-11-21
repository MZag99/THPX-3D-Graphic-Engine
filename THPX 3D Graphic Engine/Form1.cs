using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;

namespace THPX_3D_Graphic_Engine
{
    public partial class Form1 : Form
    {
        private float _time;
        private int _fps;
        private Mesh _meshCube;
        private SolidBrush _b;
        private Pen _p;
        private Graphics _g;
        private Stopwatch _sW = new Stopwatch();
        private bool _shouldClear;
        private List<int> _fpsList = new List<int>();
        private bool _isFilled;

        // Projection matrix parameters
        private float _fov = 90.0f;
        private float _fNear = 0.1f;
        private float _fFar = 1000.0f;
        private float _fAspectRatio;
        private float _fFovRad;
        private float _fTheta;

        // Projection matrix
        private Mat4x4 _matProj;

        struct Vec3d
        {
            public float x, y, z;

            public Vec3d(float a, float b, float c)
            {
                x = a;
                y = b;
                z = c;
            }
        }


        
        struct Triangle
        {
            public Vec3d[] p;

            public Triangle(Vec3d a, Vec3d b, Vec3d c)
            {
                p = new Vec3d[3] { a, b, c };
            }
        }



        struct Mesh
        { 
            public Triangle[] tris;
        }



        struct Mat4x4
        {
            public float[,] mat4;

            public static Vec3d MultiplyMatrixVector(Vec3d i, Mat4x4 m)
            {
                Vec3d o;

                o.x = i.x * m.mat4[0,0] + i.y * m.mat4[1, 0] + i.z * m.mat4[2, 0] + m.mat4[3, 0];
                o.y = i.x * m.mat4[0,1] + i.y * m.mat4[1, 1] + i.z * m.mat4[2, 1] + m.mat4[3, 1];
                o.z = i.x * m.mat4[0,2] + i.y * m.mat4[1, 2] + i.z * m.mat4[2, 2] + m.mat4[3, 2];

                float w = i.x * m.mat4[0, 3] + i.y * m.mat4[1, 3] + i.z * m.mat4[2, 3] + m.mat4[3, 3];

                if (w != 0)
                {
                    o.x/= w; o.y /= w; o.z /= w;
                }

                return o;
            }

        }



        public Form1()
        {
            InitializeComponent();
            backgroundWorker1.WorkerSupportsCancellation = true;
        }



        private void Form1_Activated(object sender, EventArgs e)
        {
            if (backgroundWorker1.IsBusy)
            {
                return;
            }

            // Graphics class instance
            _g = this.CreateGraphics();

            // Solid Brush
            if (_b == null)
            {
                _b = new SolidBrush(Color.White);
            }

            if (_p == null)
            {
                _p = new Pen(Color.White);
            }

            // Populate fields
            _fAspectRatio = (float)this.Height / (float)this.Width;
            _fFovRad = (float)(1.0f / Math.Tan(_fov * 0.5d / 180.0f * 3.14159f));

            // Define projection matrix
            _matProj.mat4 = new float[4,4];
            _matProj.mat4[0, 0] = _fAspectRatio * _fFovRad;
            _matProj.mat4[1, 1] = _fFovRad;
            _matProj.mat4[2, 2] = _fFar / (_fFar - _fNear);
            _matProj.mat4[2, 3] = 1.0f;
            _matProj.mat4[3, 2] = (_fFar * -_fNear) / (_fFar - _fNear);
            _matProj.mat4[3, 3] = 0.0f;

            backgroundWorker1.RunWorkerAsync();
        }



        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            _sW.Start();

            while (!worker.CancellationPending)
            {
                RunEngineLoop();
                Thread.Sleep(5);
            }

            Application.Exit();
        }



        private void RunEngineLoop()
        {
            float timeSpan = (float)_sW.ElapsedMilliseconds;
            
            // Get elapsed time from the last loop cycle (in seconds).
            float elapsed = (timeSpan - _time) * 0.001f;

            _time = timeSpan;

            if (elapsed != 0)
            {
                int f = (int)(1 / elapsed );

                if (_fpsList.Count < 10 && !_isFilled)
                {
                    _fpsList.Add(f);
                }
                else
                {
                    _isFilled = true;

                    _fpsList.RemoveAt(0);
                    _fpsList.Add(f);
                }

                _fps = (int)_fpsList.Average();

                if (_fpsList.Count == 10)
                {
                    this.label1.Invoke((MethodInvoker)delegate
                    {
                        this.label1.Text = $"Fps @ {_fps}";
                    });
                }
                
            }

            OnEngineUpdate(elapsed);
        }



        private void OnEngineUpdate(float fElapsedTime)
        {
            // Clear Screen
            if (_shouldClear)
            {
                _g.Clear(Color.Black);
                _shouldClear = false;
            }

            // Angle changing in time
            _fTheta += fElapsedTime * 1.0f;

            // Rotation matrices
            Mat4x4 matRotZ, matRotX;

            // z-axis
            matRotZ.mat4 = new float[4, 4];
            matRotZ.mat4[0, 0] = (float)Math.Cos(_fTheta);
            matRotZ.mat4[0, 1] = (float)Math.Sin(_fTheta);
            matRotZ.mat4[1, 0] = (float)-Math.Sin(_fTheta);
            matRotZ.mat4[1, 1] = (float)Math.Cos(_fTheta);
            matRotZ.mat4[2, 2] = 1.0f;
            matRotZ.mat4[3, 3] = 1.0f;

            // x-axis
            matRotX.mat4 = new float[4, 4];
            matRotX.mat4[0, 0] = 1;
            matRotX.mat4[1, 1] = (float)Math.Cos(_fTheta * 0.5f);
            matRotX.mat4[1, 2] = (float)Math.Sin(_fTheta * 0.5f);
            matRotX.mat4[2, 1] = (float)-Math.Sin(_fTheta * 0.5f);
            matRotX.mat4[2, 2] = (float)Math.Cos(_fTheta * 0.5f);
            matRotX.mat4[3, 3] = 1.0f;

            // Draw triangles
            foreach (Triangle tri in _meshCube.tris)
            {
                Triangle triProjected, triTranslated, triRotatedZ, triRotatedZX;

                triProjected.p = new Vec3d[3];

                // Rotate in Z axis
                triRotatedZ.p = new Vec3d[3];
                triRotatedZ.p[0] = Mat4x4.MultiplyMatrixVector(tri.p[0], matRotZ);
                triRotatedZ.p[1] = Mat4x4.MultiplyMatrixVector(tri.p[1], matRotZ);
                triRotatedZ.p[2] = Mat4x4.MultiplyMatrixVector(tri.p[2], matRotZ);

                // Rotate in X axis
                triRotatedZX.p = new Vec3d[3];
                triRotatedZX.p[0] = Mat4x4.MultiplyMatrixVector(triRotatedZ.p[0], matRotX);
                triRotatedZX.p[1] = Mat4x4.MultiplyMatrixVector(triRotatedZ.p[1], matRotX);
                triRotatedZX.p[2] = Mat4x4.MultiplyMatrixVector(triRotatedZ.p[2], matRotX);

                // Offset into the screen
                triTranslated = triRotatedZX;
                triTranslated.p[0].z = triRotatedZX.p[0].z + 3.0f;
                triTranslated.p[1].z = triRotatedZX.p[1].z + 3.0f;
                triTranslated.p[2].z = triRotatedZX.p[2].z + 3.0f;

                /*triProjected.p[0] = Mat4x4.MultiplyMatrixVector(tri.p[0], _matProj);
                triProjected.p[1] = Mat4x4.MultiplyMatrixVector(tri.p[1], _matProj);
                triProjected.p[2] = Mat4x4.MultiplyMatrixVector(tri.p[2], _matProj);*/
                
                // Project triangles from 3D to 2D
                triProjected.p[0] = Mat4x4.MultiplyMatrixVector(triTranslated.p[0], _matProj);
                triProjected.p[1] = Mat4x4.MultiplyMatrixVector(triTranslated.p[1], _matProj);
                triProjected.p[2] = Mat4x4.MultiplyMatrixVector(triTranslated.p[2], _matProj);

                // Scale into view
                triProjected.p[0].x += 1.0f; triProjected.p[0].y += 1.0f;
                triProjected.p[1].x += 1.0f; triProjected.p[1].y += 1.0f;
                triProjected.p[2].x += 1.0f; triProjected.p[2].y += 1.0f;

                triProjected.p[0].x *= 0.5f * (float)this.Width;
                triProjected.p[0].y *= 0.5f * (float)this.Height;
                triProjected.p[1].x *= 0.5f * (float)this.Width;
                triProjected.p[1].y *= 0.5f * (float)this.Height;
                triProjected.p[2].x *= 0.5f * (float)this.Width;
                triProjected.p[2].y *= 0.5f * (float)this.Height;

                DrawTriangle(triProjected.p);
            }
        }



        private void DrawTriangle(Vec3d[] arg)
        {
            Point[] pts =  new Point[3];

            pts[0].X = (int)arg[0].x;
            pts[0].Y = (int)arg[0].y;
            pts[1].X = (int)arg[1].x;
            pts[1].Y = (int)arg[1].y;
            pts[2].X = (int)arg[2].x;
            pts[2].Y = (int)arg[2].y;

            _g.DrawPolygon(_p, pts);

            _shouldClear = true;
        }



        private void Form1_Load(object sender, EventArgs e)
        {
            _meshCube.tris = new Triangle[] {
                // South
                new Triangle( new Vec3d(0, 0, 0), new Vec3d(0, 1, 0), new Vec3d(1, 1, 0)),
                new Triangle( new Vec3d(1, 1, 0), new Vec3d(1, 0, 0), new Vec3d(0, 0, 0)),
                // East
                new Triangle( new Vec3d(1, 0, 0), new Vec3d(1, 1, 0), new Vec3d(1, 1, 1)),
                new Triangle( new Vec3d(1, 1, 1),new Vec3d(1, 0, 1),new Vec3d(1, 0, 0)),
                // North
                new Triangle( new Vec3d(1, 0, 1), new Vec3d(1, 1, 1), new Vec3d(0, 1, 1)),
                new Triangle( new Vec3d(0, 1, 1), new Vec3d(0, 0, 1), new Vec3d(1, 0, 1)),
                // West
                new Triangle( new Vec3d(0, 0, 1), new Vec3d(0, 1, 1), new Vec3d(0, 1, 0)),
                new Triangle( new Vec3d(0, 1, 0), new Vec3d(0, 0, 0), new Vec3d(0, 0, 1)),
                // Bottom
                new Triangle( new Vec3d(0, 0, 0), new Vec3d(1, 0, 0), new Vec3d(1, 0, 1)),
                new Triangle( new Vec3d(1, 0, 1), new Vec3d(0, 0, 1), new Vec3d(0, 0, 0)),
                // Top
                new Triangle( new Vec3d(0, 1, 0), new Vec3d(0, 1, 1), new Vec3d(1, 1, 1)),
                new Triangle( new Vec3d(1, 1, 1), new Vec3d(1, 1, 0), new Vec3d(0, 1, 0)),
            };
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
        }
    }
}