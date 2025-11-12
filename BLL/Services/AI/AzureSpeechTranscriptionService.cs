using BLL.Settings;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NAudio.Wave; // added for Windows in-memory decode

namespace BLL.Services.AI
{
    public class AzureSpeechTranscriptionService : ITranscriptionService
    {
        private readonly SpeechSettings _settings;
        private readonly ILogger<AzureSpeechTranscriptionService> _logger;

        // Cache SpeechConfig per language to avoid re-allocating and mutating per call
        private readonly ConcurrentDictionary<string, SpeechConfig> _configCache = new();
        private AutoDetectSourceLanguageConfig? _autoDetectConfig;

        // -1 unknown,0 not available,1 available
        private int _ffmpegAvailableState = -1;

        public AzureSpeechTranscriptionService(IOptions<SpeechSettings> settings, ILogger<AzureSpeechTranscriptionService> logger)
        {
            _settings = settings.Value; _logger = logger;
        }

        public async Task<string?> TranscribeAsync(byte[] audioBytes, string fileName, string contentType, string? languageCode)
        {
            try
            {
                var ct = (contentType ?? string.Empty).ToLowerInvariant();
                var isWav = ct.Contains("wav") || fileName.ToLowerInvariant().EndsWith(".wav");

                // If not WAV -> try fast ffmpeg transcode to16kHz mono PCM WAV
                if (!isWav)
                {
                    byte[]? wavBytes = null;
                    try
                    {
                        wavBytes = await ConvertToPcmWavAsync(audioBytes);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "FFmpeg convert failed");
                    }

                    // Windows fallback decode using NAudio if ffmpeg unavailable
                    if (wavBytes == null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        try
                        {
                            using var inputMs = new MemoryStream(audioBytes);
                            using var reader = new Mp3FileReader(inputMs);
                            using var resampler = new MediaFoundationResampler(reader, new WaveFormat(16000,16,1));
                            using var pcmMs = new MemoryStream();
                            WaveFileWriter.WriteWavFileToStream(pcmMs, resampler);
                            wavBytes = pcmMs.ToArray();
                            _logger.LogDebug("NAudio MP3->WAV fallback used");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "NAudio fallback decode failed");
                        }
                    }

                    if (wavBytes != null)
                    {
                        _logger.LogDebug("Transcode/Decode->WAV path used for {ContentType}", ct);
                        return await RecognizeWavAsync(wavBytes, languageCode);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !IsFfmpegAvailable())
                    {
                        // On Linux if neither ffmpeg nor gstreamer available, bail out early to avoid SPXERR_GSTREAMER_NOT_FOUND_ERROR
                        _logger.LogWarning("Skipping compressed stream recognition for {ContentType} due to missing ffmpeg/gstreamer", ct);
                        return null;
                    }
                }

                // Direct WAV recognition if original or if previous conversion failed but was WAV
                if (isWav)
                {
                    var direct = await RecognizeWavAsync(audioBytes, languageCode);
                    if (!string.IsNullOrWhiteSpace(direct)) return direct;
                }

