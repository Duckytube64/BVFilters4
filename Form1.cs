using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;

namespace INFOIBV
{
    public partial class INFOIBV : Form
    {
        private Bitmap InputImage;
        private Bitmap OutputImage;
        Color[,] Image;
        bool doubleProgress = false;
        string modeSize, mode;
        bool[,] edges, H;
        List<Point> neighbourPriority = new List<Point>();

        public INFOIBV()
        {
            InitializeComponent();
        }

        private void LoadImageButton_Click(object sender, EventArgs e)
        {
            if (openImageDialog.ShowDialog() == DialogResult.OK)             // Open File Dialog
            {
                string file = openImageDialog.FileName;                     // Get the file name
                imageFileName.Text = file;                                  // Show file name
                if (InputImage != null) InputImage.Dispose();               // Reset image
                InputImage = new Bitmap(file);                              // Create new Bitmap from file
                if (InputImage.Size.Height <= 0 || InputImage.Size.Width <= 0 ||
                    InputImage.Size.Height > 512 || InputImage.Size.Width > 512) // Dimension check
                    MessageBox.Show("Error in image dimensions (have to be > 0 and <= 512)");
                else
                    pictureBox1.Image = (Image)InputImage;                 // Display input image
            }
        }

        //This project was made by:
        //Steven van Blijderveen	5553083
        //Jeroen Hijzelendoorn		6262279
        //As an assignment to be delivered by at most sunday 22 september 2019

        private void applyButton_Click(object sender, EventArgs e)
        {
            string filter = (string)comboBox1.SelectedItem;
            if (filter == "Structuring element")                            // This function should also work when no image is chosen yet
            {
                mode = comboBox3.Text;
                modeSize = textBox2.Text;
                SetH();

                string message = "Structuring element set as: \n";
                int size = int.Parse(modeSize);
                for (int x = 0; x < size * 2 - 1; x++)
                {
                    for (int y = 0; y < size * 2 - 1; y++)
                    {
                        if (H[x, y])
                        {
                            message += "1 ";
                        }
                        else
                        {
                            message += "0 ";
                        }
                    }
                    message += "\n";
                }

                MessageBox.Show(message, "Structuring element visual", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            if (InputImage == null) return;                                 // Get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // Reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // Create new output image
            Image = new Color[InputImage.Size.Width, InputImage.Size.Height];       // Create array to speed-up operations (Bitmap functions are very slow)

            // Setup progress bar
            progressBar.Visible = true;
            progressBar.Minimum = 1;
            progressBar.Maximum = InputImage.Size.Width * InputImage.Size.Height;
            progressBar.Value = 1;
            progressBar.Step = 1;

            // Copy input Bitmap to array            
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Image[x, y] = InputImage.GetPixel(x, y);                // Set pixel color in array at (x,y)
                }
            }

            //==========================================================================================
            // TODO: include here your own code
            // example: create a negative image

            switch (filter)
            {
                case ("Negative"):
                    Negative();
                    break;
                case ("Grayscale"):
                    Grayscale();
                    break;
                case ("Contrast adjustment"):
                    ContrastAdjustment();
                    break;
                case ("Linear filter"):
                    LinearFilter();
                    break;
                case ("Nonlinear filter"):
                    NonlinearFilter();
                    break;
                case ("Gaussian filter"):
                    GaussianFilter();
                    break;
                case ("Edge detection"):
                    EdgeDetection();
                    break;
                case ("Thresholding"):
                    Thresholding();
                    break;
                case ("Erosion"):
                    ErosionOrDialation(true);
                    break;
                case ("Dilation"):
                    ErosionOrDialation(false);
                    break;
                case ("Opening"):
                    doubleProgress = true;
                    ErosionOrDialation(true);
                    ErosionOrDialation(false);
                    doubleProgress = false;
                    break;
                case ("Closing"):
                    doubleProgress = true;
                    ErosionOrDialation(false);
                    ErosionOrDialation(true);
                    doubleProgress = false;
                    break;
                case ("Value counting"):
                    ValueCounting();
                    break;
                case ("Boundary trace"):
                    BoundaryTrace();
                    break;
                case ("Nothing"):
                default:
                    break;
            }
            //==========================================================================================

            // Copy array to output Bitmap
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    OutputImage.SetPixel(x, y, Image[x, y]);               // Set the pixel color at coordinate (x,y)
                }
            }

