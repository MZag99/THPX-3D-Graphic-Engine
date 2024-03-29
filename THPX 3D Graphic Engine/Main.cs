using System.ComponentModel;
using System.Diagnostics;

namespace THPX_3D_Graphic_Engine
{
    public partial class Main : Form
    {
        // Public fields
        public Mesh ObjMesh;



        // Private fields
        private float _time;
        private float _felapsed;
        private int _fps;
        private SolidBrush _b;
        private Pen _p;
        private Stopwatch _sW = new Stopwatch();
        private List<int> _fpsList = new List<int>(); // List holding the last fps values.
        private bool _isFilled; // Has the _fpsList reached the _fpsCount number;
        private byte _fpsCount = 10; // How many frames are used to calculate average fps.



        //Camera
        private Vec3d _camera = new Vec3d(0, 0, 0);



        // Projection matrix parameters
        private float _fov = 90.0f;
        private float _fNear = 0.1f;
        private float _fFar = 1000.0f;
        private float _fAspectRatio;
        private float _fFovRad;
        private float _fTheta;



        // Projection matrix
        private Mat4x4 _matProj;



        public struct Vec3d
        {
            public float x, y, z;

            public Vec3d(float a, float b, float c)
            {
                x = a;
                y = b;
                z = c;
            }
        }


        
        public struct Triangle
        {
            public Vec3d[] p;
            public Color col = new Color();


            public Triangle(Vec3d a, Vec3d b, Vec3d c)
            {
                p = new Vec3d[3] { a, b, c };
            }
        }



        public struct Mesh
        { 
            public List<Triangle> tris;
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



        public Main()
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

            // Solid Brush
            if (_b == null)
            {
                _b = new SolidBrush(Color.White);
            }

            if (_p == null)
            {
                _p = new Pen(Color.Black);
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
            }

            Application.Exit();
        }



