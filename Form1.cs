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
        int width, height, rounds;
        bool[,] potentialEdge;
        double[] perimeterCounter;
        double[] compactness;
        double[] circularity;

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
                width = InputImage.Size.Width;
                height = InputImage.Size.Height;
            }
        }

        //This project was made by:
        //Steven van Blijderveen	5553083
        //Jeroen Hijzelendoorn		6262279
        //As an assignment to be delivered by at most sunday 27 oktober 2019

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
            OutputImage = new Bitmap(InputImage.Size.Width, height); // Create new output image
            Image = new Color[InputImage.Size.Width, height];       // Create array to speed-up operations (Bitmap functions are very slow)

            // Setup progress bar
            progressBar.Visible = true;
            progressBar.Minimum = 1;
            progressBar.Maximum = width * height;
            progressBar.Value = 1;
            progressBar.Step = 1;

            // Copy input Bitmap to array            
            for (int x = 0; x < width; x++)            
                for (int y = 0; y < height; y++)
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
                    EdgeDetection(comboBox2.Text, true);
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
                case ("NiblackThresholding"):
                    NiblackThresholding();
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
                case ("Closing"):
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
                case ("Reduce Binary Noise"):
                    ReduceBinaryNoise();
                    break;
                case ("Tag zones"):
                    TagZones();
                    break;
                case ("Pipeline v1_1"):
                    PipelineV1_1();
                    break;
                case ("Nothing"):
                default:
                    break;
            }
            //==========================================================================================

            ShowImage();
            progressBar.Visible = false;                                    // Hide progress bar
        }

        private void ShowImage()
        {
            // Copy array to output Bitmap
            for (int x = 0; x < width; x++)            
                for (int y = 0; y < height; y++)                
                    OutputImage.SetPixel(x, y, Image[x, y]);                // Set the pixel color at coordinate (x,y)
            ap.Image = (Image)OutputImage;                                  // Display output image
            ap.Refresh();
        }

        private void Negative()
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
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
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
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

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
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

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
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
            Color[,] OriginalImage = new Color[width, height];   // Duplicate the original image
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    OriginalImage[x, y] = Image[x, y];
                }
            }

            int totalsize = (2 * kernelsize + 1) * (2 * kernelsize + 1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int totalvalue = 0;
                    for (int i = 0 - kernelsize; i <= kernelsize; i++)               // Loop over all pixels in the kernel and add their value to total value
                    {
                        for (int j = 0 - kernelsize; j <= kernelsize; j++)
                        {
                            if (x + i >= 0 && y + j >= 0 && x + i < width && y + j < height)        // If a pixel is out of image bounds, it has value 0
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
            Color[,] OriginalImage = new Color[width, height];   // Duplicate the original image
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    OriginalImage[x, y] = Image[x, y];
                }
            }

            float[] pixelValues = new float[(int)Math.Pow(medianSize * 2 + 1, 2)];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float value = Image[x, y].R;                            // Get the pixel color at coordinate (x,y)
                    int counter = 0;
                    for (int i = -medianSize; i <= medianSize; i++)         // Get color values for all pixels in median range
                    {
                        for (int j = -medianSize; j <= medianSize; j++)
                        {
                            if (x + i >= 0 && x + i < width && y + j >= 0 && y + j < height)
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

            Color[,] OriginalImage = new Color[width, height];   // Duplicate the original image
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    OriginalImage[x, y] = Image[x, y];
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    double newGray = 0;
                    for (int i = -kernelsize; i <= kernelsize; i++)
                    {
                        for (int j = -kernelsize; j <= kernelsize; j++)
                        {
                            if (x + i >= 0 && x + i < width && y + j >= 0 && y + j < height)
                                newGray += (weightskernel[i + kernelsize, j + kernelsize] / total) * OriginalImage[x + i, y + j].R;       // Add the pixels fraction of its color to the new color of Image[x,y]
                        }
                    }
                    Image[x, y] = Color.FromArgb((int)newGray, (int)newGray, (int)newGray);     // Update pixel in image
                    if (!pipelineing)
                        progressBar.PerformStep();                                // Increment progress bar
                }
            }
        }

        private void EdgeDetection(string filter, bool inColor)
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

            Color[,] OriginalImage = new Color[width, height];   // Duplicate the original image
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    OriginalImage[x, y] = Image[x, y];
                }            

            for (int x = 0; x < width; x++)            
                for (int y = 0; y < height; y++)
                {
                    double totalX = 0, totalY = 0;
                    double totalRX = 0, totalRY = 0, totalGX = 0, totalGY = 0, totalBX = 0, totalBY = 0;
                    for (int i = -1; i <= 1; i++)                    
                        for (int j = -1; j <= 1; j++)
                        {
                            if (x + i >= 0 && x + i < width && y + j >= 0 && y + j < height)
                            {
                                if (!inColor)
                                {
                                    totalX += OriginalImage[x + i, y + j].R * edgeFilterX[i + 1, j + 1];
                                    totalY += OriginalImage[x + i, y + j].R * edgeFilterY[i + 1, j + 1];                                    
                                }
                                else
                                {                                                                     
                                    totalRX += OriginalImage[x + i, y + j].R * edgeFilterX[i + 1, j + 1];
                                    totalRY += OriginalImage[x + i, y + j].R * edgeFilterY[i + 1, j + 1];
                                    totalGX += OriginalImage[x + i, y + j].G * edgeFilterX[i + 1, j + 1];
                                    totalGY += OriginalImage[x + i, y + j].G * edgeFilterY[i + 1, j + 1];
                                    totalBX += OriginalImage[x + i, y + j].B * edgeFilterX[i + 1, j + 1];
                                    totalBY += OriginalImage[x + i, y + j].B * edgeFilterY[i + 1, j + 1];
                                }
                            }
                            else
                            {
                                if (!inColor)
                                {
                                    totalX += 255 * edgeFilterX[i + 1, j + 1];
                                    totalY += 255 * edgeFilterY[i + 1, j + 1];
                                }
                                else
                                {
                                    totalRX += 255 * edgeFilterX[i + 1, j + 1];
                                    totalRY += 255 * edgeFilterY[i + 1, j + 1];
                                    totalGX += 255 * edgeFilterX[i + 1, j + 1];
                                    totalGY += 255 * edgeFilterY[i + 1, j + 1];
                                    totalBX += 255 * edgeFilterX[i + 1, j + 1];
                                    totalBY += 255 * edgeFilterY[i + 1, j + 1];
                                }
                            }
                            // If the selected pixel is out of bounds, count that pixel value as 255, otherwise white lines would always be created at the edges
                        }
                    if (!inColor)
                    {
                        totalX *= normalisationFactor;
                        totalY *= normalisationFactor;
                        double EdgeStrength = Math.Sqrt(totalX * totalX + totalY * totalY);
                        Image[x, y] = Color.FromArgb((int)EdgeStrength, (int)EdgeStrength, (int)EdgeStrength);
                        if (!pipelineing)
                            progressBar.PerformStep();            // Increment progress bar
                    }
                    else
                    {
                        totalRX *= normalisationFactor;
                        totalRY *= normalisationFactor;
                        totalGX *= normalisationFactor;
                        totalGY *= normalisationFactor;
                        totalBX *= normalisationFactor;
                        totalBY *= normalisationFactor;
                        double EdgeStrength = Math.Sqrt(totalRX * totalRX + totalRY * totalRY + totalGX * totalGX + totalGY * totalGY + totalBX * totalBX + totalBY * totalBY);
                        Image[x, y] = Color.FromArgb((int)EdgeStrength, (int)EdgeStrength, (int)EdgeStrength);

                    }
                }            
        }
        
        int[,] edge;

        private void Thresholding(int threshold)
        {
            threshold = Math.Max(0, Math.Min(255, threshold));                  // Clamp threshold between 0 and 255         
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
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
            RegisterEdges();
        }

        private void NiblackThresholding()
        {
            // Some Niblack Thresholding variables, default (according to the internet): k = 0.2; filterradius = 15 (VERY SLOW); d = 0.
            double k = 0.2;
            int filterradius = Math.Max(2, Math.Min((Image.GetLength(0) + Image.GetLength(1)) / 64, 10)) + 5;       // Depending on the image size, take a filterradius between 2 and 10
            int d = 15;

            Color[,] OriginalImage = new Color[width, height];   // Duplicate the original image
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    OriginalImage[x, y] = Image[x, y];
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    double mean = 0;
                    double variance = 0;
                    int counter = 0;
                    int[] histogram = new int[256];

                    for (int i = 0 - filterradius; i <= filterradius; i++)
                    {
                        for (int j = 0 - filterradius; j <= filterradius; j++)
                        {
                            if (x + i >= 0 && x + i < width && y + j >= 0 && y + j < height)
                            {
                                int value = OriginalImage[x + i, y + j].R;
                                mean += value;
                                histogram[value]++;
                                counter++;
                            }
                        }
                    }

                    mean = mean / counter;

                    for (int i = 0; i < 256; i++)
                    {
                        if (histogram[i] != 0)
                        {
                            variance += ((i - mean) * (i - mean)) * histogram[i];
                        }
                    }

                    variance = variance / counter;
                    double standarddeviation = Math.Sqrt(variance);

                    int threshold = (int)(mean + k * standarddeviation + d);
                    if (Image[x, y].R > threshold)                    
                        Image[x, y] = Color.White;                    
                    else                    
                        Image[x, y] = Color.Black;                    
                }
            }
            RegisterEdges();
        }

        private void ReduceBinaryNoise()
        {
            bool[,] covered = new bool[Image.GetLength(0), Image.GetLength(1)];
            for (int x = 0; x < Image.GetLength(0); x++)            // Tag a group of neighbouring pixels with 0 values in the edge array,
                for (int y = 0; y < Image.GetLength(1); y++)        // then find the next 0 that's not part of the previous group
                {
                    if (edge[x, y] == 1 && !covered[x, y])
                    {
                        Stack<Point> zonePointsStack = new Stack<Point>();
                        List<Point> zonePoints = new List<Point>();
                        zonePointsStack.Push(new Point(x, y));
                        zonePoints.Add(new Point(x, y));
                        covered[x, y] = true;

                        while (zonePointsStack.Count > 0)
                        {
                            Point currPos = zonePointsStack.Pop();
                            int X = currPos.X, Y = currPos.Y;

                            for (int i = -1; i <= 1; i++)
                                for (int j = -1; j <= 1; j++)
                                    if (X + i >= 0 && X + i < Image.GetLength(0) && Y + j >= 0 && Y + j < Image.GetLength(1))
                                    {
                                        if (edge[X + i, Y + j] == 1 && !covered[X + i, Y + j])
                                        {
                                            zonePointsStack.Push(new Point(X + i, Y + j));
                                            zonePoints.Add(new Point(X + i, Y + j));
                                            covered[X, Y] = true;
                                        }
                                    }
                        }

                        // If an edge segment covers less than 0.1% of the pixels, its probably not a mug, so we remove it from the image
                        if (zonePoints.Count < (float)(Image.GetLength(0) * Image.GetLength(1)) / 100 * 0.1f)
                            foreach(Point p in zonePoints)
                            {
                                edge[p.X, p.Y] = 0;
                                Image[p.X, p.Y] = Color.Black;
                            }
                    }
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
                Color[,] OriginalImage = new Color[width, height];   // Duplicate the original image
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        OriginalImage[x, y] = Image[x, y];
                    }
                }

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int minColor = baseMinColor;
                        for (int i = -(size); i <= size; i++)
                        {
                            for (int j = -(size); j <= size; j++)
                            {
                                if (H[i + size, j + size] && x + i >= 0 && y + j >= 0 && x + i < width && y + j < height) // Do nothing if selected position is out of bounds
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
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
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

        int tagNr;
        int[] zoneSizes;

        private void TagZones()
        {
            Color[,] OriginalImage = new Color[width, height];
            tagNr = 1;

            for (int x = 0; x < width; x++)                 // Duplicate the original image
                for (int y = 0; y < height; y++)
                {
                    OriginalImage[x, y] = Image[x, y];
                    if (OriginalImage[x, y] != Color.FromArgb(255, 255, 255) && OriginalImage[x, y] != Color.FromArgb(0, 0, 0) && OriginalImage[x, y] != Color.Black && OriginalImage[x, y] != Color.White)
                        throw new ConstraintException("De input moet een binaire edge image zijn, dat is dit dus niet");
                }

            for (int x = 0; x < Image.GetLength(0); x++)            // Tag a group of neighbouring pixels with 0 values in the edge array,
                for (int y = 0; y < Image.GetLength(1); y++)        // then find the next 0 that's not part of the previous group
                {
                    if (edge[x, y] == 0)
                    {
                        tagNr++;
                        FloodFill(x, y);
                    }
                    if (!pipelineing)
                        progressBar.PerformStep();                  // Increment progress bar
                }

            zoneSizes = CountZoneSizes();
            int[,] newEdge = new int[Image.GetLength(0), Image.GetLength(1)];
            bool pixelsDistributed = false;

            while (!pixelsDistributed)
            {
                pixelsDistributed = true;
                for (int x = 0; x < Image.GetLength(0); x++)            // After floodfilling, a few edge pixels are left untagged as the algorithm is
                    for (int y = 0; y < Image.GetLength(1); y++)        // uncertain to which grouop it belongs, we look in the 8-neighbourhood and add it to the least recurring tag (min 1x)
                        if (edge[x, y] == 1)
                        {
                            Image[x, y] = Color.Red;
                            bool[] tagNeighborhood = new bool[tagNr + 1];
                            int minTagVal = Image.GetLength(0) * Image.GetLength(1) + 1;
                            int minTag = tagNr + 1;
                            int ceilingTag = tagNr + 1;

                            for (int i = -1; i <= 1; i++)               // Get the tag# of pixels in the 8 neighbourhood
                                for (int j = -1; j <= 1; j++)
                                    if (x + i >= 0 && x + i < Image.GetLength(0) && y + j >= 0 && y + j < Image.GetLength(1))
                                        tagNeighborhood[edge[x + i, y + j]] = true;

                            for (int k = 2; k <= tagNr; k++)            // Find the tag with the smallest size, these tend to be foreground
                                if (tagNeighborhood[k] && zoneSizes[k] < minTagVal)
                                {
                                    minTagVal = zoneSizes[k];
                                    minTag = k;
                                }
                            if (minTag < ceilingTag)                            
                                newEdge[x, y] = minTag;                            
                            else
                                pixelsDistributed = false;
                        }
                for (int i = 0; i < edge.GetLength(0); i++)
                    for (int j = 0; j < edge.GetLength(1); j++)
                        if (newEdge[i, j] > 1)
                            edge[i, j] = newEdge[i, j];
            }

            CountZoneSizes();

            for (int i = 0; i < edge.GetLength(0); i++)             // Visualise every tag group by coloring them in            
                for (int j = 0; j < edge.GetLength(1); j++)
                {
                    //if (newEdge[i, j] > 1)
                    //    edge[i, j] = newEdge[i, j];
                    int tag = edge[i, j];
                    if (tag == 1)
                        Image[i, j] = Color.FromArgb(255, 255, 255);
                    else if (tag == 0)
                        Image[i, j] = Color.FromArgb(0, 0, 0);
                    else
                        Image[i, j] = Color.FromArgb(231 * tag % 256, 301 * tag % 256, 421 * tag % 256);    // Jeroen Hijzelendoorn's highly advanced random color generator *tm
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

                /// Ugly hardcoded 4-Neighborhood-way
                int i = 0;
                int j = 0;
                for (i = -1; i <= 1; i = i + 2)
                {
                    if (x + i >= 0 && x + i < Image.GetLength(0) && y + j >= 0 && y + j < Image.GetLength(1))
                    {
                        if (edge[x + i, y + j] == 0)
                            zonePoints.Push(new Point(x + i, y + j));
                    }
                }
                i = 0;
                j = 0;
                for (j = -1; j <= 1; j = j + 2)
                {
                    if (x + i >= 0 && x + i < Image.GetLength(0) && y + j >= 0 && y + j < Image.GetLength(1))
                    {
                        if (edge[x + i, y + j] == 0)
                            zonePoints.Push(new Point(x + i, y + j));
                    }
                }
            }
        }

        private int[] CountZoneSizes()
        {
            zoneSizes = new int[tagNr + 1];
            
            for (int x = 0; x < Image.GetLength(0); x++)
            {
                for (int y = 0; y < Image.GetLength(1); y++)
                {
                    zoneSizes[edge[x, y]]++;
                }
            }  

            return zoneSizes;
        }
        
        private void BoundaryTrace(int tag)
        {
            // For the BoundaryTrace we chose an 8-neighbourhood to determine if a pixel is a boundary
            // This is because we believe that pixels aren't really part of an edge if they aren't directly next to a white pixel
            potentialEdge = new bool[width, height]; // Initialize boolian array to keep track of boundary pixels
            Color[,] OriginalImage = new Color[width, height];   // Duplicate the original image
            bool startFound = false;
            Point start = Point.Empty;

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    OriginalImage[x, y] = Image[x, y];

            for (int x = 0; x < width; x++)                 // Fill in the array of edge pixels
                for (int y = 0; y < height; y++)
                {
                    if (edge[x, y] == tag)
                    {
                        if (!startFound)
                        {
                            start = new Point(x, y);
                            startFound = true;
                        }
                        for (int i = -1; i <= 1; i++)                       // Check the entire 8-neighbourhood for pixels with another tag                        
                            for (int j = -1; j <= 1; j++)
                                if (x + i > 0 && y + j > 0 && x + i < width && y + j < height && edge[x + i, y + j] != tag && (i == 0 || j == 0))
                                    potentialEdge[x, y] = true;
                    }
                    if (!pipelineing)
                    {
                        progressBar.PerformStep();                              // Increment progress bar
                    }
                }
           
            double perimeter = CountBoundaryLength(start, potentialEdge);
            perimeterCounter[tag] = perimeter;
                       
        }

        private double CountBoundaryLength(Point start, bool[,] boundary)                  // Counts how long the outerBound is
        {
            double count = 0;
            boundary[start.X, start.Y] = false;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (start.X + i > 0 && start.Y + j > 0 && start.X + i < width && start.Y + j < height && boundary[start.X + i, start.Y + j])
                    {
                        if (i != 0 && j != 0)
                        {
                            boundary[start.X + i, start.Y + j] = false;
                            count += CountBoundaryLength(new Point(start.X + i, start.Y + j), boundary) + Math.Sqrt(2);
                        }
                    }
                }
            }
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (start.X + i > 0 && start.Y + j > 0 && start.X + i < width && start.Y + j < height && boundary[start.X + i, start.Y + j])
                    {
                        if (i == 0 || j == 0)
                        {
                            boundary[start.X + i, start.Y + j] = false;
                            count += CountBoundaryLength(new Point(start.X + i, start.Y + j), boundary) + 1;
                        }
                    }
                }
            }

            return count;
        }

        // Checks if one zone surrounds another zone
        // Usefull for checking if something is a mug:
        // The handle gap is always surrounded by the mug
        private List<int>[] CheckIfZonesSurrounded()        // We kunnen nog toevoegen dat het een % uitrekend van welke tags om elke tag heen zitten, en dat met een hoog % een tag nogsteeds een andere omsingeld (vanwege thresholding fouten enzo kunnen er gaten in het handvat van de mok zitten)
        {
            List<int>[] hasSurrounded = new List<int>[tagNr + 1];
            for (int i = 0; i <= tagNr; i++)            
                hasSurrounded[i] = new List<int>();            
            for (int tag = 2; tag <= tagNr; tag++)            
                CheckTag(tag, hasSurrounded);               // For each tag#...            
            return hasSurrounded;
        }

        private void CheckTag(int tag, List<int>[] hasSurrounded)      
        {
            int neighbourCount = 0;
            bool[] neighbourTags = new bool[tagNr + 1];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (edge[x, y] == tag)                              // For every pixel that has tag# tag...                        
                        for (int i = -1; i <= 1; i++)
                            for (int j = -1; j <= 1; j++)               // Check its 8 neighbourhood for the tag# of its surrounding pixels...
                                if (x + i >= 0 && x + i < width && y + j >= 0 && y + j < height)
                                    if (!neighbourTags[edge[x + i, y + j]])
                                    {
                                        neighbourTags[edge[x + i, y + j]] = true;
                                        neighbourCount++;
                                        if (neighbourCount > 2)         // If there are more tag# than itself and 1 other, the tagzone is not surrounded by one tag, so we are done for this tag
                                            return;
                                    }

            for (int k = 2; k <= tagNr; k++)                            // Otherwise, note which tag# is surrounding tag
            {
                if (neighbourTags[k] && k != tag)
                    hasSurrounded[k].Add(tag);
            }
        }

        private void Or(Color[,] img1, Color[,] img2)
        {
            if (new Point(img1.GetLength(0), img1.GetLength(1)) == new Point(img2.GetLength(0),img2.GetLength(1)))
                for (int x = 0; x < img1.GetLength(0); x++)                
                    for (int y = 0; y < img1.GetLength(1); y++)
                    {
                        int maxColorVal = Math.Max(img1[x, y].R, img2[x, y].R);
                        Image[x, y] = Color.FromArgb(maxColorVal, maxColorVal, maxColorVal);
                    }                
        }

        private void RegisterEdges()
        {
            edge = new int[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (Image[x, y] != Color.FromArgb(255, 255, 255) && Image[x, y] != Color.FromArgb(0, 0, 0) && Image[x, y] != Color.Black && Image[x, y] != Color.White)
                        throw new ConstraintException("De input moet een binaire edge image zijn, dat is dit dus niet");
                    if (Image[x, y].R == 255)
                        edge[x, y] = 1;
                }
        }

        private void CompactnessAndCircularity(int tag)
        {
            double perimeterSquared = Math.Pow(perimeterCounter[tag], 2);
            compactness[tag] = zoneSizes[tag] / perimeterSquared;
            circularity[tag] = 4 * Math.PI * (zoneSizes[tag] / perimeterSquared);
        }

        // If we take the boundingbox of a object and split it in two,
        // there should be a noticable difference in the density of tag pixels between the two boxes
        // if we have a mug, as the hole in the handle lowers the density significantly
        private float[,] BBDensityCompare()
        {
            float[,] zoneDensities = new float[tagNr + 1, 2];

            for (int tag = 2; tag <= tagNr; tag++)
            {
                if (zoneSizes[tag] < Image.GetLength(0) * Image.GetLength(1) / 100 * 3)
                    continue;
                int minX = 512, minY = 512, maxX = 0, maxY = 0;
                for (int x = 0; x < Image.GetLength(0); x++)
                    for (int y = 0; y < Image.GetLength(1); y++)
                        if (edge[x, y] == tag)
                        {
                            if (x < minX)
                                minX = x;
                            if (x > maxX)
                                maxX = x;
                            if (y < minY)
                                minY = y;
                            if (y > maxY)
                                maxY = y;
                        }

                float boxSize = (maxX - minX) / 2 * (maxY - minY);
                float count1 = 0, count2 = 0, count3 = 0, count4 = 0, density1, density2, density3, density4;

                for (int x = minX; x < minX + (maxX - minX) / 2; x++)       // Count left half of BB
                    for (int y = minY; y < maxY; y++)
                        if (edge[x, y] == tag)
                            count1++;
                density1 = count1 / boxSize;

                for (int x = minX + (maxX - minX) / 2; x <= maxX; x++)      // Count right half of BB
                    for (int y = minY; y < maxY; y++)
                        if (edge[x, y] == tag)
                            count2++;
                density2 = count2 / boxSize;

                for (int x = minX; x <= maxX; x++)                          // Count upper half of BB
                    for (int y = minY; y < minY + (maxY - minY) / 2; y++)
                        if (edge[x, y] == tag)
                            count3++;
                density3 = count3 / boxSize;

                for (int x = minX; x <= maxX; x++)                          // Count lower half of BB
                    for (int y = minY + (maxY - minY) / 2; y < maxY; y++)
                        if (edge[x, y] == tag)
                            count4++;
                density4 = count4 / boxSize;

                zoneDensities[tag, 0] = Math.Max(density1, density2) / Math.Min(density1, density2);
            }
            return zoneDensities;
        }

        private void GradeMug(double[] roundness, double[] compactness, float[,] zoneDensities, List<int>[] hasSurrounded)
        {
            int[] grades = new int[tagNr + 1];
            List<int> mugs = new List<int>();

            for (int tag = 2; tag <= tagNr; tag++)
            {
                if (roundness[tag] > 0.65 && roundness[tag] < 0.75)
                    grades[tag]++;
                if (compactness[tag] > 0.04 && compactness[tag] < 0.06)
                    grades[tag]++;
                if (zoneDensities[tag, 0] > 1.5 && zoneDensities[tag, 0] < 1.8)
                    grades[tag]++;
                else if (zoneDensities[tag, 0] >= 1.8)
                    grades[tag]--;
                if (zoneDensities[tag, 1] > 0.8 && zoneDensities[tag, 1] < 1.2)
                    grades[tag]++;
                else if (zoneDensities[tag, 1] >= 1.2)
                    grades[tag]--;
                if (hasSurrounded[tag].Any())
                    grades[tag] += 2;
                if (grades[tag] >= 4)
                    mugs.Add(tag);
            }

            foreach(int tag in mugs)
            {
                for (int x = 0; x < Image.GetLength(0); x++)
                    for (int y = 0; y < Image.GetLength(1); y++)
                        if (edge[x, y] == tag)
                            Image[x, y] = Color.Red;
            }
        }

        private void PipelineV1_1()
        {
            // Every method increases the progress bar as if it were the only method changing it
            // Because we now use multiple methods at once, the progress bar would exceed 100%,
            // but for some reason this causes a significant slowdown in calculation time, so we shut it off temporarily
            Color[,] OriginalImage = new Color[Image.GetLength(0), Image.GetLength(1)], grayImage = new Color[Image.GetLength(0), Image.GetLength(1)],
                tagImage = new Color[Image.GetLength(0), Image.GetLength(1)], BinaryImage = new Color[Image.GetLength(0), Image.GetLength(1)], 
                grayEdge = new Color[Image.GetLength(0), Image.GetLength(1)], colorEdge = new Color[Image.GetLength(0), Image.GetLength(1)];

            pipelineing = true;
            label2.Visible = true;
            label2.Text = "Creating edge image..."; ap.Refresh();

            CopyImage(ref OriginalImage, Image);
            Grayscale();
            ContrastAdjustment();
            CopyImage(ref grayImage, Image);
            GetEdge(false);
            CopyImage(ref grayEdge, Image);
            ShowImage();
            CopyImage(ref Image, OriginalImage);
            GetEdge(true);
            CopyImage(ref colorEdge, Image);
            ShowImage();
            Or(grayEdge, colorEdge);
            ShowImage();

            label2.Text = "Creating binary image..."; ap.Refresh();
            NiblackThresholding();
            ReduceBinaryNoise();
            RegisterEdges();
            ShowImage();
            System.Threading.Thread.Sleep(2500);

            label2.Text = "Tagging zones..."; ap.Refresh();
            CopyImage(ref BinaryImage, Image);
            TagZones();
            ShowImage();

            label2.Text = "Evaluating..."; ap.Refresh();
            perimeterCounter = new double[tagNr + 1];
            for (int i = 0; i <= tagNr; i++)
            {
                BoundaryTrace(i);
            }
            compactness = new double[tagNr + 1];
            circularity = new double[tagNr + 1];
            for (int i = 0; i <= tagNr; i++)
            {
                CompactnessAndCircularity(i);
            }
            CopyImage(ref Image, OriginalImage);

            float[,] zoneDensities = BBDensityCompare();
            List<int>[] hasSurrounded = CheckIfZonesSurrounded();
            GradeMug(circularity, compactness, zoneDensities, hasSurrounded);

            label2.Text = "";
            label2.Visible = false;
            pipelineing = false;
        }

        private void GetEdge(bool colorED)
        {
            if (!colorED)
            {
                StructuringElement("Rectangle", 2);
                Closing(1);
            }
            EdgeDetection("Sobel", colorED);
            ContrastAdjustment();
        }

        private void CopyImage(ref Color[,] input, Color[,] toCopy)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    input[x, y] = toCopy[x, y];
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