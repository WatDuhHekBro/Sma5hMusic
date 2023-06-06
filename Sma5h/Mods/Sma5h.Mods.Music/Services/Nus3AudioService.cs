﻿using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sma5h.Interfaces;
using Sma5h.Mods.Music.Helpers;
using Sma5h.Mods.Music.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Sma5h.Mods.Music.Services
{
    public class Nus3AudioService : INus3AudioService
    {
        private readonly ILogger _logger;
        private readonly IProcessService _processService;
        private readonly IAudioMetadataService _audioMetadataService;
        private readonly IOptionsMonitor<Sma5hMusicOptions> _config;
        private readonly string _nus3AudioExeFile;
        private readonly string _nus3BankTemplateFile;
        private ushort _lastBankId;

        public Nus3AudioService(IOptionsMonitor<Sma5hMusicOptions> config, IAudioMetadataService audioMetadataService, IProcessService processService, ILogger<INus3AudioService> logger)
        {
            _logger = logger;
            _processService = processService;
            _audioMetadataService = audioMetadataService;
            _config = config;
            _nus3AudioExeFile = Path.Combine(config.CurrentValue.ToolsPath, MusicConstants.Resources.NUS3AUDIO_EXE_FILE);
            _nus3BankTemplateFile = Path.Combine(config.CurrentValue.ResourcesPath, MusicConstants.Resources.NUS3BANK_TEMPLATE_FILE);

            var nus3BankIds = GetCoreNus3BankIds();
            _lastBankId = (ushort)(nus3BankIds.Count > 0 ? nus3BankIds.Values.OrderByDescending(p => p).First() : 0);
        }

        public bool GenerateNus3Audio(string toneId, string inputMediaFile, string outputMediaFile)
        {
            _logger.LogDebug("Generate nus3audio {InternalToneName} from {AudioInputFile} to {Nus3AudioOutputFile}", toneId, inputMediaFile, outputMediaFile);

            EnsureRequiredFilesAreFound();

            if (!File.Exists(inputMediaFile))
            {
                _logger.LogError("File {mediaPath} does not exist....", inputMediaFile);
                return false;
            }

            //Test nus3audio
            if (Path.GetExtension(inputMediaFile).ToLower() == ".nus3audio")
            {
                //Double checking that tone ids match
                var fileToneId = GetToneIdFromNus3Audio(inputMediaFile);
                //TODO : This doesn't seem to matter, but should definitely fix
                //if (fileToneId != toneId)
                //{
                //    _logger.LogError("The ToneId within the nus3audio {ToneIdNus3Audio} doesn't match the ToneId {ToneId} registered in the mod..", fileToneId, toneId);
                //    return false;
                //}
                File.Copy(inputMediaFile, outputMediaFile);
                return true;
            }

            //Handle conversation if necessary
            if (MusicConstants.EXTENSIONS_NEED_CONVERSION.Contains(Path.GetExtension(inputMediaFile).ToLower()))
                return ConvertIncompatibleFormat(toneId, ref inputMediaFile, outputMediaFile);

            //Create nus3audio
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputMediaFile));
                _processService.RunProcess(_nus3AudioExeFile, $"-n -w \"{outputMediaFile}\"");
                _processService.RunProcess(_nus3AudioExeFile, $"-A {toneId} \"{inputMediaFile}\" -w \"{outputMediaFile}\"");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while generating nus3audio file");
                return false;
            }

            return true;
        }

        public string GetToneIdFromNus3Audio(string inputMediaFile)
        {
            _logger.LogDebug("Retrieving ToneId from {InputMediaFile}...", inputMediaFile);

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var fileStream = File.Open(inputMediaFile, FileMode.Open, FileAccess.Read))
                    {
                        using (var w = new BinaryReader(fileStream))
                        {
                            w.BaseStream.Position = 0x48; //ToneId

                            var sb = new StringBuilder();
                            char c;
                            while ((c = w.ReadChar()) != '\0')
                            {

                                sb.Append(c);
                            };
                            return sb.ToString();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return null;
            }
        }

        public bool GenerateNus3Bank(string toneId, float volume, string outputMediaFile)
        {
            _logger.LogDebug("Generate nus3bank {InternalToneName} from {Nus3BankInputFile} to {Nus3BankOutputFile}", toneId, _nus3BankTemplateFile, outputMediaFile);

            EnsureRequiredFilesAreFound();


            using (var memoryStreamWrite = new MemoryStream())
            {
                long nameIdPosition = 0;
                using (var memoryStreamRead = new MemoryStream())
                {
                    using (var fileStream = File.Open(_nus3BankTemplateFile, FileMode.Open, FileAccess.Read))
                    {
                        fileStream.CopyTo(memoryStreamWrite);
                        fileStream.Position = 0;
                        fileStream.CopyTo(memoryStreamRead);
                    }

                    //Automatically retrieve position nameId
                    using (var w = new BinaryReader(memoryStreamRead))
                    {
                        w.BaseStream.Position = 0x98;
                        var sizeBlock = w.ReadUInt16();
                        nameIdPosition = w.BaseStream.Position + sizeBlock - 2;
                    }
                }

                var bytes = memoryStreamWrite.ToArray();
                var found = ByteHelper.Locate(bytes, new byte[] { 0xE8, 0x22, 0x00, 0x00 });

                using (var w = new BinaryWriter(memoryStreamWrite))
                {
                    w.BaseStream.Position = nameIdPosition; //NameId
                    w.Write(GetNewNus3BankId());
                    if (found.Length != 3)
                    {
                        _logger.LogError("Error while locating the volume offset in the nus3bank");
                    }
                    else
                    {
                        w.BaseStream.Position = found[1] + 4;
                        w.Write(volume);
                    }
                }
                Directory.CreateDirectory(Path.GetDirectoryName(outputMediaFile));
                File.WriteAllBytes(outputMediaFile, memoryStreamWrite.ToArray());
            }

            return true;
        }

        private ushort GetNewNus3BankId()
        {
            _lastBankId++;
            return _lastBankId;
        }

        private void EnsureRequiredFilesAreFound()
        {
            if (!File.Exists(_nus3AudioExeFile))
                throw new Exception($"nus3audio: {_nus3AudioExeFile} could not be found.");

            if (!File.Exists(_nus3BankTemplateFile))
                throw new Exception($"template.nus3bank: {_nus3BankTemplateFile} could not be found.");
        }

        private bool ConvertIncompatibleFormat(string toneId, ref string inputMediaFile, string outputMediaFile, bool isFallback = false)
        {
            bool result = false;
            var formatConversation = isFallback ? _config.CurrentValue.Sma5hMusic.AudioConversionFormatFallBack : _config.CurrentValue.Sma5hMusic.AudioConversionFormat;
            var tempFile = Path.Combine(_config.CurrentValue.TempPath, string.Format(MusicConstants.Resources.NUS3AUDIO_TEMP_FILE, formatConversation));
            if (_audioMetadataService.ConvertAudio(inputMediaFile, tempFile))
            {
                inputMediaFile = tempFile;
                result = GenerateNus3Audio(toneId, inputMediaFile, outputMediaFile);
            }
            if (File.Exists(tempFile))
                File.Delete(tempFile);

            if (!result && !isFallback && !string.IsNullOrEmpty(_config.CurrentValue.Sma5hMusic.AudioConversionFormatFallBack)
                && _config.CurrentValue.Sma5hMusic.AudioConversionFormat.ToLower() != _config.CurrentValue.Sma5hMusic.AudioConversionFormatFallBack.ToLower())
            {
                _logger.LogWarning("The conversion from {InputMediaFile} to {OutputMediaFile} failed. Trying fallback conversation format.", inputMediaFile, outputMediaFile);
                return ConvertIncompatibleFormat(toneId, ref inputMediaFile, outputMediaFile, true);
            }

            return result;
        }

        private Dictionary<string, ushort> GetCoreNus3BankIds()
        {
            var output = new Dictionary<string, ushort>();

            var nusBankResourceFile = Path.Combine(_config.CurrentValue.ResourcesPath, MusicConstants.Resources.NUS3BANK_IDS_FILE);
            if (!File.Exists(nusBankResourceFile))
                return output;

            _logger.LogDebug("Retrieving NusBankIds from CSV {CSVResource}", nusBankResourceFile);
            using (var reader = new StreamReader(nusBankResourceFile))
            {
                var csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    PrepareHeaderForMatch = (args) => Regex.Replace(args.Header, @"\s", string.Empty)
                };
                using (var csv = new CsvReader(reader, csvConfiguration))
                {
                    var records = csv.GetRecords<dynamic>();
                    foreach (var record in records)
                    {
                        var id = Convert.ToUInt16(record.ID, 16);
                        output.Add(record.NUS3BankName, id);
                    }
                }
            }
            return output;
        }
    }
}
