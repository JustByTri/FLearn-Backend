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
                // If not WAV -> try fast ffmpeg transcode to16kHz mono PCM WAV, then recognize from memory
                if (!ct.Contains("wav"))
                {
                    try
                    {
                        var wavBytes = await ConvertToPcmWavAsync(audioBytes);
                        if (wavBytes != null)
                        {
                            _logger.LogDebug("Transcode->WAV path used for {ContentType}", ct);
                            return await RecognizeWavAsync(wavBytes, languageCode);
                        }
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        {
                            _logger.LogWarning("FFmpeg not available on Linux for {ContentType}. Consider installing FFmpeg or upload WAV.", ct);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "FFmpeg convert failed, fallback to compressed stream path");
                    }
                }

                // If it's WAV already or after conversion failed, try in-memory WAV recognition first
                if (ct.Contains("wav"))
                {
                    var result = await RecognizeWavAsync(audioBytes, languageCode);
                    if (!string.IsNullOrWhiteSpace(result)) return result;
                }

                // Before using compressed stream path, avoid GStreamer requirement on Linux when FFmpeg is missing
                if (!ct.Contains("wav") && RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !IsFfmpegAvailable())
                {
                    _logger.LogWarning("Skipping compressed stream recognition for {ContentType} on Linux due to missing GStreamer/FFmpeg. Returning null transcript.", ct);
                    return null;
                }

                // Fallback: original compressed stream path (requires GStreamer for mp3/ogg/webm)
                var configAndAuto = GetConfig(languageCode);
                var (config, autoCfg) = (configAndAuto.config, configAndAuto.autoCfg);

                // Determine by extension/content type
                string? ext = null;
                if (!string.IsNullOrWhiteSpace(fileName)) ext = Path.GetExtension(fileName).ToLowerInvariant();

                AudioStreamContainerFormat container = AudioStreamContainerFormat.ANY;
                if (ct.Contains("mp3") || ext == ".mp3" || ct.Contains("mpeg"))
                {
                    container = AudioStreamContainerFormat.MP3;
                }
                else if (ct.Contains("ogg") || ct.Contains("webm") || ext == ".ogg" || ext == ".webm")
                {
                    container = AudioStreamContainerFormat.OGG_OPUS;
                }

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
                        // Retry with en-US if auto-detect path yielded nothing
                        var enCfg = GetConfig("en-US").config;
                        using var push2 = AudioInputStream.CreatePushStream(format);
                        push2.Write(audioBytes);
                        push2.Close();
                        using var audioConfig2 = AudioConfig.FromStreamInput(push2);
                        var forcedText = await RecognizeOnceAsync(enCfg, audioConfig2, null);
                        if (!string.IsNullOrWhiteSpace(forcedText)) return forcedText;
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

            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                return result.Text;
            }
            if (result.Reason == ResultReason.Canceled)
            {
                var details = CancellationDetails.FromResult(result);
                _logger.LogWarning("STT canceled. Code={Code}, Reason={Reason}, Error={Error}", details.ErrorCode, details.Reason, details.ErrorDetails);
            }
            else
            {
                _logger.LogWarning("STT no match. Reason={Reason}", result.Reason);
            }
            return null;
        }

        // Reuse SpeechConfig per language; if language is null/empty, return config without language and an AutoDetect config
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

                // Write input to stdin
                await proc.StandardInput.BaseStream.WriteAsync(input,0, input.Length);
                await proc.StandardInput.BaseStream.FlushAsync();
                proc.StandardInput.Close();

                // Read stdout fully
                using var ms = new MemoryStream();
#if NET8_0_OR_GREATER
 await proc.StandardOutput.BaseStream.CopyToAsync(ms);
#else
 await proc.StandardOutput.BaseStream.CopyToAsync(ms,81920);
