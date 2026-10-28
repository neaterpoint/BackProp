using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using Backprop;

namespace BackProp
{
    public partial class Form1 : Form
    {
        NeuralNet nn;
        Bitmap loaded;
        public Form1()
        {
            InitializeComponent();
            comboBox1.Items.Add("AND");
            comboBox1.Items.Add("OR");
            comboBox1.Items.Add("XOR");
            comboBox1.SelectedIndex = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            nn = new NeuralNet(2,5,1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string operation = comboBox1.SelectedItem.ToString();

            if (operation == "AND")
                //perform AND operation
                for (int i = 0; i < 1000; i++)
                {
                    //1 AND 1
                    nn.setInputs(0, 1.0);
                    nn.setInputs(1, 1.0);
                    nn.setDesiredOutput(0, 1.0);
                    nn.learn();

                    // 0 AND 1
                    nn.setInputs(0, 0);
                    nn.setInputs(1, 1.0);
                    nn.setDesiredOutput(0, 0);
                    nn.learn();

                    // 1 AND 0
                    nn.setInputs(0, 1.0);
                    nn.setInputs(1, 0);
                    nn.setDesiredOutput(0, 0);
                    nn.learn();

                    // 0 AND 0
                    nn.setInputs(0, 0);
                    nn.setInputs(1, 0);
                    nn.setDesiredOutput(0, 0);
                    nn.learn();
                }

            if (operation == "OR")
                //perform OR operation
                for (int i = 0; i < 1000; i++)
                {
                    //1 OR 1
                    nn.setInputs(0, 1.0);
                    nn.setInputs(1, 1.0);
                    nn.setDesiredOutput(0, 1.0);
                    nn.learn();

                    // 0 OR 1
                    nn.setInputs(0, 0);
                    nn.setInputs(1, 1.0);
                    nn.setDesiredOutput(0, 1.0);
                    nn.learn();

                    // 1 OR 0
                    nn.setInputs(0, 1.0);
                    nn.setInputs(1, 0);
                    nn.setDesiredOutput(0, 1.0);
                    nn.learn();

                    // 0 OR 0
                    nn.setInputs(0, 0);
                    nn.setInputs(1, 0);
                    nn.setDesiredOutput(0, 0);
                    nn.learn();
                }

            if (operation == "XOR")
                //perform XOR operation
                for (int i = 0; i < 1000; i++)
                {
                    //1 XOR 1
                    nn.setInputs(0, 1.0);
                    nn.setInputs(1, 1.0);
                    nn.setDesiredOutput(0, 0);
                    nn.learn();

                    // 0 XOR 1
                    nn.setInputs(0, 0);
                    nn.setInputs(1, 1.0);
                    nn.setDesiredOutput(0, 1.0);
                    nn.learn();

                    // 1 XOR 0
                    nn.setInputs(0, 1.0);
                    nn.setInputs(1, 0);
                    nn.setDesiredOutput(0, 1.0);
                    nn.learn();

                    // 0 XOR 0
                    nn.setInputs(0, 0);
                    nn.setInputs(1, 0);
                    nn.setDesiredOutput(0, 0);
                    nn.learn();
                }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            nn.setInputs(0, Convert.ToDouble(textBox1.Text));
            nn.setInputs(1, Convert.ToDouble(textBox2.Text));
            nn.run();
            textBox3.Text = nn.getOutputData(0).ToString();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            loaded = new Bitmap(openFileDialog1.FileName);
            pictureBox1.Image = loaded;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            nn = new NeuralNet(4096, 2000, 1); //output 1 or 0

        }

        //Buggy Slow Error-Prone
        //private void button5_Click(object sender, EventArgs e)
        //{
        //    //imagecheck
        //    if (loaded == null)
        //    {
        //        MessageBox.Show("Please load an image first!");
        //        return;
        //    }
        //    //image_size check
        //    if (loaded.Width != 64 || loaded.Height != 64)
        //    {
        //        MessageBox.Show("Image must be strictly 64x64 pixels!");
        //        return;
        //    }

        //    label2.Text = "Training..."; 
        //    for (int i = 0; i < 5000; i++)
        //    {
        //        int neuroncount = 0;
        //        for (int x = 0; x < loaded.Width; x++)
        //        {
        //            for (int y = 0; y < loaded.Height; y++)
        //            {
        //                Color pixel = loaded.GetPixel(x, y);
        //                int grey = (pixel.R + pixel.G + pixel.B) / 3;
        //                nn.setInputs(neuroncount, grey/255.0);
        //                neuroncount++;
        //            }
        //        }
        //        nn.setDesiredOutput(0, 0);
        //        nn.learn();
        //    }
        //    label2.Text = "Training Complete";
        //}

        private async void button5_Click(object sender, EventArgs e)
        {
            if (loaded == null)
            {
                MessageBox.Show("Please load an image first!");
                return;
            }

            if (loaded.Width != 64 || loaded.Height != 64)
            {
                MessageBox.Show("Image must be strictly 64x64 pixels!");
                return;
            }

            label2.Text = "Training...";
            button5.Enabled = false; // Disable button so you don't click it twice

            // 1. Cache the pixel data upfront on the UI thread (safe for Bitmap)
            double[] pixelData = new double[4096];
            int index = 0;
            for (int y = 0; y < loaded.Height; y++)
            {
                for (int x = 0; x < loaded.Width; x++)
                {
                    Color pixel = loaded.GetPixel(x, y);
                    double grey = (pixel.R + pixel.G + pixel.B) / 3.0;
                    pixelData[index++] = grey / 255.0; // Normalized [0.0, 1.0]
                }
            }

            // 2. Run the heavy training loop on a background thread using Task.Run
            await Task.Run(() =>
            {
                int epochs = 5000;
                for (int i = 0; i < epochs; i++)
                {
                    for (int j = 0; j < pixelData.Length; j++)
                    {
                        nn.setInputs(j, pixelData[j]);
                    }
                    nn.setDesiredOutput(0, 0);
                    nn.learn();
                    Console.WriteLine($"Training iteration {i + 1}/{epochs} completed.");
                }
            });

            // 3. Update UI safely once finished
            label2.Text = "Training Complete";
            button5.Enabled = true;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            int neuroncount = 0;
            for (int x = 0; x < loaded.Width; x++)
            {
                for (int y = 0; y < loaded.Height; y++)
                {
                    Color pixel = loaded.GetPixel(x, y);
                    int grey = (pixel.R + pixel.G + pixel.B) / 3;
                    nn.setInputs(neuroncount, grey);
                }
            }
            nn.run();
            textBox4.Text = nn.getOutputData(0).ToString();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            //Open form 2
            Form2 f2 = new Form2();
            f2.Show();
            //Hide this form
            this.Hide();
        }
    }
}
