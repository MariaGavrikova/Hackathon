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

            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

            Task.WaitAny(stopRecognition.Task);

            await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
        }
    }
}