        private void RunEngineLoop()
        {
            float timeSpan = (float)_sW.ElapsedMilliseconds;
            
            // Get elapsed time from the last loop cycle (in seconds).
            _felapsed = (timeSpan - _time) * 0.001f;

            _time = timeSpan;

            if (_felapsed != 0)
            {
                int f = (int)(1 / _felapsed );

                if (_fpsList.Count < _fpsCount && !_isFilled)
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

                if (_fpsList.Count == _fpsCount)
                {
                    this.label1.Invoke((MethodInvoker)delegate
                    {
                        this.label1.Text = $"Fps @ {_fps}";
                    });
                }
                
            }

            if (!this.IsDisposed)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    this.pictureBox1.Refresh();
                });
            }
        }



        private void OnEngineUpdate(float fElapsedTime, Graphics g)
        {
            // Angle changing in time
            _fTheta += fElapsedTime * 1.0f;

            // Rotation matrices
            Mat4x4 matRotZ, matRotX;

            // Depth buffer
            List<Triangle> depthBuffer = new List<Triangle>();

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

            if (ObjMesh.tris == null) {  return; }

            // Draw triangles
            foreach (Triangle tri in ObjMesh.tris)
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
                triRotatedZX.col = new Color();
                triRotatedZX.p[0] = Mat4x4.MultiplyMatrixVector(triRotatedZ.p[0], matRotX);
                triRotatedZX.p[1] = Mat4x4.MultiplyMatrixVector(triRotatedZ.p[1], matRotX);
                triRotatedZX.p[2] = Mat4x4.MultiplyMatrixVector(triRotatedZ.p[2], matRotX);

                // Offset into the screen
                triTranslated = triRotatedZX;
                triTranslated.p[0].z = triRotatedZX.p[0].z + 3.0f;
                triTranslated.p[1].z = triRotatedZX.p[1].z + 3.0f;
                triTranslated.p[2].z = triRotatedZX.p[2].z + 3.0f;

                // Calculate triangle normal
                Vec3d normal, line1, line2;
                line1.x = triTranslated.p[1].x - triTranslated.p[0].x;
                line1.y = triTranslated.p[1].y - triTranslated.p[0].y;
                line1.z = triTranslated.p[1].z - triTranslated.p[0].z;

                line2.x = triTranslated.p[2].x - triTranslated.p[0].x;
                line2.y = triTranslated.p[2].y - triTranslated.p[0].y;
                line2.z = triTranslated.p[2].z - triTranslated.p[0].z;

                normal.x = line1.y * line2.z - line1.z * line2.y;
                normal.y = line1.z * line2.x - line1.x * line2.z;
                normal.z = line1.x * line2.y - line1.y * line2.x;

                float l = (float)Math.Sqrt(normal.x * normal.x + normal.y * normal.y + normal.z * normal.z);
                normal.x /= l; normal.y /= l; normal.z /= l;

                //if (normal.z < 0)
                if (normal.x * (triTranslated.p[0].x - _camera.x) +
                    normal.y * (triTranslated.p[0].y - _camera.y) +
                    normal.z * (triTranslated.p[0].z - _camera.z) < 0)
                {

                    // Illumination
                    Vec3d light_direction = new Vec3d(0.0f, 0.0f, -1.0f);

                    float length = (float)Math.Sqrt(light_direction.x * light_direction.x + light_direction.y * light_direction.y + light_direction.z * light_direction.z);

                    normal.x /= length;
                    normal.y /= length;
                    normal.z /= length;

                    // How similiar is normal to light direction
                    float dp = normal.x * light_direction.x + normal.y * light_direction.y + normal.z * light_direction.z;
                    triProjected.col = Color.FromArgb(Math.Max(0, (int)(dp * 255)), Math.Max(0, (int)(dp * 255)), Math.Max(0, (int)(dp * 255)));

                    // Project triangles from 3D to 2D
                    triProjected.p[0] = Mat4x4.MultiplyMatrixVector(triTranslated.p[0], _matProj);
                    triProjected.p[1] = Mat4x4.MultiplyMatrixVector(triTranslated.p[1], _matProj);
                    triProjected.p[2] = Mat4x4.MultiplyMatrixVector(triTranslated.p[2], _matProj);

                    // Scale into view
                    triProjected.p[0].x += 1.0f; triProjected.p[0].y += 1.0f;
                    triProjected.p[1].x += 1.0f; triProjected.p[1].y += 1.0f;
                    triProjected.p[2].x += 1.0f; triProjected.p[2].y += 1.0f;

                    triProjected.p[0].x *= 0.5f * (float)this.pictureBox1.Width;
                    triProjected.p[0].y *= 0.5f * (float)this.pictureBox1.Height;
                    triProjected.p[1].x *= 0.5f * (float)this.pictureBox1.Width;
                    triProjected.p[1].y *= 0.5f * (float)this.pictureBox1.Height;
                    triProjected.p[2].x *= 0.5f * (float)this.pictureBox1.Width;
                    triProjected.p[2].y *= 0.5f * (float)this.pictureBox1.Height;

                    depthBuffer.Add(triProjected);
                }
            }

            SortTriangles(depthBuffer);
            DrawTriangles(depthBuffer, g);
        }



        private void SortTriangles(List<Triangle> dB)
        {
            dB.Sort((Triangle tri1, Triangle tri2) =>
            {
                float z1 = (tri1.p[0].z + tri1.p[1].z + tri1.p[2].z) / 3.0f;
                float z2 = (tri2.p[0].z + tri2.p[1].z + tri2.p[2].z) / 3.0f;

                return z2.CompareTo(z1);
            });
        }



        private void DrawTriangles(List<Triangle> dB, Graphics g)
        {
            foreach(Triangle tri in dB)
            {
                DrawTriangle(tri.p, tri.col, g);
            }
        }



        private void DrawTriangle(Vec3d[] arg, Color col, Graphics g)
        {
            Point[] pts =  new Point[3];
            _b.Color = col;

            pts[0].X = (int)arg[0].x;
            pts[0].Y = (int)arg[0].y;
            pts[1].X = (int)arg[1].x;
            pts[1].Y = (int)arg[1].y;
            pts[2].X = (int)arg[2].x;
            pts[2].Y = (int)arg[2].y;

            g.FillPolygon(_b, pts);
            //g.DrawPolygon(_p, pts);
        }



        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            OnEngineUpdate(_felapsed, e.Graphics);
        }



        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            this.Parent = null;
            e.Cancel = true;
        }



        private void LoadButton_Click(object sender, EventArgs e)
        {
            ObjMesh.tris = FileLoader.LoadObjFile();
        }
    }
}