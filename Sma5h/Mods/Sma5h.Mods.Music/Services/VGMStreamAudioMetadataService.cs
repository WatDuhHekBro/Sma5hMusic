﻿using Microsoft.Extensions.Logging;
using Sma5h.Mods.Music.Interfaces;
using Sma5h.Mods.Music.Models;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using VGMMusic;

namespace Sma5h.Mods.Music.Services
{
    public class VGMStreamAudioMetadataService : IAudioMetadataService
    {
        private readonly ILogger _logger;
        private readonly IVGMMusicPlayer _vgmMusicPlayer;

        public VGMStreamAudioMetadataService(ILogger<IAudioMetadataService> logger, IVGMMusicPlayer vgmMusicPlayer)
        {
            _logger = logger;
            _vgmMusicPlayer = vgmMusicPlayer;
        }

        public async Task<AudioCuePoints> GetCuePoints(string inputFile)
        {
            _logger.LogDebug("Retrieving audio metadata for {FilePath}...", inputFile);

            VGMAudioCuePoints audioCuePoints = null;
            try
            {
                audioCuePoints = await _vgmMusicPlayer.GetAudioCuePoints(inputFile);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }

            _logger.LogDebug("VGMAudio Metadata for {FilePath}: TotalSamples: {TotalSamples}, LoopStartSample: {LoopStartSample}, LoopEndSample: {LoopEndSample}, LoopStartMs: {LoopStartMs}, LoopEndMs: {LoopEndMs}",
                inputFile, audioCuePoints.TotalSamples, audioCuePoints.LoopStartSample, audioCuePoints.LoopEndSample, audioCuePoints.LoopStartMs, audioCuePoints.LoopEndMs);

            if (audioCuePoints.TotalSamples == 0 || audioCuePoints.LoopEndSample == 0)
            {
                _logger.LogWarning("VGMAudio Metadata for {FilePath}: Total Samples, Frequency or/and loop end sample was 0! Check the logs for more information. Use song_cue_points_override property in the payload to override these values.", inputFile);
            }

            if (audioCuePoints.TotalSamples < 0 || audioCuePoints.LoopStartSample < 0 || audioCuePoints.LoopEndSample < 0
                || audioCuePoints.TotalTimeMs < 0 || audioCuePoints.LoopStartMs < 0)
            {
                _logger.LogWarning("VGMAudio Metadata for {FilePath}: Some cue values are negative. This is shouldn't happen.", inputFile);
            }

            return new AudioCuePoints()
            {
                TotalSamples = (uint)audioCuePoints.TotalSamples,
                LoopStartSample = (uint)audioCuePoints.LoopStartSample,
                LoopEndSample = (uint)audioCuePoints.LoopEndSample,
                TotalTimeMs = (uint)audioCuePoints.TotalTimeMs,
                LoopStartMs = (uint)audioCuePoints.LoopStartMs,
                LoopEndMs = (uint)audioCuePoints.LoopEndMs
            };
        }

        public bool ConvertAudio(string inputMediaFile, string outputMediaFile)
        {
            _logger.LogDebug("Convert BRSTM from {AudioMediaFile} to {AudioOutputFile}", inputMediaFile, outputMediaFile);

            if (!File.Exists(inputMediaFile))
            {
                _logger.LogError("File {mediaPath} does not exist....", inputMediaFile);
                return false;
            }

            if (File.Exists(outputMediaFile))
            {
                _logger.LogDebug("The conversion from {InputMediaFile} to {OutputMediaFile} was skipped. The file already exists.", inputMediaFile, outputMediaFile);
                return true;
            }

            var builder = new StringBuilder();

            var oldValue = Console.Out;
            using (var writer = new StringWriter(builder))
            {
                Console.SetOut(writer);
                if (outputMediaFile.EndsWith("lopus"))
                {
                    //Special tags for opus
                    Converter.RunConverterCli(new string[] { "-i", inputMediaFile, "-o", outputMediaFile, "--opusheader", "Namco", "--cbr" });
                }
                else
                {
                    Converter.RunConverterCli(new string[] { "-i", inputMediaFile, "-o", outputMediaFile });
                }
            }
            Console.SetOut(oldValue);

            var output = builder.ToString();

            _logger.LogDebug("VGAudio Convert for {OutputMediaFile}: {Data}", outputMediaFile, output.Trim('\r', '\n'));

            if (!File.Exists(outputMediaFile) || new FileInfo(outputMediaFile).Length == 0)
            {
                _logger.LogError("VGAudio Error - The conversion from {InputMediaFile} to {OutputMediaFile} failed. Reason {Reason}", inputMediaFile, outputMediaFile, output.Trim('\r', '\n'));
                return false;
            }
            return true;
        }

        private ulong ReadValueUInt64Safe(string searchString, string parsingStartIndex, string parsingEndIndex = " ")
        {
            var output = searchString.Split(parsingStartIndex);
            if (output.Length > 1)
            {
                var foundValue = output[1].Split(parsingEndIndex)[0];
                if (ulong.TryParse(foundValue, out ulong result))
                {
                    return result;
                }
            }
            return 0;
        }

        private uint ReadValueUInt32Safe(string searchString, string parsingStartIndex, string parsingEndIndex = " ")
        {
            var output = searchString.Split(parsingStartIndex);
            if (output.Length > 1)
            {
                var foundValue = output[1].Split(parsingEndIndex)[0];
                if (uint.TryParse(foundValue, out uint result))
                {
                    return result;
                }
            }
            return 0;
        }

        private ushort ReadValueUInt16Safe(string searchString, string parsingStartIndex, string parsingEndIndex = " ")
        {
            var output = searchString.Split(parsingStartIndex);
            if (output.Length > 1)
            {
                var foundValue = output[1].Split(parsingEndIndex)[0];
                if (ushort.TryParse(foundValue, out ushort result))
                {
                    return result;
                }
            }
            return 0;
        }
    }
}
