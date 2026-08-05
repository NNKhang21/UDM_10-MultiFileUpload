using System;
using System.Net.Sockets;
using System.Threading.Tasks;

class ClientSession
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;

    public ClientSession(TcpClient client)
    {
        _client = client;
        _stream = client.GetStream();
    }

    public async Task RunAsync()
    {
        Console.WriteLine("Client session started.");

        try
        {
            while (_client.Connected)
            {
                // TODO:
                // Sau này thay bằng:
                // MessageBase message = await MessageFramer.ReadAsync(_stream);

                object? message = await ReceiveMessageAsync();

                if (message == null)
                {
                    Console.WriteLine("Client disconnected.");
                    break;
                }

                await HandleMessageAsync(message);
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Client disconnected: {ex.Message}");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Socket error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client error: {ex.Message}");
        }
        finally
        {
            _stream.Close();
            _client.Close();

            Console.WriteLine("Client session ended.");
        }
    }

    /// <summary>
    /// Xử lý toàn bộ chuỗi UploadStart → Ack → Chunk → Done → Result
    /// </summary>
    private async Task HandleMessageAsync(object message)
    {
        switch (message)
        {
            case UploadStartMessage start:

                Console.WriteLine($"UploadStart: {start.FileName}");

                // TODO:
                // _storage.BeginUpload(start);

                await SendAckAsync();

                break;

            case UploadChunkMessage chunk:

                Console.WriteLine($"Chunk: {chunk.ChunkIndex}");

                // TODO:
                // HandleUploadAsync(chunk);

                await SendAckAsync();

                break;

            case UploadDoneMessage done:

                Console.WriteLine("Upload Done");

                // TODO:
                // _storage.FinishUpload(done);

                await SendResultAsync();

                break;

            default:

                Console.WriteLine("Unknown message.");

                break;
        }
    }

    // ==========================
    // Placeholder
    // ==========================

    private async Task<object?> ReceiveMessageAsync()
    {
        // TODO:
        // Sau này thay bằng MessageFramer.ReadAsync()
        await Task.Delay(10);

        return null;
    }

    private async Task SendAckAsync()
    {
        // TODO:
        // Gửi Ack về Client
        Console.WriteLine("ACK sent.");

        await Task.CompletedTask;
    }

    private async Task SendResultAsync()
    {
        // TODO:
        // Gửi Result về Client
        Console.WriteLine("RESULT sent.");

        await Task.CompletedTask;
    }
}