            ap.Image = (Image)OutputImage;                         // Display output image
            progressBar.Visible = false;                                    // Hide progress bar
        }

        private void Negative()
        {
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Color pixelColor = Image[x, y];                         // Get the pixel color at coordinate (x,y)
                    Color updatedColor = Color.FromArgb(255 - pixelColor.R, 255 - pixelColor.G, 255 - pixelColor.B); // Negative image
                    Image[x, y] = updatedColor;                             // Set the new pixel color at coordinate (x,y)
                    progressBar.PerformStep();                              // Increment progress bar
                }
            }
        }

        private void Grayscale()
        {
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Color pixelColor = Image[x, y];                         // Get the pixel color at coordinate (x,y)
                    int Clinear = (int)(0.2126f * pixelColor.R + 0.7152 * pixelColor.G + 0.0722 * pixelColor.B); // Calculate grayscale
                    Color updatedColor = Color.FromArgb(Clinear, Clinear, Clinear); // Grayscale image
                    Image[x, y] = updatedColor;                             // Set the new pixel color at coordinate (x,y)
                    progressBar.PerformStep();                              // Increment progress bar
                }
            }
        }

        private void ContrastAdjustment()
        {
            byte minimumValue = 255, maximumValue = 0;

            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    byte value = Image[x, y].R;                             // Get the pixel color at coordinate (x,y)
                    if (value > maximumValue)                               // Get the lowest and highest grayscale values of the picture
                        maximumValue = value;
                    if (value < minimumValue)
                        minimumValue = value;
                }
            }

            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {               
                    float value = Image[x, y].R;                            // Get the pixel grayscale color at coordinate (x,y)
                    value -= minimumValue;                                  // Calculate the pixel's "grayness" as a percent between minimum- and maximumValue
                    value /= (maximumValue - minimumValue);
                    int grayColor = (int)(255 * value);
                    Image[x, y] = Color.FromArgb(grayColor, grayColor, grayColor);   // Set pixel's color to be this same percent of grayness, but then between 0 and 255
                    progressBar.PerformStep();                              // Increment progress bar
                }
            }
        }

        private void LinearFilter()
        {
            int kernalsize;

            try
            {
                kernalsize = int.Parse(textBox1.Text);
            }
            catch
            {
                return;
            }

            Color[,] OriginalImage = new Color[InputImage.Size.Width, InputImage.Size.Height];   // Duplicate the original image
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    OriginalImage[x, y] = Image[x, y];
                }
            }

            int totalsize = (2 * kernalsize + 1) * (2 * kernalsize + 1);

            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    int totalvalue = 0;
                    for (int i = 0 - kernalsize; i <= kernalsize; i++)               // Loop over all pixels in the kernal and add their value to total value
                    {
                        for (int j = 0 - kernalsize; j <= kernalsize; j++)
                        {
                            if (x + i >= 0 && y + j >= 0 && x + i < InputImage.Size.Width && y + j < InputImage.Size.Height)        // If a pixel is out of image bounds, it has value 0
                                totalvalue += OriginalImage[x + i, y + j].R;
                        }
                    }

                    totalvalue /= totalsize;
                    Image[x, y] = Color.FromArgb(totalvalue, totalvalue, totalvalue);
                    progressBar.PerformStep();
                }
            }
        }

        private void NonlinearFilter()
        {
            int medianSize;

            try
            {
                medianSize = int.Parse(textBox1.Text);
            }
            catch
            {
                return;
            }

            Color[,] OriginalImage = new Color[InputImage.Size.Width, InputImage.Size.Height];   // Duplicate the original image
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    OriginalImage[x, y] = Image[x, y];
                }
            }

            float[] pixelValues = new float[(int)Math.Pow(medianSize * 2 + 1, 2)];

            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {                   
                    float value = Image[x, y].R;                            // Get the pixel color at coordinate (x,y)
                    int counter = 0;
                    for (int i = -medianSize; i <= medianSize; i++)         // Get color values for all pixels in median range
                    {
                        for (int j = -medianSize; j <= medianSize; j++)
                        {
                            if (x + i >= 0 && x + i < InputImage.Size.Width && y + j >= 0 && y + j < InputImage.Size.Height)
                            {
                                pixelValues[counter] = (OriginalImage[x + i, y + j].R);
                                counter++;
                            }
                        }
                    }

                    Array.Sort(pixelValues);
                    int newValue = (int)pixelValues[pixelValues.Length / 2 + 1];
                    Image[x, y] = Color.FromArgb(newValue, newValue, newValue);     // Set the new pixel color at coordinate (x,y)
                    progressBar.PerformStep();                              // Increment progress bar
                }
            }
        }

        private void GaussianFilter()
        {
            double euler = Math.E;
            float sigma;
            int kernelSize;

            try
            {
                sigma = float.Parse(textBox1.Text);                         // Try to get the sigma by parsing
                kernelSize = int.Parse(textBox2.Text);                      // Try to get the kernelsize by parsing
            }
            catch
            {
                return;
            }
            if (sigma < 0 || kernelSize < 0)
                return;

            double[,] weightskernel = new double[kernelSize * 2 + 1, kernelSize * 2 + 1];
            double total = 0;

            for (int i = -kernelSize; i <= kernelSize; i++)                 // Calculate initial weight for each cell in the kernel
            {
                for (int j = -kernelSize; j <= kernelSize; j++)
                {
                    double value = Math.Pow(euler, -(i * i + j * j) / (2 * sigma * sigma));
                    weightskernel[i + kernelSize, j + kernelSize] = value;
                    total += value;
                }
            }

            Color[,] OriginalImage = new Color[InputImage.Size.Width, InputImage.Size.Height];   // Duplicate the original image
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    OriginalImage[x, y] = Image[x, y];
                }
            }

            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    double newGray = 0;
                    for (int i = -kernelSize; i <= kernelSize; i++)
                    {
                        for (int j = -kernelSize; j <= kernelSize; j++)
                        {
                            if (x + i >= 0 && x + i < InputImage.Size.Width && y + j >= 0 && y + j < InputImage.Size.Height)
                                newGray += (weightskernel[i + kernelSize, j + kernelSize] / total) * OriginalImage[x + i, y + j].R;       // Add the pixels fraction of its color to the new color of Image[x,y]
                        }
                    }
                    Image[x, y] = Color.FromArgb((int)newGray, (int)newGray, (int)newGray);     // Update pixel in image
                    progressBar.PerformStep();                              // Increment progress bar
                }
            }
        }

        private void EdgeDetection()
        {
            double normalisationFactor;
            double[,] edgeFilterX = GetEDFilter(comboBox2.Text + "x");
            double[,] edgeFilterY = GetEDFilter(comboBox2.Text + "y");

            switch (comboBox2.Text)
            {
                case ("Prewitt"):
                    normalisationFactor = 1f / 6f;
                    break;
                case ("Sobel"):
                    normalisationFactor = 1f / 8f;
                    break;
                default:
                    normalisationFactor = 0f;
                    break;
            }

            Color[,] OriginalImage = new Color[InputImage.Size.Width, InputImage.Size.Height];   // Duplicate the original image
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    OriginalImage[x, y] = Image[x, y];
                }
            }

            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    double totalX = 0, totalY = 0;
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            if (x + i >= 0 && x + i < InputImage.Size.Width && y + j >= 0 && y + j < InputImage.Size.Height)
                            {
                                totalX += OriginalImage[x + i, y + j].R * edgeFilterX[i + 1, j + 1];
                                totalY += OriginalImage[x + i, y + j].R * edgeFilterY[i + 1, j + 1];
                            }
                            // If the selected pixel is out of bounds, count that pixel value as 0, which does nothing
                        }
                    }
                    totalX *= normalisationFactor;
                    totalY *= normalisationFactor;
                    double EdgeStrength = Math.Sqrt(totalX * totalX + totalY * totalY);
                    Image[x, y] = Color.FromArgb((int)EdgeStrength, (int)EdgeStrength, (int)EdgeStrength);
                    progressBar.PerformStep();                              // Increment progress bar
                }
            }
        }

        private void Thresholding()
        {
            int threshold;
            try
            {
                threshold = int.Parse(textBox1.Text);                       // Try to get the threshold by parsing
            }
            catch
            {
                return;
            }

            threshold = Math.Max(0, Math.Min(255, threshold));              // Clamp threshold between 0 and 255              

            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Color pixelColor = Image[x, y];                         // Get the pixel color at coordinate (x,y)
                    if (pixelColor.R > threshold)                           // Set color to black if grayscale (thus either R, G or B) is above threshold, else make the color white
                        Image[x, y] = Color.White;
                    else
                        Image[x, y] = Color.Black;
                    progressBar.PerformStep();                              // Increment progress bar
                }
            }
        }

        private double[,] GetEDFilter(string filterName)
        {
            switch (filterName)
            {
                case ("Prewittx"):
                    return new double[,]
                    {
                        { -1, 0, 1 },
                        { -1, 0, 1 },
                        { -1, 0, 1 }
                    };
                case ("Sobelx"):
                    return new double[,]
                    {
                        { -1, 0, 1 },
                        { -2, 0, 2 },
                        { -1, 0, 1 }
                    };
                case ("Prewitty"):
                    return new double[,]
                    {
                        { -1, -1, -1 },
                        { 0, 0, 0 },
                        { 1, 1, 1 }
                    };
                case ("Sobely"):
                    return new double[,]
                    {
                        { -1, -2, -1 },
                        { 0, 0, 0 },
                        { 1, 2, 1 }
                    };
                default:
                    return new double[,]
                    {
                        { 0, 0, 0 },
                        { 0, 0, 0 },
                        { 0, 0, 0 }
                    };
            }
        }

        private void GaussianFilter(float sigma, int kernelSize)
        {
            double euler = Math.E;
            if (sigma < 0 || kernelSize < 0)
                return;

            double[,] weightskernel = new double[kernelSize * 2 + 1, kernelSize * 2 + 1];
            double total = 0;

            for (int i = -kernelSize; i <= kernelSize; i++)                 // Calculate initial weight for each cell in the kernel
            {
                for (int j = -kernelSize; j <= kernelSize; j++)
                {
                    double value = Math.Pow(euler, -(i * i + j * j) / (2 * sigma * sigma));
                    weightskernel[i + kernelSize, j + kernelSize] = value;
                    total += value;
                }
            }

            Color[,] OriginalImage = new Color[InputImage.Size.Width, InputImage.Size.Height];   // Duplicate the original image
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    OriginalImage[x, y] = Image[x, y];
                }
            }

            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    double newGray = 0;
                    for (int i = -kernelSize; i <= kernelSize; i++)
                    {
                        for (int j = -kernelSize; j <= kernelSize; j++)
                        {
                            if (x + i >= 0 && x + i < InputImage.Size.Width && y + j >= 0 && y + j < InputImage.Size.Height)
                                newGray += (weightskernel[i + kernelSize, j + kernelSize] / total) * OriginalImage[x + i, y + j].R;       // Add the pixels fraction of its color to the new color of Image[x,y]
                        }
                    }
                    Image[x, y] = Color.FromArgb((int)newGray, (int)newGray, (int)newGray);     // Update pixel in image
                    progressBar.PerformStep();                              // Increment progress bar
                }
            }
        }

        private void ErosionOrDialation(bool IsErosion)
        {
            int size;
            int baseMinColor;
            int rounds;
            try
            {
                size = int.Parse(modeSize) - 1;
                rounds = int.Parse(textBox1.Text);
            }
            catch
            {
                return;
            }

            if (IsErosion)
                baseMinColor = 0;
            else
                baseMinColor = 255;

            for (int Nr = 0; Nr < rounds; Nr++)
            {
                Color[,] OriginalImage = new Color[InputImage.Size.Width, InputImage.Size.Height];   // Duplicate the original image
                for (int x = 0; x < InputImage.Size.Width; x++)
                {
                    for (int y = 0; y < InputImage.Size.Height; y++)
                    {
                        OriginalImage[x, y] = Image[x, y];
                    }
                }

                for (int x = 0; x < InputImage.Size.Width; x++)
                {
                    for (int y = 0; y < InputImage.Size.Height; y++)
                    {
                        int minColor = baseMinColor;
                        for (int i = -(size); i <= size; i++)
                        {
                            for (int j = -(size); j <= size; j++)
                            {
                                if (H[i + size, j + size] && x + i >= 0 && y + j >= 0 && x + i < InputImage.Size.Width && y + j < InputImage.Size.Height) // Do nothing if selected position is out of bounds
                                {
                                    if (IsErosion)
                                        minColor = Math.Max(minColor, OriginalImage[x + i, y + j].R);
                                    else
                                        minColor = Math.Min(minColor, OriginalImage[x + i, y + j].R);
                                }
                            }
                        }
                        Image[x, y] = Color.FromArgb(minColor, minColor, minColor);         // Set the new pixel color at coordinate (x,y)
                        if (doubleProgress)
                        {
                            if (y % (rounds * 2) == 0)
                                progressBar.PerformStep();                          // Increment progress bar
                        }
                        else if (y % rounds == 0)
                            progressBar.PerformStep();                              // Increment progress bar
                    }
                }
            }
        }

        private void ValueCounting()
        {
            chart1.Series.Clear();
            chart1.ResetAutoValues();
            int[] values = new int[256];
            int valuecounter = 0;
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    int value = Image[x, y].R;
                    if (values[value] == 0)
                    {
                        valuecounter++;
                    }
                    values[value]++;
                    progressBar.PerformStep();                              // Increment progress bar
                }
            }

            var values1 = chart1.Series.Add("Values");
            for (int i = 0; i < 256; i++)
            {
                values1.Points.AddY(values[i]);
            }

            label1.Text = "Aantal values: " + valuecounter;
        }

        private void BoundaryTrace()
        {
            // For the BoundaryTrace we chose an 8-neighbourhood to determine if a pixel is a boundary
            // This is because we believe that pixels aren't really part of an edge if they aren't directly next to a white pixel
            edges = new bool[InputImage.Size.Width, InputImage.Size.Height]; // Initialize boolian array to keep track of boundary pixels
            Color[,] OriginalImage = new Color[InputImage.Size.Width, InputImage.Size.Height];   // Duplicate the original image

            for (int x = 0; x < InputImage.Size.Width; x++)
                for (int y = 0; y < InputImage.Size.Height; y++)
                    OriginalImage[x, y] = Image[x, y];

            for (int x = 0; x < InputImage.Size.Width; x++)                 // Fill in the array of edge pixels
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    if (OriginalImage[x, y].R == 0)
                    {
                        for (int i = -1; i <= 1; i++)                       // Check the entire 8-neighbourhood for white pixels                        
                            for (int j = -1; j <= 1; j++)
                                if (x + i > 0 && y + j > 0 && x + i < InputImage.Size.Width && y + j < InputImage.Size.Height && OriginalImage[x + i, y + j].R == 255)
                                    edges[x, y] = true;
                    }
                    progressBar.PerformStep();                              // Increment progress bar
                }
        }

        private void SetH()
        {
            bool[,] newH = new bool[3, 3];
            int size;
            try
            {
                size = int.Parse(modeSize);                     // Try to get the inputted size - if it's a number
                newH = new bool[size * 2 - 1, size * 2 - 1];
            }
            catch
            {
                return;
            }

            for (int i = 0; i < size * 2 - 1; i++)
            {
                for (int j = 0; j < size * 2 - 1; j++)
                {
                    if (mode == "Plus")
                    {
                        if (i == newH.GetLength(0) / 2 || j == newH.GetLength(1) / 2)
                            newH[i, j] = true;
                    }
                    else if (mode == "Rectangle")
                    {
                        newH[i, j] = true;
                    }
                }
            }
            H = newH;
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (OutputImage == null) return;                                // Get out if no output image
            if (saveImageDialog.ShowDialog() == DialogResult.OK)
                OutputImage.Save(saveImageDialog.FileName);                 // Save the output image
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (OutputImage == null) return;                                // Get out if no output image
            pictureBox1.Image = ap.Image;
            InputImage = new Bitmap(ap.Image);
        }
    }
}