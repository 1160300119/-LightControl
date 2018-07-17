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
using System.Speech.Synthesis;
using Baidu.Aip;
using WMPLib;
using System.IO;

namespace LightControl
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            button1.Text = "准备中";
            pictureBox1.Load("BedOff.png");
            pictureBox2.Load("KitOff.png");
            pictureBox3.Load("AirConditioner.png");
            textBox5.Text = "关闭";
        }

        // 语音识别器
        SpeechRecognizer recognizer;
        bool isRecording = false;
        WMPLib.WindowsMediaPlayer wplayer = new WMPLib.WindowsMediaPlayer();

        // 设置APPID/AK/SK
        static string APP_ID = "11537377";
        static string API_KEY = "T9LOIUkTYslEF5PzfEE647CH";
        static string SECRET_KEY = "Rph7jw8rGOtCv7DzXGm9oYFYKwA3mx5m ";
        int speed = 5;
        int volume = 7;
        int style = 4;
        int mp3id = 1;
        //spd 选填 语速，取值0-15，默认为5中语速
        //pit 选填 音调，取值0-15，默认为5中语调
        //vol 选填 音量，取值0-15，默认为5中音量
        //per 选填 发音人选择, 0为普通女声，1为普通男生，3为情感合成-度逍遥，4为情感合成-度丫丫，默认为普通女声


        private void Form1_Load(object sender, EventArgs e)
        {
            this.BackgroundImage = Image.FromFile("house.jpg");
            Tts("欢迎您使用智能家居服务");
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
                buttonClick();
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
                    buttonClick();
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
                button1.Text = "服务进行中";
            }
            else
            {
                // 停止识别器
                await recognizer.StopContinuousRecognitionAsync();
                button1.Text = "服务暂停";
            }

            button1.Enabled = true;
        }

        private async void buttonClick()
        {
            button1.Enabled = false;

            isRecording = !isRecording;
            if (isRecording)
            {
                // 启动识别器
                await recognizer.StartContinuousRecognitionAsync();
                button1.Text = "服务进行中";
            }
            else
            {
                // 停止识别器
                await recognizer.StopContinuousRecognitionAsync();
                button1.Text = "服务暂停";

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
                    //synth.Speak("收到指令，正在打开卧室的灯");
                    Tts("收到指令，正在打开卧室的灯");
                    BedOpenLight();
                }
                else if (intent.Equals("BedOff", StringComparison.OrdinalIgnoreCase))
                {
                    //synth.Speak("收到指令，正在关闭卧室的灯");
                    Tts("收到指令，正在关闭卧室的灯");
                    BedCloseLight();
                }
                else if (intent.Equals("KitOn", StringComparison.OrdinalIgnoreCase))
                {
                    //synth.Speak("收到指令，正在打开厨房的灯");
                    Tts("收到指令，正在打开厨房的灯");
                    KitOpenLight();
                }
                else if (intent.Equals("KitOff", StringComparison.OrdinalIgnoreCase))
                {
                    //synth.Speak("收到指令，正在关闭厨房的灯");
                    Tts("收到指令，正在关闭厨房的灯");
                    KitCloseLight();
                }
                else if (intent.Equals("BKOn", StringComparison.OrdinalIgnoreCase))
                {
                    //synth.Speak("收到指令，正在打开所有的灯");
                    Tts("收到指令，正在打开所有的灯");
                    BKOnLight();
                }
                else if (intent.Equals("BKOff", StringComparison.OrdinalIgnoreCase))
                {
                    //synth.Speak("收到指令，正在关闭所有的灯");
                    Tts("收到指令，正在关闭所有的灯");
                    BKOffLight();
                }
                else if (intent.Equals("ACoff", StringComparison.OrdinalIgnoreCase))
                {
                    Tts("收到指令，关闭空调");
                    textBox5.Text = "关闭";

                }
                else if (intent.Equals("ACopen", StringComparison.OrdinalIgnoreCase))
                {
                    Tts("收到指令，打开空调");
                    textBox5.Text = "27度";
                }
                else if (intent.Equals("ManVoice", StringComparison.OrdinalIgnoreCase))
                {

                    //synth.Speak("收到指令，正在关闭卧室的灯并打开厨房的灯");
                    Tts("收到指令，正在切换声音");
                    style = 1;
                    Tts("切换完成");
                }
                else if (intent.Equals("WomanVoice", StringComparison.OrdinalIgnoreCase))
                {
                    Tts("收到指令，正在切换声音");
                    style = 4;
                    Tts("切换完成");
                }
                else if (intent.Equals("SpeedQuicker", StringComparison.OrdinalIgnoreCase))
                {
                    Tts("收到指令，正在调整速度");
                    speed += 2;
                    Tts("调整完成");
                }
                else if (intent.Equals("SpeedSlower", StringComparison.OrdinalIgnoreCase))
                {
                    Tts("收到指令，正在调整速度");
                    speed -= 3;
                    Tts("调整完成");
                }
                else if (intent.Equals("louder", StringComparison.OrdinalIgnoreCase))
                {
                    Tts("收到指令，正在增加音量");
                    volume += 3;
                    Tts("调整完成");
                }
                else if (intent.Equals("quieter", StringComparison.OrdinalIgnoreCase))
                {
                    Tts("收到指令，正在减小音量");
                    volume -= 3;
                    Tts("调整完成");
                }
                else if (intent.Equals("Stop", StringComparison.OrdinalIgnoreCase))
                {
                    buttonClick();
                    Tts("服务暂时停止，期待您下次使用");
                    button1.Text = "服务停止";

                }
                //else if (intent.Equals("quieter", StringComparison.OrdinalIgnoreCase))
                //{
                //    //synth.Speak("收到指令，正在关闭卧室的灯并打开厨房的灯");
                //    Tts("收到指令，正在关闭卧室的灯并打开厨房的灯");
                //    BKfnLight();
                //}
                else
                {
                    Tts("很抱歉，我没有理解您的指令");
                    //synth.Speak("很抱歉，我没有理解您的指令");
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

        // 合成
        public void Tts(string content)
        {
            var client = new Baidu.Aip.Speech.Tts(API_KEY, SECRET_KEY);
            client.Timeout = 60000;  // 修改超时时间
            // 可选参数
            var option = new Dictionary<string, object>()
            {
                {"spd",speed}, // 语速
                {"vol", volume}, // 音量
                {"per", style}  // 发音人，4：情感度丫丫童声
            };
            var result = client.Synthesis(content, option);

            if (result.ErrorCode == 0)  // 或 result.Success
            {
                File.WriteAllBytes(mp3id + ".mp3", result.Data);
            }
            wplayer.URL = mp3id + ".mp3";
            mp3id += 1;
            wplayer.controls.play();
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

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
