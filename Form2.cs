using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Backprop;

namespace BackProp
{
    public partial class Form2 : Form
    {
        // ---------- SETTINGS (change these if you want) ----------
        int imgSize = 32;          // images are shrunk to 32x32 (1024 inputs). Smaller = faster training
        int hiddenCount = 64;      // hidden neurons
        int epochs = 1000;         // how many times the network sees all the training images
        // ---------------------------------------------------------

        NeuralNet nn;
        Bitmap[] images;
        string[] fnames;
        PictureBox[] pbimages;

        List<string> classNames = new List<string>(); // e.g. "apple", "banana", "cat", "dog"
        int[] labels;                                 // class number of each loaded image

        OpenFileDialog compareDialog = new OpenFileDialog();
        OpenFileDialog loadWeightsDialog = new OpenFileDialog();
        SaveFileDialog saveWeightsDialog = new SaveFileDialog();

        Label lblStatus = new Label(); 
        int highlightIndex = -1;   // which training picture gets the pink border
        PictureBox pbCompare = new PictureBox();

        public Form2()
        {
            InitializeComponent();

            // Hook up the menu items (they had no handlers in the designer)
            trainNeuralNetToolStripMenuItem.Click += trainNeuralNetToolStripMenuItem_Click;
            saveWeightsToolStripMenuItem.Click += saveWeightsToolStripMenuItem_Click;
            loadWeightsToolStripMenuItem.Click += loadWeightsToolStripMenuItem_Click;
            loadImageToCompareToolStripMenuItem.Click += loadImageToCompareToolStripMenuItem_Click;
            this.FormClosed += (s, e) => Application.Exit(); // Form1 is only hidden, so close the whole app

            compareDialog.Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif";
            compareDialog.FileOk += compareDialog_FileOk;
            loadWeightsDialog.Filter = "Weights (*.txt)|*.txt";
            saveWeightsDialog.Filter = "Weights (*.txt)|*.txt";

            // Status label (top)
            lblStatus.Location = new Point(10, 30);
            lblStatus.AutoSize = true;
            lblStatus.Text = "Load images named like: apple1.jpg, apple2.jpg, banana1.jpg ... (at least 4 different names)";
            this.Controls.Add(lblStatus);

            // Image to compare (bottom-left)
            pbCompare.Size = new Size(96, 96);
            pbCompare.SizeMode = PictureBoxSizeMode.StretchImage;
            pbCompare.BorderStyle = BorderStyle.FixedSingle;
            pbCompare.Location = new Point(10, this.ClientSize.Height - 110);
            pbCompare.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.Controls.Add(pbCompare);

        }

        // =========================================================
        //  LOAD TRAINING IMAGES  (class = letters at the start of the file name)
        // =========================================================
        private void loadImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            highlightIndex = -1;
            // remove pictureboxes from a previous load
            if (pbimages != null)
            {
                foreach (PictureBox p in pbimages)
                {
                    this.Controls.Remove(p);
                    p.Dispose();
                }
            }

            images = new Bitmap[openFileDialog1.FileNames.Count()];
            fnames = new string[images.Length];
            pbimages = new PictureBox[images.Length];
            labels = new int[images.Length];
            classNames.Clear();

            int thumb = 64;
            int locx = 60;
            int locy = 70;
            for (int x = 0; x < images.Length; x++)
            {
                fnames[x] = openFileDialog1.FileNames[x];
                images[x] = new Bitmap(fnames[x]);

                // work out the class of this image from its file name
                string cname = GetClassName(fnames[x]);
                if (!classNames.Contains(cname))
                    classNames.Add(cname);
                labels[x] = classNames.IndexOf(cname);

                pbimages[x] = new PictureBox();
                pbimages[x].Size = new Size(thumb, thumb);
                pbimages[x].SizeMode = PictureBoxSizeMode.StretchImage;
                pbimages[x].Location = new Point(locx, locy);
                pbimages[x].Image = images[x];
                pbimages[x].Paint += pbTraining_Paint;
                this.Controls.Add(pbimages[x]);

                locx += thumb + 5;
                if (locx + thumb + 5 > this.Width)
                {
                    locx = 60;
                    locy += thumb + 5;
                }
            }

