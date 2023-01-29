using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace THPX_3D_Graphic_Engine
{
    internal class FileLoader
    {
        public static List<Main.Triangle> LoadObjFile()
        {
            var d = SetupDialog();

            if (d.ShowDialog() == DialogResult.OK)
            {
                Stream fileStream = d.OpenFile();
                var vecList = GetVectorList(fileStream);

                return GetTriangleList(fileStream, vecList);

            } else
            {
                MessageBox.Show("Error", "Unable to load this file!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw new Exception();
            }
        }



        private static OpenFileDialog SetupDialog()
        {
            OpenFileDialog d = new OpenFileDialog();

            d.Title = "Load object file";
            d.Filter = "Object files(*.obj) | *.obj";
            d.DefaultExt = "obj";

            return d;
        }



        private static List<Main.Vec3d> GetVectorList(Stream fS)
        {
            var vecList = new List<Main.Vec3d>();

            StreamReader reader = new StreamReader(fS);
            string line;

            Debug.WriteLine("VEC: " + fS.Position.ToString());

            while ((line = reader.ReadLine()) != null)
            {
                if (line[0] == 'v')
                {
                    vecList.Add(FileLoader.LineToVec(line));
                }
            }

            return vecList;
        }



        private static List<Main.Triangle> GetTriangleList(Stream fS, List<Main.Vec3d> vList)
        {
            var triList = new List<Main.Triangle>();

            StreamReader reader = new StreamReader(fS);
            string line;

            fS.Position = 0;

            while ((line = reader.ReadLine()) != null)
            {
                if (line[0] == 'f')
                {
                    triList.Add(FileLoader.LineToTri(vList, line));
                }
            }

            return triList;
        }



        private static Main.Vec3d LineToVec(string l)
        {
            string[] split = l.Remove(0, 2).Split(' ');
            float[] tempArr = split.Select(s => float.Parse(s, CultureInfo.InvariantCulture.NumberFormat)).ToArray();

            return new Main.Vec3d(tempArr[0], tempArr[1], tempArr[2]);
        }



        private static Main.Triangle LineToTri(List<Main.Vec3d> vList,string l)
        {
            string[] split = l.Remove(0, 2).Split(' ');
            float[] tempArr = split.Select(s => float.Parse(s, CultureInfo.InvariantCulture.NumberFormat)).ToArray();

            return new Main.Triangle(vList[Int32.Parse(split[0]) - 1], vList[Int32.Parse(split[1]) - 1], vList[Int32.Parse(split[2]) - 1]);
        }
    }
}
