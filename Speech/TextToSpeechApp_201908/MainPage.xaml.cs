using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TextToSpeechSample_201908;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace TextToSpeechApp_201908
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MediaPlayer mediaPlayer1;
        private MediaPlayer mediaPlayer2;
        private readonly string _speechServiceApiKey = "YOUR_SPEECH_API_KEY"; //Replace with your Speech API Key
        private readonly string _speechServiceLocation = "southeastasia"; //Replace with your Speech API location
        private List<SynthesisLanguage> languageList = new List<SynthesisLanguage>();

        public MainPage()
        {
            this.InitializeComponent();

            this.mediaPlayer1 = new MediaPlayer();
            this.mediaPlayer2 = new MediaPlayer();

            // Please replace | add language code and voices
            // Refer: https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support#text-to-speech
            languageList.Add(new SynthesisLanguage() { LanguageCode = "ja-JP", StandardVoice = "ja-JP-HarukaRUS", NeuralVoice = "ja-JP-NanamiNeural" });
            foreach (var item in languageList)
            {
                LanguageSelectBox.Items.Add(item.LanguageCode);
            }

            SpeakButton1.IsEnabled = false;
            SpeakButton2.IsEnabled = false;
        }
        private async void LoadButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (LanguageSelectBox.SelectedValue != null & TextForSynthesis.Text !="")
            {
                var language = LanguageSelectBox.SelectedValue.ToString();
                var text = TextForSynthesis.Text;

                var config1 = SpeechConfig.FromSubscription(_speechServiceApiKey, _speechServiceLocation);
                config1.SpeechSynthesisLanguage = language;
                config1.SpeechSynthesisVoiceName = languageList.Find(x => x.LanguageCode == language).StandardVoice;

                var config2 = SpeechConfig.FromSubscription(_speechServiceApiKey, _speechServiceLocation);
                config2.SpeechSynthesisLanguage = language;
                config2.SpeechSynthesisVoiceName = languageList.Find(x => x.LanguageCode == language).NeuralVoice;

                var audioFile1 = await SynthesizeText(config1, text, "Standard");
                var audioFile2 = await SynthesizeText(config2, text, "Neural");

                if (audioFile1 != null)
                {
                    mediaPlayer1.Source = MediaSource.CreateFromStorageFile(await StorageFile.GetFileFromPathAsync(audioFile1));
                    SpeakButton1.IsEnabled = true;
                }
                if (audioFile2 != null)
                {
                    mediaPlayer2.Source = MediaSource.CreateFromStorageFile(await StorageFile.GetFileFromPathAsync(audioFile2));
                    SpeakButton2.IsEnabled = true;
                }
            }

        }

        private void SpeakButton1_Clicked(object sender, RoutedEventArgs e)
        {
            mediaPlayer1.Play();
        }

        private void SpeakButton2_Clicked(object sender, RoutedEventArgs e)
        {
            mediaPlayer2.Play();
        }

        private void StopButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer1.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                mediaPlayer1.Pause();
            if (mediaPlayer2.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                mediaPlayer2.Pause();
        }


        private async Task<string> SynthesizeText(SpeechConfig config, string text, string synthesisType)
        {
            try
            {
                using (var synthesizer = new SpeechSynthesizer(config, null))
                {
                    // Receive a text from TextForSynthesis text box and synthesize it to speaker.
                    using (var result = await synthesizer.SpeakTextAsync(text).ConfigureAwait(false))
                    {
                        // Checks result.
                        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                        {
                            NotifyUser($"Speech Synthesis(" + synthesisType + ")Succeeded.", NotifyType.StatusMessage);

                            using (var audioStream = AudioDataStream.FromResult(result))
                            {
                                // Save synthesized audio data as a wave file and user MediaPlayer to play it
                                var filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, synthesisType + ".wav");
                                await audioStream.SaveToWaveFileAsync(filePath);
                                return filePath;
                            }

                        }
                        else if (result.Reason == ResultReason.Canceled)
                        {
                            var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine($"CANCELED: Reason={cancellation.Reason}");
                            sb.AppendLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                            sb.AppendLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");

                            NotifyUser(sb.ToString(), NotifyType.ErrorMessage);

                            return null;
                        }
                        else
                            return null;

                    }
                }

            }
            catch
            {
                NotifyUser($"Speech Synthesis Error", NotifyType.StatusMessage);
                return null;
            }
        }

        private enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };

        private void NotifyUser(string strMessage, NotifyType type)
        {
            // If called from the UI thread, then update immediately.
            // Otherwise, schedule a task on the UI thread to perform the update.
            if (Dispatcher.HasThreadAccess)
            {
                UpdateStatus(strMessage, type);
            }
            else
            {
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateStatus(strMessage, type));
            }
        }

        private void UpdateStatus(string strMessage, NotifyType type)
        {
            StatusBlock.Text = string.IsNullOrEmpty(StatusBlock.Text) ? strMessage : "\n" + strMessage;

            // Raise an event if necessary to enable a screen reader to announce the status update.
            var peer = Windows.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(StatusBlock);
            if (peer != null)
            {
                peer.RaiseAutomationEvent(Windows.UI.Xaml.Automation.Peers.AutomationEvents.LiveRegionChanged);
            }
        }

    }

}
