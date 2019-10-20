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
        bool doubleProgress = false, pipelineing = false;
        string mode;
        bool[,] H;
        int rounds;

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
                int size;
                try
                {
                    size = int.Parse(textBox2.Text);
                }
                catch
                {
                    return;
                }
                StructuringElement(comboBox3.Text, size);
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
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Image[x, y] = InputImage.GetPixel(x, y);                // Set pixel color in array at (x,y)
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
                    int kernelsize;
                    try
                    {
                        kernelsize = int.Parse(textBox1.Text);
                    }
                    catch
                    {
                        return;
                    }
                    LinearFilter(kernelsize);
                    break;
                case ("Nonlinear filter"):
                    int medianSize;
                    try
                    {
                        medianSize = int.Parse(textBox1.Text);
                    }
                    catch
                    {
                        return;
                    }
                    NonlinearFilter(medianSize);
                    break;
                case ("Gaussian filter"):
                    float sigma;
                    int kernelSize;
                    try
                    {
                        kernelSize = int.Parse(textBox2.Text);                      // Try to get the kernelsize by parsing
                        sigma = float.Parse(textBox1.Text);                         // Try to get the sigma by parsing
                    }
                    catch
                    {
                        return;
                    }
                    GaussianFilter(kernelSize, sigma);
                    break;
                case ("Edge detection"):
                    EdgeDetection(comboBox2.Text);
                    break;
                case ("Thresholding"):
                    int threshold;
                    try
                    {
                        threshold = int.Parse(textBox1.Text);                       // Try to get the threshold by parsing
                    }
                    catch
                    {
                        return;
                    }
                    Thresholding(threshold);
                    break;
                case ("Erosion"):
                    try
                    {
                        rounds = int.Parse(textBox1.Text);
                    }
                    catch
                    {
                        return;
                    }
                    ErosionOrDialation(true, rounds);
                    break;
                case ("Dilation"):
                    try
                    {
                        rounds = int.Parse(textBox1.Text);
                    }
                    catch
                    {
                        return;
                    }
                    ErosionOrDialation(false, rounds);
                    break;
                case ("Opening"):
                    try
                    {
                        rounds = int.Parse(textBox1.Text);
                    }
                    catch
                    {
                        return;
                    }
                    Opening(rounds);
                    break;
                case ("Closing"):       // Deze kan misschien wel helpen om details uit de scene te halen en edge detection van het object als geheel makkelijker te maken
                    try
                    {
                        rounds = int.Parse(textBox1.Text);
                    }
                    catch
                    {
                        return;
                    }
                    Closing(rounds);
                    break;
                case ("Value counting"):
                    ValueCounting();
                    break;
                case ("Tag zones"):
                    TagZones();
                    break;
                case ("Pipeline v0"):
                    PipelineV0();
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
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Color pixelColor = Image[x, y];                         // Get the pixel color at coordinate (x,y)
                    Color updatedColor = Color.FromArgb(255 - pixelColor.R, 255 - pixelColor.G, 255 - pixelColor.B); // Negative image
                    Image[x, y] = updatedColor;                             // Set the new pixel color at coordinate (x,y)
                    if (!pipelineing)
                        progressBar.PerformStep();            // Increment progress bar
                }
        }

        private void Grayscale()
        {
            for (int x = 0; x < InputImage.Size.Width; x++)
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Color pixelColor = Image[x, y];                         // Get the pixel color at coordinate (x,y)
                    int Clinear = (int)(0.2126f * pixelColor.R + 0.7152 * pixelColor.G + 0.0722 * pixelColor.B); // Calculate grayscale
                    Color updatedColor = Color.FromArgb(Clinear, Clinear, Clinear); // Grayscale image
                    Image[x, y] = updatedColor;                             // Set the new pixel color at coordinate (x,y)
                    if (!pipelineing)
                        progressBar.PerformStep();            // Increment progress bar
                }         
        }

        private void ContrastAdjustment()
        {
            byte minimumValue = 255, maximumValue = 0;

            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Color color = Image[x, y];
                    if (color.R != color.G || color.R != color.B)
                        throw new ConstraintException("Input image moet grayscale zijn");
                    byte value = color.R;                               // Get the pixel color at coordinate (x,y)
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
                    if (!pipelineing)
                        progressBar.PerformStep();                     // Increment progress bar
                }
            }
        }

        private void LinearFilter(int kernelsize)
        {
            Color[,] OriginalImage = new Color[InputImage.Size.Width, InputImage.Size.Height];   // Duplicate the original image
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    OriginalImage[x, y] = Image[x, y];
                }
            }

            int totalsize = (2 * kernelsize + 1) * (2 * kernelsize + 1);

            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    int totalvalue = 0;
                    for (int i = 0 - kernelsize; i <= kernelsize; i++)               // Loop over all pixels in the kernel and add their value to total value
                    {
                        for (int j = 0 - kernelsize; j <= kernelsize; j++)
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

        private void NonlinearFilter(int medianSize)
        {
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
                    if (!pipelineing)
                        progressBar.PerformStep();                    // Increment progress bar
                }
            }
        }

        private void GaussianFilter(int kernelsize, float sigma)
        {
            double euler = Math.E;

            if (sigma < 0 || kernelsize < 0)
                return;

            double[,] weightskernel = new double[kernelsize * 2 + 1, kernelsize * 2 + 1];
            double total = 0;

            for (int i = -kernelsize; i <= kernelsize; i++)                 // Calculate initial weight for each cell in the kernel
            {
                for (int j = -kernelsize; j <= kernelsize; j++)
                {
                    double value = Math.Pow(euler, -(i * i + j * j) / (2 * sigma * sigma));
                    weightskernel[i + kernelsize, j + kernelsize] = value;
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
                    for (int i = -kernelsize; i <= kernelsize; i++)
                    {
                        for (int j = -kernelsize; j <= kernelsize; j++)
                        {
                            if (x + i >= 0 && x + i < InputImage.Size.Width && y + j >= 0 && y + j < InputImage.Size.Height)
                                newGray += (weightskernel[i + kernelsize, j + kernelsize] / total) * OriginalImage[x + i, y + j].R;       // Add the pixels fraction of its color to the new color of Image[x,y]
                        }
                    }
                    Image[x, y] = Color.FromArgb((int)newGray, (int)newGray, (int)newGray);     // Update pixel in image
                    if (!pipelineing)
                        progressBar.PerformStep();                                // Increment progress bar
                }
            }
        }

        private void EdgeDetection(string filter)
        {
            double normalisationFactor;
            double[,] edgeFilterX = GetEDFilter(filter + "x");
            double[,] edgeFilterY = GetEDFilter(filter + "y");

            switch (filter)
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
                            else
                            {
                                totalX += 255 * edgeFilterX[i + 1, j + 1];
                                totalY += 255 * edgeFilterY[i + 1, j + 1];
                            }
                            // If the selected pixel is out of bounds, count that pixel value as 255, otherwise white lines would always be created at the edges
                        }
                    }
                    totalX *= normalisationFactor;
                    totalY *= normalisationFactor;
                    double EdgeStrength = Math.Sqrt(totalX * totalX + totalY * totalY);
                    Image[x, y] = Color.FromArgb((int)EdgeStrength, (int)EdgeStrength, (int)EdgeStrength);
                    if (!pipelineing)
                        progressBar.PerformStep();            // Increment progress bar
                }
            }
        }

        private void Thresholding(int threshold)
        {
            threshold = Math.Max(0, Math.Min(255, threshold));              // Clamp threshold between 0 and 255         
            for (int x = 0; x < InputImage.Size.Width; x++)
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Color pixelColor = Image[x, y];                         // Get the pixel color at coordinate (x,y)
                    if (pixelColor.R != pixelColor.G || pixelColor.R != pixelColor.B)
                        throw new ConstraintException("Input image moet grayscale zijn");
                    if (pixelColor.R > threshold)                           // Set color to black if grayscale (thus either R, G or B) is above threshold, else make the color white
                        Image[x, y] = Color.White;
                    else
                        Image[x, y] = Color.Black;
                    if (!pipelineing)
                        progressBar.PerformStep();            // Increment progress bar
                }          
        }

        private void StructuringElement(string Mode, int size)
        {
            mode = Mode;
            SetH(size);

            if (size <= 0)
                throw new ArgumentOutOfRangeException("size has to be greater than 0");

            string message = "Structuring element set as: \n";
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

            if (!pipelineing)
                MessageBox.Show(message, "Structuring element visual", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void ErosionOrDialation(bool IsErosion, int rounds)
        {
            int size = H.GetLength(0) / 2;
            if (size <= 0)
                throw new ArgumentOutOfRangeException("size has to be greater than 0");

            int baseMinColor;

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
                            if (y % (rounds * 2) == 0 && !pipelineing)
                                progressBar.PerformStep();                          // Increment progress bar
                        }
                        else if (y % rounds == 0 && !pipelineing)
                            progressBar.PerformStep();                              // Increment progress bar
                    }
                }
            }
        }

        private void Closing(int rounds)
        {
            doubleProgress = true;
            ErosionOrDialation(false, rounds);
            ErosionOrDialation(true, rounds);
            doubleProgress = false;
        }

        private void Opening(int rounds)
        {
            doubleProgress = true;
            ErosionOrDialation(true, rounds);
            ErosionOrDialation(false, rounds);
            doubleProgress = false;
        }

        private void ValueCounting()
        {
            chart1.Series.Clear();
            chart1.ResetAutoValues();
            int[] values = new int[256];
            int valuecounter = 0;
            for (int x = 0; x < InputImage.Size.Width; x++)
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    int value = Image[x, y].R;
                    if (values[value] == 0)
                    {
                        valuecounter++;
                    }
                    values[value]++;
                    if (!pipelineing)
                        progressBar.PerformStep();                    // Increment progress bar
                }            

            var values1 = chart1.Series.Add("Values");
            for (int i = 0; i < 256; i++)            
                values1.Points.AddY(values[i]);            

            label1.Text = "Aantal values: " + valuecounter;
        }

        int[,] edge;
        int tagNr;

        private void TagZones()
        {
            edge = new int[InputImage.Size.Width, InputImage.Size.Height];      // Initialize int array to keep track of boundary pixels and their respective tags
            Color[,] OriginalImage = new Color[InputImage.Size.Width, InputImage.Size.Height];
            tagNr = 2;

            for (int x = 0; x < InputImage.Size.Width; x++)                 // Duplicate the original image
                for (int y = 0; y < InputImage.Size.Height; y++)
                    OriginalImage[x, y] = Image[x, y];

            for (int x = 0; x < InputImage.Size.Width; x++)                 // Fill in the array of edge pixels
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Color asdf = OriginalImage[x, y];
                    if (OriginalImage[x, y] != Color.White && OriginalImage[x, y] != Color.Black)
                        throw new ConstraintException("De input moet een binaire edge image zijn, dat is dit dus niet");
                    if (OriginalImage[x, y].R == 255)
                        edge[x, y] = 1;                             
                }

            for (int x = 0; x < Image.GetLength(0); x++)            // Tag a group of neighbouring pixels with 0 values in the edge array,
                for (int y = 0; y < Image.GetLength(1); y++)        // then find the next 0 that's not part of the previous group
                {
                    if (edge[x, y] == 0)
                    {
                        FloodFill(x, y);
                        tagNr++;
                    }
                    if (!pipelineing)
                        progressBar.PerformStep();    // Increment progress bar
                }

            for (int x = 0; x < Image.GetLength(0); x++)            // After floodfilling, a few edge pixels are left untagged as the algorithm is
                for (int y = 0; y < Image.GetLength(1); y++)        // uncertain to which grouop it belongs, we look in the 8-neighbourhood and add it to the least recurring tag (min 1x)
                    if (edge[x, y] == 1)
                    {
                        int[] tagNeighborhood = new int[tagNr];
                        int minTagVal = 8;
                        int minTag = 1;

                        for (int i = -1; i <= 1; i++)
                            for (int j = -1; j <= 1; j++)
                                if (x + i >= 0 && x + i < Image.GetLength(0) && y + j >= 0 && y + j < Image.GetLength(1) && !(x + i == 0 && y + j == 0))
                                {
                                    tagNeighborhood[edge[x + i, y + j]]++;
                                }
                        for (int k = 0; k < tagNr; k++)
                        {
                            if (k > 1 && tagNeighborhood[k] < minTagVal && tagNeighborhood[k] > 0)
                            {
                                minTagVal = tagNeighborhood[k];
                                minTag = k;
                            }
                        }
                        edge[x, y] = minTag;
                    }

            for (int i = 0; i < edge.GetLength(0); i++)             // Visualise every tag group by coloring them in            
                for (int j = 0; j < edge.GetLength(1); j++)
                {
                    int tag = edge[i, j];
                    if (tag == 1)
                        Image[i, j] = Color.FromArgb(255, 255, 255);
                    else
                        Image[i, j] = Color.FromArgb(463 * tag % 256, 233 * tag % 256, 337 * tag % 256);    // Jeroen Hijzelendoorn's highly advanced random color generator *tm
                }            
        }

        private void FloodFill(int startx, int starty)
        {
            Stack<Point> zonePoints = new Stack<Point>();
            zonePoints.Push(new Point(startx, starty));

            while (zonePoints.Count > 0)
            {
                Point currPos = zonePoints.Pop();
                int x = currPos.X, y = currPos.Y;

                edge[x,y] = tagNr;

                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                        if (x + i >= 0 && x + i < Image.GetLength(0) && y + j >= 0 && y + j < Image.GetLength(1))
                        {
                            if (edge[x + i, y + j] == 0)
                                zonePoints.Push(new Point(x + i, y + j));
                            else if (edge[x + i, y + j] == 1)
                                edge[x + i, y + j] = tagNr;
                        }
            }
        }

        private void SetH(int size)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException("size has to be greater than 0");

            bool[,] newH = new bool[3, 3];
            try
            {
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


        // misschien een idee om naar Color Edge detection te kijken, maakt nogal verschil in performance:
        // https://nl.mathworks.com/matlabcentral/fileexchange/28114-fast-edges-of-a-color-image-actual-color-not-converting-to-grayscale
        private void PipelineV0()
        {
            // Every method increases the progress bar as if it were the only method changing it
            // Because we now use multiple methods at once, the progress bar would exceed 100%,
            // but for some reason this causes a significant slowdown in calculation time, so we shut it off temporarily
            Color[,] OriginalImage = Image, GrayImage;

            pipelineing = true;

            Grayscale();
            ContrastAdjustment();
            GrayImage = Image;
            StructuringElement("Rectangle", 2);
            Closing(1);
            EdgeDetection("Sobel");
            ContrastAdjustment();
            Thresholding(40);
            TagZones();

            pipelineing = false;
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