using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;

namespace Hackathon
{
    public static class AudioBlobTrigger
    {
        private const string Key = "de687e397f18495798b75b383e1c26ce";
        private const string Region = "eastus";

        [FunctionName("AudioBlobTrigger")]
        public static async Task Run([BlobTrigger("audio-files/{name}", Connection = "AudioFilesStorage")] Stream blobStream, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {blobStream.Length} Bytes");

            var config = SpeechConfig.FromSubscription(Key, Region);

            var reader = new BinaryReader(blobStream);
            using var audioInputStream = AudioInputStream.CreatePushStream();
            using var audioConfig = AudioConfig.FromStreamInput(audioInputStream);
            using var recognizer = new SpeechRecognizer(config, audioConfig);

            var stopRecognition = new TaskCompletionSource<int>();

            byte[] readBytes;
            do
            {
                readBytes = reader.ReadBytes(1024);
                audioInputStream.Write(readBytes, readBytes.Length);
            } while (readBytes.Length > 0);

            recognizer.Recognizing += (s, e) =>
            {
                //Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
            };

            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                }
            };

            recognizer.Canceled += (s, e) =>
            {
                Console.WriteLine($"CANCELED: Reason={e.Reason}");

                if (e.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                    Console.WriteLine($"CANCELED: Did you update the subscription info?");
                }

                stopRecognition.TrySetResult(0);
            };

            recognizer.SessionStarted += (s, e) =>
            {
                Console.WriteLine("\n    Session started event.");
            };

            recognizer.SessionStopped += (s, e) =>
            {
                Console.WriteLine("\n    Session stopped event.");
                Console.WriteLine("\nStop recognition.");
                stopRecognition.TrySetResult(0);
            };

            // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

            // Waits for completion.
            // Use Task.WaitAny to keep the task rooted.
            Task.WaitAny(stopRecognition.Task);

            // Stops recognition.
            await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
        }

        private static async void EndDetectedHandler(object? sender, RecognitionEventArgs e)
        {
            Console.WriteLine("----------------------End----------------------");

            if (sender is SpeechRecognizer recognizer)
            {
                await recognizer.StopContinuousRecognitionAsync();
            }
        }

        private static void StartDetectedHandler(object? sender, RecognitionEventArgs e)
        {
            Console.WriteLine("----------------------Start----------------------");
        }

        private static void RecognizingHandler(object? sender, SpeechRecognitionEventArgs e)
        {
            
        }

        private static void RecognizedHandler(object? sender, SpeechRecognitionEventArgs e)
        {
            WriteRecognitionResult(e.Result);
        }

        private static void CancelledHandler(object? sender, SpeechRecognitionCanceledEventArgs e)
        {
            
        }

        private static async Task RecognizeOnePhraseAsync(SpeechRecognizer recognizer)
        {
            var result = await recognizer.RecognizeOnceAsync();

            WriteRecognitionResult(result);
        }

        private static void WriteRecognitionResult(SpeechRecognitionResult result)
        {
            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                Console.WriteLine($"We recognized: {result.Text}");
            }
            else if (result.Reason == ResultReason.NoMatch)
            {
                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = CancellationDetails.FromResult(result);
                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                    Console.WriteLine($"CANCELED: Did you update the subscription info?");
                }
            }
        }

        //    public static async Task TranscribeConversationsAsync(string conversationWaveFile, string subscriptionKey, string region)
        //    {
        //        var config = SpeechConfig.FromSubscription(subscriptionKey, region);
        //        config.SetProperty("ConversationTranscriptionInRoomAndOnline", "true");
        //        var stopRecognition = new TaskCompletionSource<int>();

        //        // Create an audio stream from a wav file or from the default microphone if you want to stream live audio from the supported devices
        //        using (var audioInput = AudioStreamReader.OpenWavFile(conversationWaveFile))
        //        {
        //            var meetingID = Guid.NewGuid().ToString();
        //            using (var conversation = await Conversation.CreateConversationAsync(config, meetingID))
        //            {
        //                // Create a conversation transcriber using audio stream input
        //                using (var conversationTranscriber = new ConversationTranscriber(audioInput))
        //                {
        //                    // Subscribe to events
        //                    conversationTranscriber.Transcribing += (s, e) =>
        //                    {
        //                        Console.WriteLine($"TRANSCRIBING: Text={e.Result.Text} SpeakerId={e.Result.UserId}");
        //                    };

        //                    conversationTranscriber.Transcribed += (s, e) =>
        //                    {
        //                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
        //                        {
        //                            Console.WriteLine($"TRANSCRIBED: Text={e.Result.Text} SpeakerId={e.Result.UserId}");
        //                        }
        //                        else if (e.Result.Reason == ResultReason.NoMatch)
        //                        {
        //                            Console.WriteLine($"NOMATCH: Speech could not be recognized.");
        //                        }
        //                    };

        //                    conversationTranscriber.Canceled += (s, e) =>
        //                    {
        //                        Console.WriteLine($"CANCELED: Reason={e.Reason}");

        //                        if (e.Reason == CancellationReason.Error)
        //                        {
        //                            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
        //                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
        //                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
        //                            stopRecognition.TrySetResult(0);
        //                        }
        //                    };

        //                    conversationTranscriber.SessionStarted += (s, e) =>
        //                    {
        //                        Console.WriteLine($"\nSession started event. SessionId={e.SessionId}");
        //                    };

        //                    conversationTranscriber.SessionStopped += (s, e) =>
        //                    {
        //                        Console.WriteLine($"\nSession stopped event. SessionId={e.SessionId}");
        //                        Console.WriteLine("\nStop recognition.");
        //                        stopRecognition.TrySetResult(0);
        //                    };

        //                    // Add participants to the conversation.
        //                    // Voice signature needs to be in the following format:
        //                    // { "Version": <Numeric value>, "Tag": "string", "Data": "string" }
        //                    var languageForUser1 = "User1PreferredLanguage"; // For example "en-US"
        //                    var speakerA = Participant.From("User1", languageForUser1, voiceSignatureUser1);
        //                    var languageForUser2 = "User2PreferredLanguage"; // For example "en-US"
        //                    var speakerB = Participant.From("User2", languageForUser2, voiceSignatureUser2);
        //                    await conversation.AddParticipantAsync(speakerA);
        //                    await conversation.AddParticipantAsync(speakerB);

        //                    // Join to the conversation.
        //                    await conversationTranscriber.JoinConversationAsync(conversation);

        //                    // Starts transcribing of the conversation. Uses StopTranscribingAsync() to stop transcribing when all participants leave.
        //                    await conversationTranscriber.StartTranscribingAsync().ConfigureAwait(false);

        //                    // Waits for completion.
        //                    // Use Task.WaitAny to keep the task rooted.
        //                    Task.WaitAny(new[] { stopRecognition.Task });

        //                    // Stop transcribing the conversation.
        //                    await conversationTranscriber.StopTranscribingAsync().ConfigureAwait(false);
        //                }
        //            }
        //        }
        //    }
    }
}