                // Fallback compressed stream path (requires gstreamer for mp3/ogg/webm). On Windows easier; on Linux only if ffmpeg available (assuming gstreamer installed alongside)
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || IsFfmpegAvailable())
                {
                    var configAndAuto = GetConfig(languageCode);
                    var (config, autoCfg) = (configAndAuto.config, configAndAuto.autoCfg);
                    string? ext = null;
                    if (!string.IsNullOrWhiteSpace(fileName)) ext = Path.GetExtension(fileName).ToLowerInvariant();
                    AudioStreamContainerFormat container = AudioStreamContainerFormat.ANY;
                    if (ct.Contains("mp3") || ext == ".mp3" || ct.Contains("mpeg"))
                        container = AudioStreamContainerFormat.MP3;
                    else if (ct.Contains("ogg") || ct.Contains("webm") || ext == ".ogg" || ext == ".webm")
                        container = AudioStreamContainerFormat.OGG_OPUS;
                    var format = AudioStreamFormat.GetCompressedFormat(container);
                    using (var push = AudioInputStream.CreatePushStream(format))
                    {
                        push.Write(audioBytes);
                        push.Close();
                        using var audioConfig = AudioConfig.FromStreamInput(push);
                        var text = await RecognizeOnceAsync(config, audioConfig, autoCfg);
                        if (!string.IsNullOrWhiteSpace(text)) return text;
                        if (autoCfg != null)
                        {
                            var enCfg = GetConfig("en-US").config;
                            using var push2 = AudioInputStream.CreatePushStream(format);
                            push2.Write(audioBytes); push2.Close();
                            using var audioConfig2 = AudioConfig.FromStreamInput(push2);
                            var forced = await RecognizeOnceAsync(enCfg, audioConfig2, null);
                            if (!string.IsNullOrWhiteSpace(forced)) return forced;
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Azure Speech transcription error");
                return null;
            }
        }

        private async Task<string?> RecognizeOnceAsync(SpeechConfig config, AudioConfig audioConfig, AutoDetectSourceLanguageConfig? autoCfg)
        {
            SpeechRecognitionResult result;
            if (autoCfg != null)
            {
                using var recognizer = new SourceLanguageRecognizer(config, autoCfg, audioConfig);
                result = await recognizer.RecognizeOnceAsync();
                var detected = result.Properties.GetProperty(PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult);
                _logger.LogDebug("STT Auto locale: {Locale}", detected);
            }
            else
            {
                using var recognizer = new SpeechRecognizer(config, audioConfig);
                result = await recognizer.RecognizeOnceAsync();
            }

            if (result.Reason == ResultReason.RecognizedSpeech) return result.Text;
            if (result.Reason == ResultReason.Canceled)
            {
                var details = CancellationDetails.FromResult(result);
                _logger.LogWarning("STT canceled. Code={Code}, Reason={Reason}, Error={Error}", details.ErrorCode, details.Reason, details.ErrorDetails);
            }
            else _logger.LogWarning("STT no match. Reason={Reason}", result.Reason);
            return null;
        }

        private (SpeechConfig config, AutoDetectSourceLanguageConfig? autoCfg) GetConfig(string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                var key = "auto";
                var cfg = _configCache.GetOrAdd(key, _ =>
                {
                    var c = SpeechConfig.FromSubscription(_settings.ApiKey, _settings.Region);
                    return c;
                });
                var langsList = (_settings.Languages?.Count >0 ? _settings.Languages : new List<string> { "en-US", "ja-JP", "zh-CN" })!;
                _autoDetectConfig ??= AutoDetectSourceLanguageConfig.FromLanguages(langsList.ToArray());
                return (cfg, _autoDetectConfig);
            }
            else
            {
                var lang = languageCode!;
                var cfg = _configCache.GetOrAdd(lang, _ =>
                {
                    var c = SpeechConfig.FromSubscription(_settings.ApiKey, _settings.Region);
                    c.SpeechRecognitionLanguage = lang;
                    return c;
                });
                return (cfg, null);
            }
        }