            lblStatus.Text = images.Length + " images loaded, " + classNames.Count + " classes: "
                             + string.Join(", ", classNames);
        }

        // "apple12.jpg" -> "apple",  "cat_3.png" -> "cat"
        private string GetClassName(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            int i = 0;
            while (i < name.Length && char.IsLetter(name[i]))
                i++;
            if (i == 0) return name.ToLower();
            return name.Substring(0, i).ToLower();
        }

        // =========================================================
        //  CONVERT AN IMAGE TO NETWORK INPUTS
        // =========================================================
        private double[] ToInputs(Bitmap src)
        {
            // shrink to imgSize x imgSize
            using (Bitmap small = new Bitmap(src, new Size(imgSize, imgSize)))
            {
                double[] data = new double[imgSize * imgSize];
                int index = 0;
                for (int y = 0; y < imgSize; y++)
                {
                    for (int x = 0; x < imgSize; x++)
                    {
                        Color p = small.GetPixel(x, y);
                        double grey = (p.R + p.G + p.B) / 3.0;
                        data[index++] = grey / 255.0;   // 0.0 to 1.0
                    }
                }
                return data;
            }
        }

        // Feed one image into the network and return the output of every class neuron
        private double[] Classify(double[] input)
        {
            for (int i = 0; i < input.Length; i++)
                nn.setInputs(i, input[i]);
            nn.run();

            double[] outputs = new double[classNames.Count];
            for (int k = 0; k < outputs.Length; k++)
                outputs[k] = nn.getOutputData(k);
            return outputs;
        }

        // =========================================================
        //  TRAIN
        // =========================================================
        private async void trainNeuralNetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (images == null || images.Length == 0)
            {
                MessageBox.Show("Load the training images first (Images > Load Image/s).");
                return;
            }
            if (classNames.Count < 4)
            {
                MessageBox.Show("You need at least 4 classes. Found " + classNames.Count +
                                ". Name files like apple1.jpg, banana1.jpg, cat1.jpg, dog1.jpg.");
                return;
            }

            int classCount = classNames.Count;
            int imageCount = images.Length;

            // Prepare data on the UI thread (Bitmap is not thread safe)
            double[][] data = new double[imageCount][];
            for (int i = 0; i < imageCount; i++)
                data[i] = ToInputs(images[i]);

            int[] lbl = (int[])labels.Clone();

            // Create a new network: inputs = pixels, outputs = one neuron per class
            nn = new NeuralNet(imgSize * imgSize, hiddenCount, classCount);

            menuStrip1.Enabled = false;
            lblStatus.Text = "Training...";
            double finalError = 0;
            int correct = 0;

            await Task.Run(() =>
            {
                Random rnd = new Random();
                int[] order = Enumerable.Range(0, imageCount).ToArray();

                for (int ep = 0; ep < epochs; ep++)
                {
                    // shuffle the order of the images each epoch
                    for (int i = order.Length - 1; i > 0; i--)
                    {
                        int j = rnd.Next(i + 1);
                        int t = order[i]; order[i] = order[j]; order[j] = t;
                    }

                    double epochError = 0;
                    foreach (int idx in order)
                    {
                        for (int p = 0; p < data[idx].Length; p++)
                            nn.setInputs(p, data[idx][p]);

                        // desired output: 1 for the correct class, 0 for the rest
                        for (int k = 0; k < classCount; k++)
                            nn.setDesiredOutput(k, k == lbl[idx] ? 1.0 : 0.0);

                        nn.learn();

                        // error of this sample (outputs from before the weight update)
                        for (int k = 0; k < classCount; k++)
                        {
                            double diff = (k == lbl[idx] ? 1.0 : 0.0) - nn.getOutputData(k);
                            epochError += diff * diff;
                        }
                    }
                    finalError = epochError / imageCount;

                    if (ep % 10 == 0)
                    {
                        int shown = ep + 1;
                        double err = finalError;
                        this.BeginInvoke((Action)(() =>
                            lblStatus.Text = "Training... epoch " + shown + "/" + epochs +
                                             "  error: " + err.ToString("0.0000")));
                    }
                }

                // check how many training images are now recognised correctly
                for (int i = 0; i < imageCount; i++)
                {
                    double[] outs = Classify(data[i]);
                    if (IndexOfMax(outs) == lbl[i]) correct++;
                }
            });

