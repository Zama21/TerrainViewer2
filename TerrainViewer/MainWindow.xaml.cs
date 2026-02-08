using HelixToolkit.Wpf;
using OSGeo.GDAL;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace TerrainViewer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTerrain(@"D:\terrain\terrain.tif");
        }

        private void LoadTerrain(string path)
        {
            Gdal.AllRegister();

            var ds = Gdal.Open(path, Access.GA_ReadOnly);
            if (ds == null)
            {
                MessageBox.Show("Не удалось открыть TIFF");
                return;
            }

            int width = ds.RasterXSize;
            int height = ds.RasterYSize;

            var band = ds.GetRasterBand(1);

            float[] elevation = new float[width * height];
            band.ReadRaster(0, 0, width, height,
                            elevation, width, height, 0, 0);

            double[] geo = new double[6];
            ds.GetGeoTransform(geo);

            // Гео-параметры
            double pixelSizeX = geo[1];         // градусы
            double pixelSizeY = geo[5];         // градусы (отриц)
            double metersPerDegree = 111320.0;  // приближение

            double scaleX = pixelSizeX * metersPerDegree;
            double scaleY = -pixelSizeY * metersPerDegree;
            double scaleZ = 1.0; // высоты уже в метрах

            int step = 4; // downsample (очень важно)

            var mesh = new MeshGeometry3D();

            // Вершины
            for (int y = 0; y < height; y += step)
            {
                for (int x = 0; x < width; x += step)
                {
                    int i = y * width + x;
                    double z = elevation[i] * scaleZ;

                    mesh.Positions.Add(new Point3D(
                        x * scaleX,
                        z,
                        y * scaleY
                    ));
                }
            }

            int w = width / step;

            // Треугольники
            for (int y = 0; y < (height / step) - 1; y++)
            {
                for (int x = 0; x < (width / step) - 1; x++)
                {
                    int i0 = y * w + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + w;
                    int i3 = i2 + 1;

                    mesh.TriangleIndices.Add(i0);
                    mesh.TriangleIndices.Add(i2);
                    mesh.TriangleIndices.Add(i1);

                    mesh.TriangleIndices.Add(i1);
                    mesh.TriangleIndices.Add(i2);
                    mesh.TriangleIndices.Add(i3);
                }
            }

            mesh.Normals = HelixToolkit.Geometry.MeshGeometryHelper.CalculateNormals(mesh);

            var model = new GeometryModel3D
            {
                Geometry = mesh,
                Material = MaterialHelper.CreateMaterial(Brushes.LightGray),
                BackMaterial = MaterialHelper.CreateMaterial(Brushes.LightGray)
            };

            View.Children.Add(new ModelVisual3D
            {
                Content = model
            });
        }
    }
}
