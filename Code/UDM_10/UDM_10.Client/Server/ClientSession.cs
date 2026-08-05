using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    // NOTE:
    // This template assumes the following classes already exist:
    // MessageBase, MessageType, UploadStartMessage, UploadChunkMessage,
    // UploadDoneMessage, AckMessage, UploadResultMessage,
    // MessageFramer, Logger, FileStorageService.

    public sealed class ClientSession : IDisposable
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly FileStorageService _storage;

        public ClientSession(TcpClient client, FileStorageService storage)
        {
            _client = client;
            _stream = client.GetStream();
            _storage = storage;
        }

        public async Task RunAsync(CancellationToken token = default)
        {
            Logger.Info($"Connected: {_client.Client.RemoteEndPoint}");

            try
            {
                while (_client.Connected && !token.IsCancellationRequested)
                {
                    MessageBase? msg = await MessageFramer.ReadAsync(_stream, token);

                    if (msg == null)
                        break;

                    await HandleMessageAsync(msg, token);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Info("Session cancelled.");
            }
            catch (IOException ex)
            {
                Logger.Warn($"Client disconnected: {ex.Message}");
            }
            catch (SocketException ex)
            {
                Logger.Warn($"Socket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
            finally
            {
                Dispose();
            }
        }

        private async Task HandleMessageAsync(MessageBase message, CancellationToken token)
        {
            switch (message.Type)
            {
                case MessageType.UploadStart:
                {
                    var req = (UploadStartMessage)message;

                    await _storage.BeginUploadAsync(req);

                    await SendAckAsync(new AckMessage
                    {
                        TransferId = req.TransferId,
                        ChunkIndex = 0,
                        Success = true,
                        Message = "UploadStart accepted"
                    });

                    break;
                }

                case MessageType.UploadChunk:
                {
                    var chunk = (UploadChunkMessage)message;

                    await HandleUploadAsync(chunk);

                    await SendAckAsync(new AckMessage
                    {
                        TransferId = chunk.TransferId,
                        ChunkIndex = chunk.ChunkIndex,
                        Success = true,
                        Message = "Chunk received"
                    });

                    break;
                }

                case MessageType.UploadDone:
                {
                    var done = (UploadDoneMessage)message;

                    await _storage.FinishUploadAsync(done.TransferId);

                    await SendResultAsync(new UploadResultMessage
                    {
                        TransferId = done.TransferId,
                        Success = true,
                        Message = "Upload completed successfully."
                    });

                    break;
                }

                default:
                {
                    await SendResultAsync(new UploadResultMessage
                    {
                        TransferId = Guid.Empty,
                        Success = false,
                        Message = $"Unsupported message: {message.Type}"
                    });

                    break;
                }
            }
        }

        private async Task HandleUploadAsync(UploadChunkMessage chunk)
        {
            await _storage.AppendChunkAsync(
                chunk.TransferId,
                chunk.ChunkIndex,
                chunk.Data);

            Logger.Info($"Chunk #{chunk.ChunkIndex} stored.");
        }

        private async Task SendAckAsync(AckMessage ack)
        {
            await MessageFramer.WriteAsync(_stream, ack);
        }

        private async Task SendResultAsync(UploadResultMessage result)
        {
            await MessageFramer.WriteAsync(_stream, result);
        }

        public void Dispose()
        {
            try { _stream?.Dispose(); } catch { }
            try { _client?.Close(); } catch { }

            Logger.Info("Client session disposed.");
        }
    }
}