        // Try to convert arbitrary audio to16kHz mono PCM WAV using FFmpeg if available
        private async Task<byte[]?> ConvertToPcmWavAsync(byte[] input)
        {
            if (!IsFfmpegAvailable()) return null;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-hide_banner -loglevel error -i pipe:0 -ac1 -ar16000 -f wav pipe:1",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = new Process { StartInfo = psi, EnableRaisingEvents = false };
                proc.Start();
                await proc.StandardInput.BaseStream.WriteAsync(input,0, input.Length);
                await proc.StandardInput.BaseStream.FlushAsync();
                proc.StandardInput.Close();
                using var ms = new MemoryStream();
                await proc.StandardOutput.BaseStream.CopyToAsync(ms);
                _ = Task.Run(async () => { try { await proc.StandardError.ReadToEndAsync(); } catch { } });
                await proc.WaitForExitAsync();
                if (proc.ExitCode ==0 && ms.Length >0) return ms.ToArray();
                _logger.LogWarning("FFmpeg exited with code {Code}", proc.ExitCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "FFmpeg convert threw");
                return null;
            }
        }

        private bool IsFfmpegAvailable()
        {
            if (_ffmpegAvailableState != -1) return _ffmpegAvailableState ==1;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) { _ffmpegAvailableState =0; return false; }
                proc.WaitForExit(2000);
                _ffmpegAvailableState = proc.ExitCode ==0 ?1 :0;
            }
            catch { _ffmpegAvailableState =0; }
            return _ffmpegAvailableState ==1;
        }

        private async Task<string?> RecognizeWavAsync(byte[] wavBytes, string? languageCode)
        {
            if (TryParseWavHeader(wavBytes, out var header))
            {
                try
                {
                    var (config, autoCfg) = GetConfig(languageCode);
                    var fmt = AudioStreamFormat.GetWaveFormatPCM((uint)header.SampleRate, (byte)header.BitsPerSample, (byte)header.Channels);
                    using var push = AudioInputStream.CreatePushStream(fmt);
                    var dataOffset = header.DataOffset;
                    var dataLen = Math.Min(header.DataLength, wavBytes.Length - dataOffset);
                    const int chunkSize =32 *1024; int remaining = dataLen; int pos = dataOffset;
                    while (remaining >0)
                    {
                        int toCopy = Math.Min(chunkSize, remaining);
                        var buffer = ArrayPool<byte>.Shared.Rent(toCopy);
                        try
                        {
                            Buffer.BlockCopy(wavBytes, pos, buffer,0, toCopy);
                            var exact = new byte[toCopy]; Buffer.BlockCopy(buffer,0, exact,0, toCopy);
                            push.Write(exact);
                        }
                        finally { ArrayPool<byte>.Shared.Return(buffer); }
                        pos += toCopy; remaining -= toCopy;
                    }
                    push.Close();
                    using var audioCfg = AudioConfig.FromStreamInput(push);
                    var text = await RecognizeOnceAsync(config, audioCfg, autoCfg);
                    return text;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "In-memory WAV stream recognition failed; fallback to temp file");
                }
            }
            // Fallback: write temp file
            var tmp = Path.Combine(Path.GetTempPath(), $"stt_{Guid.NewGuid():N}.wav");
            await File.WriteAllBytesAsync(tmp, wavBytes);
            try
            {
                var (config2, autoCfg2) = GetConfig(languageCode);
                using var audioCfg2 = AudioConfig.FromWavFileInput(tmp);
                var text = await RecognizeOnceAsync(config2, audioCfg2, autoCfg2);
                return text;
            }
            finally
            {
                try { File.Delete(tmp); } catch { }
            }
        }

        private bool TryParseWavHeader(byte[] bytes, out WavHeader header)
        {
            header = default;
            if (bytes.Length <44) return false;
            if (System.Text.Encoding.ASCII.GetString(bytes,0,4) != "RIFF") return false;
            if (System.Text.Encoding.ASCII.GetString(bytes,8,4) != "WAVE") return false;
            int fmtIndex =12;
            while (fmtIndex +8 <= bytes.Length)
            {
                var chunkId = System.Text.Encoding.ASCII.GetString(bytes, fmtIndex,4);
                int chunkSize = BitConverter.ToInt32(bytes, fmtIndex +4);
                if (chunkId == "fmt ")
                {
                    short audioFormat = BitConverter.ToInt16(bytes, fmtIndex +8);
                    short channels = BitConverter.ToInt16(bytes, fmtIndex +10);
                    int sampleRate = BitConverter.ToInt32(bytes, fmtIndex +12);
                    short bitsPerSample = BitConverter.ToInt16(bytes, fmtIndex +22);
                    int dataSearch = fmtIndex +8 + chunkSize;
                    int dataIndex = dataSearch;
                    while (dataIndex +8 <= bytes.Length)
                    {
                        var id2 = System.Text.Encoding.ASCII.GetString(bytes, dataIndex,4);
                        int size2 = BitConverter.ToInt32(bytes, dataIndex +4);
                        if (id2 == "data")
                        {
                            header = new WavHeader
                            {
                                Channels = channels,
                                SampleRate = sampleRate,
                                BitsPerSample = bitsPerSample,
                                DataOffset = dataIndex +8,
                                DataLength = size2
                            };
                            return audioFormat ==1; // PCM
                        }
                        dataIndex +=8 + size2;
                    }
                    return false;
                }
                fmtIndex +=8 + chunkSize;
            }
            return false;
        }

        private struct WavHeader
        {
            public short Channels; public int SampleRate; public short BitsPerSample; public int DataOffset; public int DataLength;
        }
    }
}