#endif
                // Consume stderr to avoid deadlocks
                _ = Task.Run(async () =>
                {
                    try { await proc.StandardError.ReadToEndAsync(); } catch { }
                });

                await proc.WaitForExitAsync();
                if (proc.ExitCode ==0 && ms.Length >0)
                {
                    return ms.ToArray();
                }
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
            catch
            {
                _ffmpegAvailableState =0;
            }
            return _ffmpegAvailableState ==1;
        }

        // Recognize WAV bytes by parsing header and streaming PCM to the SDK without temp files. Fallback to temp file if parsing fails.
        private async Task<string?> RecognizeWavAsync(byte[] wavBytes, string? languageCode)
        {
            if (TryParseWavHeader(wavBytes, out var header))
            {
                try
                {
                    var (config, autoCfg) = GetConfig(languageCode);
                    var fmt = AudioStreamFormat.GetWaveFormatPCM((uint)header.SampleRate, (byte)header.BitsPerSample, (byte)header.Channels);
                    using var push = AudioInputStream.CreatePushStream(fmt);

                    // Write PCM data portion in chunks using buffer pool
                    var dataOffset = header.DataOffset;
                    var dataLen = Math.Min(header.DataLength, wavBytes.Length - dataOffset);
                    const int chunkSize =32 *1024;
                    int remaining = dataLen;
                    int pos = dataOffset;
                    while (remaining >0)
                    {
                        int toCopy = Math.Min(chunkSize, remaining);
                        var buffer = ArrayPool<byte>.Shared.Rent(toCopy);
                        try
                        {
                            Buffer.BlockCopy(wavBytes, pos, buffer,0, toCopy);
                            // Write exact slice
                            var exact = new byte[toCopy];
                            Buffer.BlockCopy(buffer,0, exact,0, toCopy);
                            push.Write(exact);
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(buffer);
                        }
                        pos += toCopy;
                        remaining -= toCopy;
                    }
                    push.Close();

                    using var audioCfg = AudioConfig.FromStreamInput(push);
                    var text = await RecognizeOnceAsync(config, audioCfg, autoCfg);
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "In-memory WAV recognition failed, will fallback to temp file");
                }
            }

            // Fallback: write to temp file and use FromWavFileInput
            try
            {
                var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".wav");
                await File.WriteAllBytesAsync(tmp, wavBytes);
                try
                {
                    var (config, autoCfg) = GetConfig(languageCode);
                    using var audioCfg = AudioConfig.FromWavFileInput(tmp);
                    var text = await RecognizeOnceAsync(config, audioCfg, autoCfg);
                    return text;
                }
                finally
                {
                    try { File.Delete(tmp); } catch { }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Temp-file WAV recognition failed");
                return null;
            }
        }

        private readonly struct WavInfo
        {
            public readonly int Channels;
            public readonly int SampleRate;
            public readonly int BitsPerSample;
            public readonly int DataOffset;
            public readonly int DataLength;
            public WavInfo(int ch, int sr, int bps, int off, int len)
            { Channels = ch; SampleRate = sr; BitsPerSample = bps; DataOffset = off; DataLength = len; }
        }

        private bool TryParseWavHeader(ReadOnlySpan<byte> data, out WavInfo info)
        {
            info = default;
            if (data.Length <44) return false;
            // RIFF header
            if (data[0] != (byte)'R' || data[1] != (byte)'I' || data[2] != (byte)'F' || data[3] != (byte)'F') return false;
            if (data[8] != (byte)'W' || data[9] != (byte)'A' || data[10] != (byte)'V' || data[11] != (byte)'E') return false;

            int pos =12; // start of first chunk
            int channels =0, sampleRate =0, bitsPerSample =0;
            int dataOffset =0, dataSize =0;

            while (pos +8 <= data.Length)
            {
                // chunk header
                var chunkId = data.Slice(pos,4);
                int chunkSize = BitConverter.ToInt32(data.Slice(pos +4,4));
                pos +=8;
                if (pos + chunkSize > data.Length) break;

                if (chunkId[0] == (byte)'f' && chunkId[1] == (byte)'m' && chunkId[2] == (byte)'t' && chunkId[3] == (byte)' ')
                {
                    if (chunkSize <16) return false;
                    ushort audioFormat = BitConverter.ToUInt16(data.Slice(pos,2));
                    channels = BitConverter.ToUInt16(data.Slice(pos +2,2));
                    sampleRate = BitConverter.ToInt32(data.Slice(pos +4,4));
                    bitsPerSample = BitConverter.ToUInt16(data.Slice(pos +14,2));
                    if (audioFormat !=1) // PCM only
                    {
                        // Not PCM; let SDK handle via temp-file path
                        return false;
                    }
                }
                else if (chunkId[0] == (byte)'d' && chunkId[1] == (byte)'a' && chunkId[2] == (byte)'t' && chunkId[3] == (byte)'a')
                {
                    dataOffset = pos;
                    dataSize = chunkSize;
                }

                pos += chunkSize;
            }

            if (channels <=0 || sampleRate <=0 || bitsPerSample <=0 || dataOffset <=0 || dataSize <=0) return false;
            info = new WavInfo(channels, sampleRate, bitsPerSample, dataOffset, dataSize);
            return true;
        }
    }
}
