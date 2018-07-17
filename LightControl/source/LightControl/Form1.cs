using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.CognitiveServices.Speech;
using Newtonsoft.Json;
using System.Speech;
using System.Speech.Synthesis;

namespace LightControl
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            button1.Text = "开始";
            pictureBox1.Load("BedOff.png");
            pictureBox2.Load("KitOff.png");
        }

        // 语音识别器
        SpeechRecognizer recognizer;

        bool isRecording = false;
        SpeechSynthesizer synth = new SpeechSynthesizer();
     

        private void Form1_Load(object sender, EventArgs e)
        {
            synth.Volume = 100; 
            synth.Speak("欢迎使用智能家居服务");

            try
            {
                // 第一步
                // 初始化语音服务SDK并启动识别器，进行语音转文本
                // 密钥和区域可在 https://azure.microsoft.com/zh-cn/try/cognitive-services/my-apis/?api=speech-services 中找到
                // 密钥示例: 5ee7ba6869f44321a40751967accf7a9
                // 区域示例: westus
                SpeechFactory speechFactory = SpeechFactory.FromSubscription("a566f8b985f842159e97a1f790906c0b", "westus");

                // 识别中文
                recognizer = speechFactory.CreateSpeechRecognizer("zh-CN");

                // 识别过程中的中间结果
                recognizer.IntermediateResultReceived += Recognizer_IntermediateResultReceived;
                // 识别的最终结果
                recognizer.FinalResultReceived += Recognizer_FinalResultReceived;
                // 出错时的处理
                recognizer.RecognitionErrorRaised += Recognizer_RecognitionErrorRaised;
            }
            catch (Exception ex)
            {
                if (ex is System.TypeInitializationException)
                {
                    Log("语音SDK不支持Any CPU, 请更改为x64");
                }
                else
                {
                    Log("初始化出错，请确认麦克风工作正常");
                    Log("已降级到文本语言理解模式");

                    TextBox inputBox = new TextBox();
                    inputBox.Text = "";
                    inputBox.Size = new Size(300, 26);
                    inputBox.Location = new Point(10, 10);
                    inputBox.KeyDown += inputBox_KeyDown;
                    Controls.Add(inputBox);

                    button1.Visible = false;
                }
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;

            isRecording = !isRecording;
            if (isRecording)
            {
                // 启动识别器
                await recognizer.StartContinuousRecognitionAsync();
                button1.Text = "停止";
            }
            else
            {
                // 停止识别器
                await recognizer.StopContinuousRecognitionAsync();
                button1.Text = "开始";
            }

            button1.Enabled = true;
        }

        // 识别过程中的中间结果
        private void Recognizer_IntermediateResultReceived(object sender, SpeechRecognitionResultEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Result.Text))
            {
                Log("中间结果: " + e.Result.Text);
            }
        }

        // 识别的最终结果
        private void Recognizer_FinalResultReceived(object sender, SpeechRecognitionResultEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Result.Text))
            {
                Log("最终结果: " + e.Result.Text);
                ProcessSttResult(e.Result.Text);
            }
        }

        // 出错时的处理
        private void Recognizer_RecognitionErrorRaised(object sender, RecognitionErrorEventArgs e)
        {
            Log("错误: " + e.FailureReason);
        }

        private async void ProcessSttResult(string text)
        {
            // 第二步
            // 调用语言理解服务取得用户意图
            string intent = await GetLuisResult(text);

            // 第三步
            // 按照意图控制灯
           
            if (!string.IsNullOrEmpty(intent))
            {
                if (intent.Equals("BedOn", StringComparison.OrdinalIgnoreCase))
                {
                    synth.Speak("收到指令，正在打开卧室的灯");
                    BedOpenLight(); 
                }
                else if (intent.Equals("BedOff", StringComparison.OrdinalIgnoreCase))
                {
                    synth.Speak("收到指令，正在关闭卧室的灯");
                    BedCloseLight();
                }
                else if (intent.Equals("KitOn", StringComparison.OrdinalIgnoreCase))
                {
                    synth.Speak("收到指令，正在打开厨房的灯");
                    KitOpenLight();
                }
                else if (intent.Equals("KitOff", StringComparison.OrdinalIgnoreCase))
                {
                    synth.Speak("收到指令，正在关闭厨房的灯");
                    KitCloseLight();
                }
                else if (intent.Equals("BKOn", StringComparison.OrdinalIgnoreCase))
                {
                    synth.Speak("收到指令，正在打开所有的灯");
                    BKOnLight();
                }
                else if (intent.Equals("BKOff", StringComparison.OrdinalIgnoreCase))
                {
                    synth.Speak("收到指令，正在关闭所有的灯");
                    BKOffLight();
                }
                else if (intent.Equals("BKnf", StringComparison.OrdinalIgnoreCase))
                {
                    synth.Speak("收到指令，正在打开卧室的灯并关闭厨房的灯");
                    BKnfLight();
                }
                else if (intent.Equals("BKfn", StringComparison.OrdinalIgnoreCase))
                {
                    synth.Speak("收到指令，正在关闭卧室的灯并打开厨房的灯");
                    BKfnLight();
                }
                else
                {
                    synth.Speak("很抱歉，我没有理解您的指令");
                }
            }
           
        }

        // 第二步
        // 调用语言理解服务取得用户意图
        
        private async Task<string> GetLuisResult(string text)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                // LUIS 终结点地址, 示例: https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/102f6255-0c32-4f36-9c79-fe12fea4d6c4?subscription-key=9004421650254a74876cf3c888b1d11f&verbose=true&timezoneOffset=0&q=
                // 可在 https://www.luis.ai 中进入app右上角publish中找到
                string luisEndpoint = "		https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/06f8fe7e-4921-4ae4-aa8c-bc188dc378aa?subscription-key=7217f78732b04ddcad9dd3d04236b481&verbose=true&timezoneOffset=0&q=";
                string luisJson = await httpClient.GetStringAsync(luisEndpoint + text);

                try
                {
                    dynamic result = JsonConvert.DeserializeObject<dynamic>(luisJson);
                    string intent = (string)result.topScoringIntent.intent;
                    double score = (double)result.topScoringIntent.score;
                    Log("意图: " + intent + "\r\n得分: " + score + "\r\n");

                    return intent;
                }
                catch (Exception ex)
                {
                    Log(ex.Message);
                    return null;
                }
            }
        }
       
        
        #region 界面操作

        private void Log(string message)
        {
            MakesureRunInUI(() =>
            {
                textBox1.AppendText(message + "\r\n");
            });
        }

        private void BedOpenLight()
        {
            MakesureRunInUI(() =>
            {
                pictureBox1.Load("BedOn.png");
            });
        }

        private void BedCloseLight()
        {
            MakesureRunInUI(() =>
            {
                pictureBox1.Load("BedOff.png");
            });
        }

        private void KitOpenLight()
        {
            MakesureRunInUI(() =>
            {
                pictureBox2.Load("KitOn.png");
            });
        }

        private void KitCloseLight()
        {
            MakesureRunInUI(() =>
            {
                pictureBox2.Load("KitOff.png");
            });
        }

        private void BKOnLight()
        {
            MakesureRunInUI(() =>
            {
                pictureBox1.Load("BedOn.png");
                pictureBox2.Load("KitOn.png");
            });
        }

        private void BKOffLight()
        {
            MakesureRunInUI(() =>
            {
                pictureBox1.Load("BedOff.png");
                pictureBox2.Load("KitOff.png");
            });
        }

        private void BKnfLight()
        {
            MakesureRunInUI(() =>
            {
                pictureBox1.Load("BedOn.png");
                pictureBox2.Load("KitOff.png");
            });
        }

        private void BKfnLight()
        {
            MakesureRunInUI(() =>
            {
                pictureBox1.Load("BedOff.png");
                pictureBox2.Load("KitOn.png");
            });
        }

        private void MakesureRunInUI(Action action)
        {
            if (InvokeRequired)
            {
                MethodInvoker method = new MethodInvoker(action);
                Invoke(action, null);
            }
            else
            {
                action();
            }
        }

        #endregion

        private void inputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && sender is TextBox)
            {
                TextBox textBox = sender as TextBox;
                e.Handled = true;
                Log(textBox.Text);
                ProcessSttResult(textBox.Text);
                textBox.Text = string.Empty;
            }
        }
    }
}
