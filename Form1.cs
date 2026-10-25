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
    }
}