            lblStatus.Text = "Training complete. Error: " + finalError.ToString("0.0000") +
                             "  |  Training accuracy: " + correct + "/" + imageCount;
            menuStrip1.Enabled = true;
        }

        private int IndexOfMax(double[] values)
        {
            int best = 0;
            for (int i = 1; i < values.Length; i++)
                if (values[i] > values[best]) best = i;
            return best;
        }

        // =========================================================
        //  SAVE / LOAD WEIGHTS
        //  weights go in the file you choose, the settings
        //  (size, hidden count, class names) go in  <file>.meta
        // =========================================================
        private void saveWeightsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (nn == null)
            {
                MessageBox.Show("Train (or load) a network first.");
                return;
            }
            if (saveWeightsDialog.ShowDialog() != DialogResult.OK) return;

            string path = saveWeightsDialog.FileName;
            nn.saveWeights(path);

            using (StreamWriter sw = new StreamWriter(path + ".meta"))
            {
                sw.WriteLine(imgSize);
                sw.WriteLine(hiddenCount);
                sw.WriteLine(classNames.Count);
                foreach (string c in classNames)
                    sw.WriteLine(c);
            }
            lblStatus.Text = "Weights saved to " + path;
        }

        private void loadWeightsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (loadWeightsDialog.ShowDialog() != DialogResult.OK) return;

            string path = loadWeightsDialog.FileName;
            if (!File.Exists(path + ".meta"))
            {
                MessageBox.Show("Missing settings file: " + path + ".meta");
                return;
            }

            string[] meta = File.ReadAllLines(path + ".meta");
            imgSize = Convert.ToInt32(meta[0]);
            hiddenCount = Convert.ToInt32(meta[1]);
            int classCount = Convert.ToInt32(meta[2]);

            classNames.Clear();
            for (int i = 0; i < classCount; i++)
                classNames.Add(meta[3 + i]);

            nn = new NeuralNet(imgSize * imgSize, hiddenCount, classCount);
            nn.loadWeights(path);
            lblStatus.Text = "Weights loaded. Classes: " + string.Join(", ", classNames);
        }

        // =========================================================
        //  COMPARE (TEST) AN IMAGE
        // =========================================================
        private void loadImageToCompareToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (nn == null)
            {
                MessageBox.Show("Train or load a network first.");
                return;
            }
            compareDialog.ShowDialog();
        }

        private void compareDialog_FileOk(object sender, CancelEventArgs e)
        {
            if (images == null || images.Length == 0)
            {
                MessageBox.Show("Load the training images first (Images > Load Image/s) so there is something to highlight.");
                return;
            }

            Bitmap test = new Bitmap(compareDialog.FileName);
            pbCompare.Image = test;

            double[] testInput = ToInputs(test);
            double[] outs = Classify(testInput);
            string predicted = classNames[IndexOfMax(outs)];

            // among the training pictures of the predicted class, find the most similar one
            int closest = -1;
            double bestDist = double.MaxValue;
            for (int i = 0; i < images.Length; i++)
            {
                if (GetClassName(fnames[i]) != predicted) continue;

                double[] other = ToInputs(images[i]);
                double dist = 0;
                for (int p = 0; p < other.Length; p++)
                {
                    double d = other[p] - testInput[p];
                    dist += d * d;
                }
                if (dist < bestDist)
                {
                    bestDist = dist;
                    closest = i;
                }
            }

            if (closest == -1)
            {
                MessageBox.Show("The network says \"" + predicted + "\" but no loaded image has that class.");
                return;
            }

            // remove the old border, show the new one
            int old = highlightIndex;
            highlightIndex = closest;
            if (old >= 0 && old < pbimages.Length) pbimages[old].Invalidate();
            pbimages[closest].Invalidate();
        }
        private void pbTraining_Paint(object sender, PaintEventArgs e)
        {
            if (highlightIndex < 0 || highlightIndex >= pbimages.Length) return;
            if (sender != pbimages[highlightIndex]) return;

            PictureBox pb = (PictureBox)sender;
            using (Pen pink = new Pen(Color.HotPink, 6))
            {
                e.Graphics.DrawRectangle(pink, 3, 3, pb.Width - 7, pb.Height - 7);
            }
        }
    }
}